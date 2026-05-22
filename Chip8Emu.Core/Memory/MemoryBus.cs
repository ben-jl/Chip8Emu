using Chip8Emu.Core.Debugging.Trace;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Core.Memory
{
    internal class MemoryBus : IMemoryBus
    {
        private readonly byte[] _memory;
        private readonly ITraceSink? _traceSink;
        private readonly Queue<MemoryAccessSnapshot> _accesses = new();
        private readonly int _accessCapacity = 4096;
        private long _accessSequence;

        public MemoryBus(ushort size, ITraceSink? traceSink = null)
        {
            _memory = new byte[size];
            _traceSink = traceSink;
            _accessSequence = 0;
        }

        public long CurrentAccessSequence => _accessSequence;

        public byte Read(ushort address)
        {
            if (address >= _memory.Length)
            {
                throw new IndexOutOfRangeException($"Address {address:X4} is out of bounds.");
            }

            var value = _memory[address];
            RecordAccess(MemoryAccessKind.Read, address, value);
            _traceSink?.Publish(new MemoryReadTraceEvent(address, value));
            return value;
        }

        public byte ReadRaw(ushort address)
        {
            if (address >= _memory.Length)
            {
                throw new IndexOutOfRangeException($"Address {address:X4} is out of bounds.");
            }

            return _memory[address];
        }

        public void Write(ushort address, byte value)
        {
            if (address >= _memory.Length)
            {
                throw new IndexOutOfRangeException($"Address {address:X4} is out of bounds.");
            }

            _memory[address] = value;
            RecordAccess(MemoryAccessKind.Write, address, value);
            _traceSink?.Publish(new MemoryWriteTraceEvent(address, value));
        }

        public void WriteBlock(ushort startAddress, ReadOnlySpan<byte> data)
        {
            if (startAddress + data.Length > _memory.Length)
            {
                throw new IndexOutOfRangeException($"Data block exceeds memory bounds starting at address {startAddress:X4}.");
            }

            for (int i = 0; i < data.Length; i++)
            {
                _memory[startAddress + i] = data[i];
                RecordAccess(MemoryAccessKind.Write, (ushort)(startAddress + i), data[i]);
                _traceSink?.Publish(new MemoryWriteTraceEvent((ushort)(startAddress + i), data[i]));
            }
        }

        public void Clear()
        {
            Array.Clear(_memory);
        }

        public IReadOnlyList<MemoryAccessSnapshot> GetAccessesSince(long sequenceExclusive)
        {
            if (_accesses.Count == 0)
            {
                return Array.Empty<MemoryAccessSnapshot>();
            }

            var matching = _accesses.Where(a => a.Sequence > sequenceExclusive).ToArray();
            return matching;
        }

        private void RecordAccess(MemoryAccessKind kind, ushort address, byte value)
        {
            _accessSequence++;
            _accesses.Enqueue(new MemoryAccessSnapshot(
                _accessSequence,
                kind,
                address,
                value));

            while (_accesses.Count > _accessCapacity)
            {
                _accesses.Dequeue();
            }
        }
    }
}
