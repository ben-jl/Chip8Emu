using System.Collections.ObjectModel;
using System.Threading;

namespace Chip8Emu.Core.Debugging.Trace
{
    public sealed class InMemoryTraceBuffer : ITraceBuffer
    {
        private readonly object _gate = new();
        private readonly Queue<TraceEvent> _events;
        private readonly int _capacity;
        private long _sequence;

        public InMemoryTraceBuffer(int capacity = 2048)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Trace buffer capacity must be positive.");
            }

            _capacity = capacity;
            _events = new Queue<TraceEvent>(capacity);
            _sequence = 0;
        }

        public int Capacity => _capacity;

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _events.Count;
                }
            }
        }

        public void Publish(TraceEvent traceEvent)
        {
            ArgumentNullException.ThrowIfNull(traceEvent);

            var sequence = Interlocked.Increment(ref _sequence);
            var stampedEvent = traceEvent with
            {
                Sequence = sequence,
                TimestampUtc = DateTimeOffset.UtcNow
            };

            lock (_gate)
            {
                if (_events.Count == _capacity)
                {
                    _events.Dequeue();
                }

                _events.Enqueue(stampedEvent);
            }
        }

        public IReadOnlyList<TraceEvent> Snapshot(int maxCount = int.MaxValue)
        {
            if (maxCount <= 0)
            {
                return Array.Empty<TraceEvent>();
            }

            lock (_gate)
            {
                if (_events.Count == 0)
                {
                    return Array.Empty<TraceEvent>();
                }

                var all = _events.ToArray();
                if (maxCount >= all.Length)
                {
                    return Array.AsReadOnly(all);
                }

                var start = all.Length - maxCount;
                var subset = new TraceEvent[maxCount];
                Array.Copy(all, start, subset, 0, maxCount);
                return new ReadOnlyCollection<TraceEvent>(subset);
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _events.Clear();
            }
        }
    }
}
