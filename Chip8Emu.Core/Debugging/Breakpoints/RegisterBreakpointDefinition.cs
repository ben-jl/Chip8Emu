namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed record RegisterBreakpointDefinition(
        string RegisterName,
        RegisterComparisonKind Comparison,
        ushort? CompareValue);
}
