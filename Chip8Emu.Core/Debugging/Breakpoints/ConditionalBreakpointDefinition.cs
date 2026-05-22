namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed record ConditionalBreakpointDefinition(
        string Expression,
        Func<BreakpointEvaluationContext, bool> Predicate);
}
