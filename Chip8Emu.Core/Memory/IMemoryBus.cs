using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Memory
{
    internal interface IMemoryBus
    {
        byte Read(ushort address);
        void Write(ushort address, byte value);
        void WriteBlock(ushort startAddress, ReadOnlySpan<byte> data);
        void Clear();
    }
}
