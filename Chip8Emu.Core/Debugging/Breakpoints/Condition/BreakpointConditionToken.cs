namespace Chip8Emu.Core.Debugging.Breakpoints.Condition
{
    internal readonly record struct BreakpointConditionToken(
        BreakpointConditionTokenType Type,
        string Lexeme);
}
