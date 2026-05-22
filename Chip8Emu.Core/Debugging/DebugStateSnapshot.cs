namespace Chip8Emu.Core.Debugging
{
    public sealed record DebugStateSnapshot(
        DebugExecutionMode Mode,
        DebugStopReason StopReason,
        long TransitionSequence,
        long InstructionSteps,
        DateTimeOffset LastTransitionUtc);
}
