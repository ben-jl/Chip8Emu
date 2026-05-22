namespace Chip8Emu.Core.Debugging.Trace
{
    public interface ITraceBuffer : ITraceSink
    {
        int Capacity { get; }
        int Count { get; }

        IReadOnlyList<TraceEvent> Snapshot(int maxCount = int.MaxValue);
        void Clear();
    }
}
