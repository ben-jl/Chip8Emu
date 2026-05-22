using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Core.Debugging
{
    public sealed class EmulatorDebugController : IEmulatorDebugController
    {
        private readonly IEmulatorMachine _machine;

        private DebugExecutionMode _mode;
        private DebugStopReason _stopReason;
        private long _transitionSequence;
        private long _instructionSteps;
        private DateTimeOffset _lastTransitionUtc;

        public EmulatorDebugController(IEmulatorMachine machine)
        {
            ArgumentNullException.ThrowIfNull(machine);

            _machine = machine;
            _mode = DebugExecutionMode.Running;
            _stopReason = DebugStopReason.None;
            _transitionSequence = 0;
            _instructionSteps = 0;
            _lastTransitionUtc = DateTimeOffset.UtcNow;
        }

        public DebugStateSnapshot State => new(
            _mode,
            _stopReason,
            _transitionSequence,
            _instructionSteps,
            _lastTransitionUtc);

        public MachineSnapshot MachineSnapshot => _machine.CurrentSnapshot();

        public void Pause()
        {
            if (_mode == DebugExecutionMode.Paused && _stopReason == DebugStopReason.UserPause)
            {
                return;
            }

            _mode = DebugExecutionMode.Paused;
            _stopReason = DebugStopReason.UserPause;
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
            RecordTransition();
        }

        public void Update()
        {
            if (_mode != DebugExecutionMode.Running)
            {
                return;
            }

            _machine.StepFrame();
        }

        private void RecordTransition()
        {
            _transitionSequence++;
            _lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }
}
