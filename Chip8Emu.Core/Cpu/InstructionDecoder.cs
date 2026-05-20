using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Cpu
{
    internal class InstructionDecoder
    {
        public InstructionDecoder() { }

        public Instruction Decode(ushort opcode)
        {
            byte x = (byte)((opcode & 0x0F00) >> 8);
            byte y = (byte)((opcode & 0x00F0) >> 4);
            byte n = (byte)(opcode & 0x000F);
            byte nn = (byte)(opcode & 0x00FF);
            ushort nnn = (ushort)(opcode & 0x0FFF);
            return new Instruction(opcode, x, y, n, nn, nnn);
        }
    }
}
