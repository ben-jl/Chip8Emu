namespace Chip8Emu.Core.Debugging.Disassembly
{
    public sealed record DisassemblyWindow(
        IReadOnlyList<DisassembledInstruction> Rows,
        int SelectedIndex,
        ushort ProgramCounter);
}
