using Chip8Emu.Core.Debugging.Trace;

namespace Chip8Emu.Core.Memory
{
    internal class MemoryBus : IMemoryBus
    {
        private readonly byte[] _memory;
        private readonly ITraceSink? _traceSink;

        public MemoryBus(ushort size, ITraceSink? traceSink = null)
        {
            _memory = new byte[size];
            _traceSink = traceSink;
        }

        public byte Read(ushort address)
        {
            if (address >= _memory.Length)
            {
                throw new IndexOutOfRangeException($"Address {address:X4} is out of bounds.");
            }

            var value = _memory[address];
            _traceSink?.Publish(new MemoryReadTraceEvent(address, value));
            return value;
        }

        public void Write(ushort address, byte value)
        {
            if (address >= _memory.Length)
            {
                throw new IndexOutOfRangeException($"Address {address:X4} is out of bounds.");
            }

            _memory[address] = value;
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
                _traceSink?.Publish(new MemoryWriteTraceEvent((ushort)(startAddress + i), data[i]));
            }
        }

        public void Clear()
        {
            Array.Clear(_memory);
        }
    }
}
