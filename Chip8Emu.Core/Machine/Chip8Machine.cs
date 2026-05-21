using Chip8Emu.Core.Cpu;
using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Display;
using Chip8Emu.Core.Input;
using Chip8Emu.Core.Memory;
using Chip8Emu.Core.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

        private readonly EmulationOptions _options;
        private int _instructionsSinceLastTimerTick;

        public Chip8Machine() : this(null)
        {
        }

        public Chip8Machine(EmulationOptions? options)
        {
            _options = options ?? EmulationOptions.Default;
            _memory = new MemoryBus(4096);
            _timers = new Timers();
            _memoryMap = new MemoryMap();
            _display = new MonochromeFrameBuffer(64, 32);
            _keypad = new KeypadState();
            _cpu = new Chip8Cpu(
                _memory, 
                _memoryMap, 
                _display, 
                _options.RandomSeed, 
                _keypad, 
                _timers,
                _options.ResetCarryFlagOnBitwiseOps,
                _options.IncrementIOnStoreLoadMemoryOps);
            Chip8Font.LoadInto(_memory, _memoryMap);
        }

        public IFrameBuffer Display => _display;

        public bool SoundEnabled => _timers.SoundTimer > 0;

        public void LoadRom(ReadOnlySpan<byte> romData)
        {
            Reset();
            _memory.WriteBlock(_memoryMap.RomStart, romData);
        }

        public void Reset()
        {
            _memory.Clear();
            _display.Clear();
            _keypad.Clear();
            _timers.Reset();
            _cpu.Reset();
            _instructionsSinceLastTimerTick = 0;

            Chip8Font.LoadInto(_memory, _memoryMap);
        }

        public void SetKeyState(byte key, bool isPressed)
        {
            _keypad.SetKey(key, isPressed);
        }

        public void StepFrame()
        {
            for(var i = 0; i < _options.InstructionsPerFrame; i++)
            {
                StepInstruction();
            }
        }

        public void StepInstruction()
        {
            _cpu.Step();
            _instructionsSinceLastTimerTick++;

            var tickInterval = Math.Max(1, _options.InstructionsPerFrame);
            if (_instructionsSinceLastTimerTick >= tickInterval)
            {
                _timers.Tick();
                _instructionsSinceLastTimerTick = 0;
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
    }
}
