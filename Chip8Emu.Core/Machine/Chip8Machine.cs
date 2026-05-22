using Chip8Emu.Core.Cpu;
using Chip8Emu.Core.Debugging.Trace;
using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Display;
using Chip8Emu.Core.Input;
using Chip8Emu.Core.Memory;
using Chip8Emu.Core.Timing;

namespace Chip8Emu.Core.Machine
{
    public sealed class Chip8Machine : IEmulatorMachine
    {
        private readonly Chip8Cpu _cpu;
        private readonly MemoryBus _memory;
        private readonly IFrameBuffer _display;
        private readonly KeypadState _keypad;
        private readonly Timers _timers;
        private readonly MemoryMap _memoryMap;
        private readonly ITraceSink? _traceSink;
        private byte[] _loadedRom = Array.Empty<byte>();

        private readonly EmulationOptions _options;
        private int _instructionsSinceLastTimerTick;

        public Chip8Machine() : this(null, null)
        {
        }

        public Chip8Machine(EmulationOptions? options) : this(options, null)
        {
        }

        public Chip8Machine(EmulationOptions? options, ITraceSink? traceSink)
        {
            _options = options ?? EmulationOptions.Default;
            _traceSink = traceSink;
            _memory = new MemoryBus(4096, traceSink);
            _timers = new Timers();
            _memoryMap = new MemoryMap();
            _display = new MonochromeFrameBuffer(64, 32);
            _keypad = new KeypadState();
            var cpuConfig = new Chip8CpuConfig(
                ResetCarryFlagOnBitwiseOps: _options.ResetCarryFlagOnBitwiseOps,
                IncrementIOnStoreLoadMemoryOps: _options.IncrementIOnStoreLoadMemoryOps,
                ShiftUsesVy: _options.ShiftUsesVy,
                ClipSprites: _options.ClipSprites,
                JumpWithV0: _options.JumpWithV0);
            _cpu = new Chip8Cpu(
                _memory,
                _memoryMap,
                _display,
                _options.RandomSeed,
                _keypad,
                _timers,
                cpuConfig,
                traceSink);
            Chip8Font.LoadInto(_memory, _memoryMap);
        }

        public IFrameBuffer Display => _display;

        public bool SoundEnabled => _timers.SoundTimer > 0;
        public long CurrentMemoryAccessSequence => _memory.CurrentAccessSequence;

        public void LoadRom(ReadOnlySpan<byte> romData)
        {
            Reset();
            _loadedRom = romData.ToArray();
            _memory.WriteBlock(_memoryMap.RomStart, _loadedRom);
        }

        public void Reset()
        {
            _memory.Clear();
            _display.Clear();
            _keypad.Clear();
            _timers.Reset();
            _cpu.Reset();
            _instructionsSinceLastTimerTick = 0;
            _loadedRom = Array.Empty<byte>();

            Chip8Font.LoadInto(_memory, _memoryMap);
        }

        public void SetKeyState(byte key, bool isPressed)
        {
            _keypad.SetKey(key, isPressed);
            _traceSink?.Publish(new KeyStateChangedTraceEvent(key, isPressed));
        }

        /// <summary>
        /// Executes one emulated frame. Timer cadence in this mode is one tick per frame.
        /// </summary>
        public void StepFrame()
        {
            _ = StepFrameUntil(static (_, _) => false);
        }

        public bool StepFrameUntil(Func<ushort, ushort, bool> shouldPauseBeforeInstruction)
        {
            ArgumentNullException.ThrowIfNull(shouldPauseBeforeInstruction);

            var instructionBudget = Math.Max(1, _options.InstructionsPerFrame);
            _traceSink?.Publish(new FrameStartedTraceEvent(instructionBudget));

            var drewSpriteThisFrame = false;
            var instructionsExecuted = 0;
            var exitedEarlyForDisplayWait = false;
            var interruptedByPredicate = false;
            for (var i = 0; i < instructionBudget; i++)
            {
                var pc = _cpu.CurrentSnapshot().PC;
                var opcode = PeekOpcodeAtProgramCounter();
                if (shouldPauseBeforeInstruction(pc, opcode))
                {
                    interruptedByPredicate = true;
                    break;
                }

                var allowDraw = !ShouldWaitForDraw() || !drewSpriteThisFrame;
                var stepResult = _cpu.Step(allowDraw);
                instructionsExecuted++;
                if (stepResult.WaitingForDrawVBlank)
                {
                    exitedEarlyForDisplayWait = true;
                    break;
                }
                if (stepResult.DrewSprite)
                {
                    drewSpriteThisFrame = true;
                }
            }

            // Frame stepping uses frame cadence for timers.
            // If a breakpoint pauses before any instruction executes, preserve timer state.
            if (!interruptedByPredicate || instructionsExecuted > 0)
            {
                _timers.Tick();
                _instructionsSinceLastTimerTick = 0;
                _traceSink?.Publish(new TimerTickedTraceEvent("Frame", _timers.DelayTimer, _timers.SoundTimer));
            }

            _traceSink?.Publish(new FrameCompletedTraceEvent(
                instructionsExecuted,
                drewSpriteThisFrame,
                exitedEarlyForDisplayWait));

            return interruptedByPredicate;
        }

        private bool ShouldWaitForDraw()
        {
            if (!_options.DisplayWaitOnDraw)
            {
                return false;
            }

            return _options.DisplayWaitScope switch
            {
                DisplayWaitScope.AllDisplayModes => true,
                DisplayWaitScope.LowResolutionOnly => _display.Width == 64 && _display.Height == 32,
                DisplayWaitScope.HighResolutionOnly => _display.Width == 128 && _display.Height == 64,
                _ => true
            };
        }

        /// <summary>
        /// Executes one instruction. Timer cadence in this mode is instruction-dependent.
        /// </summary>
        public void StepInstruction()
        {
            _ = _cpu.Step();
            _instructionsSinceLastTimerTick++;

            // Instruction stepping uses instruction cadence for timers.
            var tickInterval = Math.Max(1, _options.InstructionsPerFrame);
            if (_instructionsSinceLastTimerTick >= tickInterval)
            {
                _timers.Tick();
                _instructionsSinceLastTimerTick = 0;
                _traceSink?.Publish(new TimerTickedTraceEvent("Instruction", _timers.DelayTimer, _timers.SoundTimer));
            }
        }

        public MachineSnapshot CurrentSnapshot()
        {
            return new MachineSnapshot(
                _cpu.CurrentSnapshot(),
                _timers.DelayTimer,
                _timers.SoundTimer
            );
        }

        public bool TryGetLoadedRom(out ReadOnlyMemory<byte> romData, out ushort romStartAddress)
        {
            if (_loadedRom.Length == 0)
            {
                romData = ReadOnlyMemory<byte>.Empty;
                romStartAddress = _memoryMap.RomStart;
                return false;
            }

            romData = _loadedRom;
            romStartAddress = _memoryMap.RomStart;
            return true;
        }

        public ushort PeekOpcodeAtProgramCounter()
        {
            var pc = _cpu.CurrentSnapshot().PC;
            var high = _memory.ReadRaw(pc);
            var low = _memory.ReadRaw((ushort)(pc + 1));
            return (ushort)((high << 8) | low);
        }

        public IReadOnlyList<MemoryAccessSnapshot> GetMemoryAccessesSince(long sequenceExclusive)
        {
            return _memory.GetAccessesSince(sequenceExclusive);
        }
    }
}
