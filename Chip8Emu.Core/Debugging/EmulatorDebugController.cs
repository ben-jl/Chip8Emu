using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Debugging.Breakpoints;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Core.Debugging
{
    public sealed class EmulatorDebugController : IEmulatorDebugController
    {
        private readonly IEmulatorMachine _machine;
        private readonly IBreakpointManager _breakpoints;

        private DebugExecutionMode _mode;
        private DebugStopReason _stopReason;
        private long _transitionSequence;
        private long _instructionSteps;
        private DateTimeOffset _lastTransitionUtc;
        private BreakpointMatch? _lastBreakpointMatch;

        public EmulatorDebugController(IEmulatorMachine machine)
            : this(machine, null)
        {
        }

        public EmulatorDebugController(IEmulatorMachine machine, IBreakpointManager? breakpointManager)
        {
            ArgumentNullException.ThrowIfNull(machine);

            _machine = machine;
            _breakpoints = breakpointManager ?? new BreakpointManager();
            _mode = DebugExecutionMode.Running;
            _stopReason = DebugStopReason.None;
            _transitionSequence = 0;
            _instructionSteps = 0;
            _lastTransitionUtc = DateTimeOffset.UtcNow;
            _lastBreakpointMatch = null;
        }

        public DebugStateSnapshot State => new(
            _mode,
            _stopReason,
            _transitionSequence,
            _instructionSteps,
            _lastTransitionUtc);

        public MachineSnapshot MachineSnapshot => _machine.CurrentSnapshot();
        public IBreakpointManager Breakpoints => _breakpoints;
        public BreakpointMatch? LastBreakpointMatch => _lastBreakpointMatch;

        public void Pause()
        {
            if (_mode == DebugExecutionMode.Paused && _stopReason == DebugStopReason.UserPause)
            {
                return;
            }

            _mode = DebugExecutionMode.Paused;
            _stopReason = DebugStopReason.UserPause;
            _lastBreakpointMatch = null;
            RecordTransition();
        }

        public void Resume()
        {
            if (_mode == DebugExecutionMode.Running && _stopReason == DebugStopReason.None)
            {
                return;
            }

            _mode = DebugExecutionMode.Running;
            _stopReason = DebugStopReason.None;
            _lastBreakpointMatch = null;
            RecordTransition();
        }

        public void StepInstructionOnce()
        {
            if (_mode != DebugExecutionMode.Paused)
            {
                throw new InvalidOperationException("Single-instruction stepping requires paused mode.");
            }

            _machine.StepInstruction();
            _instructionSteps++;
            _stopReason = DebugStopReason.StepComplete;
            _lastBreakpointMatch = null;
            RecordTransition();
        }

        public void Update()
        {
            if (_mode != DebugExecutionMode.Running)
            {
                return;
            }

            BreakpointMatch? breakpointMatch = null;
            var interrupted = _machine.StepFrameUntil((pc, opcode) =>
            {
                if (!_breakpoints.TryMatch(pc, opcode, out var match))
                {
                    return false;
                }

                breakpointMatch = match;
                return true;
            });

            if (interrupted && breakpointMatch is not null)
            {
                _mode = DebugExecutionMode.Paused;
                _stopReason = DebugStopReason.BreakpointHit;
                _lastBreakpointMatch = breakpointMatch;
                RecordTransition();
                return;
            }
        }

        private void RecordTransition()
        {
            _transitionSequence++;
            _lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }
}
