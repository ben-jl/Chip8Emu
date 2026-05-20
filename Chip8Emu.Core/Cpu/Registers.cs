using Chip8Emu.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Cpu
{
    internal class Registers
    {
        private readonly byte[] _v; // General-purpose registers V0 to VF

        public ushort I { get; private set; } // Index register

        public ushort PC { get; private set; } // Program counter
        public byte SP { get; private set; } // Stack pointer

        public byte Delay { get; private set; } // Delay timer
        public byte Sound { get; private set; } // Sound timer

        public ushort[] Stack { get; private set; } // Stack for subroutine calls

        public Registers(MemoryMap memoryMap)
        {
            _v = new byte[16];
            Stack = new ushort[16];

            Array.Clear(_v);
            Array.Clear(Stack);

            I = 0;
            PC = memoryMap.RomStart;
            SP = 0;
            Delay = 0;
            Sound = 0;
        }

        public byte GetV(byte register)
        {
            if (register > 0xF)
            {
                throw new ArgumentOutOfRangeException(nameof(register), "Register must be between 0x0 and 0xF.");
            }
            return _v[register];
        }

        public byte SetV(byte register, byte value)
        {
            if (register > 0xF)
            {
                throw new ArgumentOutOfRangeException(nameof(register), "Register must be between 0x0 and 0xF.");
            }
            _v[register] = value;
            return value;
        }

        public void Push(ushort addr)
        {
            if(SP >= Stack.Length)
            {
                throw new StackOverflowException("Stack overflow: cannot push more than 16 addresses.");
            }
            Stack[SP++] = addr;
        }

        public ushort Pop()
        {
            if(SP == 0)
            {
                throw new InvalidOperationException("Stack underflow: cannot pop from an empty stack.");
            }
            return Stack[--SP];
        }

        public void IncrementPC(ushort offset = 2)
        {
            PC += offset;
        }

        public void SetPC(ushort address)
        {
            PC = address;
        }

        public void SetI(ushort address)
        {
            I = address;
        }
    }
}
