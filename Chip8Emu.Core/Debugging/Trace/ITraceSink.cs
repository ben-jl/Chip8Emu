namespace Chip8Emu.Core.Debugging.Trace
{
    public interface ITraceSink
    {
        void Publish(TraceEvent traceEvent);
    }
}
