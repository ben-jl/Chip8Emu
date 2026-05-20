using System;
using System.Collections.Generic;
using System.Text;

namespace Chip8Emu.Core.Memory
{
    internal class MemoryMap
    {
        public ushort RomStart { get; private set; }

        public ushort FontStart { get; private set; }
        public ushort FontSize { get; private set; }

        public ushort MemorySize { get; private set; }

        public MemoryMap()
        {
            RomStart = 0x200;
            FontStart = 0x0;
            FontSize = 0x5; // Character sprites are 5 bytes long
            MemorySize = 4096;
        }

        public ushort GetFontAddress(ushort character)
        {
            if (character > 0xF)
                throw new ArgumentOutOfRangeException(nameof(character), "Character must be between 0x0 and 0xF.");
            return (ushort)(FontStart + character * FontSize);
        }
    }
}
