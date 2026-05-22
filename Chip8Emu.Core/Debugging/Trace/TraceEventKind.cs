namespace Chip8Emu.Core.Debugging.Trace
{
    public enum TraceEventKind
    {
        InstructionFetched = 0,
        InstructionDecoded = 1,
        InstructionExecuted = 2,
        InstructionFaulted = 3,
        MemoryRead = 4,
        MemoryWrite = 5,
        FrameStarted = 6,
        FrameCompleted = 7,
        TimerTicked = 8,
        KeyStateChanged = 9
    }
}
