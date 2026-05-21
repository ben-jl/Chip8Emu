namespace Chip8Emu.Core.Cpu
{
    public sealed record InstructionVariantKey(ushort Mask, ushort Pattern);
}
