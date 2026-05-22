namespace Chip8Emu.Core.Debugging
{
    public enum DebugStopReason
    {
        None = 0,
        UserPause = 1,
        StepComplete = 2,
        BreakpointHit = 3
    }
}
