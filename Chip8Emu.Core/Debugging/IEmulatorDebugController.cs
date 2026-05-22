using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Debugging.Breakpoints;

namespace Chip8Emu.Core.Debugging
{
    public interface IEmulatorDebugController
    {
        DebugStateSnapshot State { get; }

        MachineSnapshot MachineSnapshot { get; }
        IBreakpointManager Breakpoints { get; }
        BreakpointMatch? LastBreakpointMatch { get; }

        void Pause();
        void Resume();
        void StepInstructionOnce();
        void Update();
    }
}
