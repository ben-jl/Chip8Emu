using Chip8Emu.Core.Diagnostics;

namespace Chip8Emu.Core.Debugging
{
    public interface IEmulatorDebugController
    {
        DebugStateSnapshot State { get; }

        MachineSnapshot MachineSnapshot { get; }

        void Pause();
        void Resume();
        void StepInstructionOnce();
        void Update();
    }
}
