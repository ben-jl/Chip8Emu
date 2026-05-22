namespace Chip8Emu.Core.Debugging.Trace
{
    public abstract record TraceEvent(TraceEventKind Kind)
    {
        public long Sequence { get; init; }
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    }
}
