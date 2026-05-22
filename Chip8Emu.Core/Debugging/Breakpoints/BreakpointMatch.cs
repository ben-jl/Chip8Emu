namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed record BreakpointMatch(
        Guid BreakpointId,
        BreakpointKind Kind,
        ushort ProgramCounter,
        ushort Opcode,
        string Description);
}
