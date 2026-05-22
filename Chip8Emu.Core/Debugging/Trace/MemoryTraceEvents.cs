namespace Chip8Emu.Core.Debugging.Trace
{
    public sealed record MemoryReadTraceEvent(
        ushort Address,
        byte Value) : TraceEvent(TraceEventKind.MemoryRead);

    public sealed record MemoryWriteTraceEvent(
        ushort Address,
        byte Value) : TraceEvent(TraceEventKind.MemoryWrite);
}
