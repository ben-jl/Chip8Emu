namespace Chip8Emu.Core.Debugging.Disassembly
{
    public interface IChip8Disassembler
    {
        IReadOnlyList<DisassembledInstruction> Disassemble(ReadOnlySpan<byte> romData, ushort romStartAddress = 0x200);
        DisassemblyWindow CreateWindow(IReadOnlyList<DisassembledInstruction> listing, ushort programCounter, int radius = 8);
    }
}
