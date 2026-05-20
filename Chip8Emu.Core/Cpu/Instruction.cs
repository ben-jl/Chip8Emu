using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Cpu
{
    internal readonly record struct Instruction(
        ushort Opcode,
        byte X,
        byte Y,
        byte N,
        byte NN,
        ushort NNN
    );
}
