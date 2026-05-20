using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Memory
{
    internal class MemoryBus : IMemoryBus
    {
        private readonly byte[] _memory;

        public MemoryBus(ushort size)
        {
            _memory = new byte[size];
        }

        public byte Read(ushort address)
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
            }
        }
    }
}
