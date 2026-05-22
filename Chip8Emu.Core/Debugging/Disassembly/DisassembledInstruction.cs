namespace Chip8Emu.Core.Debugging.Disassembly
{
    public sealed record DisassembledInstruction(
        ushort Address,
        ushort Opcode,
        string Mnemonic,
        string OperandsText,
        bool IsValid,
        string? Error);
}
