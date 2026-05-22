
namespace Chip8Emu.Core.Diagnostics
{
    public sealed record CpuSnapshot(
        ushort PC,
        ushort I,
        byte[] V,
        int RandomSeed,
        int RandomCount);
}
