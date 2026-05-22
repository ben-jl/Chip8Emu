
namespace Chip8Emu.Core.Diagnostics
{
    public sealed record MachineSnapshot(
        CpuSnapshot Cpu,
        byte DelayTimer,
        byte SoundTimer
    );
}
