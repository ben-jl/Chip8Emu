namespace Chip8Emu.Core.Debugging.Trace
{
    public sealed record FrameStartedTraceEvent(
        int InstructionBudget) : TraceEvent(TraceEventKind.FrameStarted);

    public sealed record FrameCompletedTraceEvent(
        int InstructionsExecuted,
        bool DrewSprite,
        bool ExitedEarlyForDisplayWait) : TraceEvent(TraceEventKind.FrameCompleted);

    public sealed record TimerTickedTraceEvent(
        string Cadence,
        byte DelayTimer,
        byte SoundTimer) : TraceEvent(TraceEventKind.TimerTicked);

    public sealed record KeyStateChangedTraceEvent(
        byte Key,
        bool IsPressed) : TraceEvent(TraceEventKind.KeyStateChanged);
}
