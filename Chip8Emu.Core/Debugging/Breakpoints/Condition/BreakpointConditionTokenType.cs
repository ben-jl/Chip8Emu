namespace Chip8Emu.Core.Debugging.Breakpoints.Condition
{
    internal enum BreakpointConditionTokenType
    {
        Identifier,
        Number,
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        And,
        Or,
        LeftParen,
        RightParen,
        End
    }
}
