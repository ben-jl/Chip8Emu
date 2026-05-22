namespace Chip8Emu.Core.Machine
{
    public sealed record MemoryAccessSnapshot(
        long Sequence,
        MemoryAccessKind Kind,
        ushort Address,
        byte Value);
}
