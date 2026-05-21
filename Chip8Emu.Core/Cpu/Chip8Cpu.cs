using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Display;
using Chip8Emu.Core.Input;
using Chip8Emu.Core.Memory;
using Chip8Emu.Core.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Cpu
{
    internal sealed class Chip8Cpu
    {
        private readonly Registers _registers;
        private readonly MemoryMap _memoryMap;
        private readonly IMemoryBus _memoryBus;
        private readonly InstructionDecoder _decoder;
        private readonly IFrameBuffer _display;
        private readonly Random _random;
        private readonly int _randomSeed;
        private int _randomCount = 0;
        private readonly IKeypad _keypad;
        private readonly Timers _timers;

        public Chip8Cpu(
            IMemoryBus memoryBus, 
            MemoryMap memoryMap, 
            IFrameBuffer display, 
            int randomSeed, 
            IKeypad keypad,
            Timers timers)
        {
            _memoryMap = memoryMap;
            _memoryBus = memoryBus;
            _display = display;
            _registers = new Registers(memoryMap);
            _decoder = new InstructionDecoder();
            _random = new Random(randomSeed);
            _randomSeed = randomSeed;
            _keypad = keypad;
            _timers = new Timers();
        }

        public CpuSnapshot CurrentSnapshot()
        {
            return new CpuSnapshot(
                _registers.PC,
                _registers.I,
                _registers.GetRegisterSnapshot(),
                _randomSeed,
                _randomCount
                );
        }

        public void Reset()
        {
            _registers.Reset(_memoryMap);
        }

        public void Step()
        {
            ushort opcode = FetchOpcode();
            var instruction = _decoder.Decode(opcode);
            Execute(instruction);
        }

        private void Execute(Instruction op)
        {
            switch(op.Opcode & 0xF000)
            {
                case 0x000:
                    switch (op.Opcode)
                    {
                        case 0x00E0: CLS(); break;
                        case 0x00EE: RET(); break;
                        default: SYS(op.NNN); break;
                    }
                    break;
                case 0x1000: JP(op.NNN); break;
                case 0x2000: CALL(op.NNN); break;
                case 0x3000: SEBYTE(op.X, op.NN); break;
                case 0x4000: SNEBYTE(op.X, op.NN); break;
                case 0x5000: SEREG(op.X, op.Y); break;
                case 0x6000: LDBYTE(op.X, op.NN); break;
                case 0x7000: ADDBYTE(op.X, op.NN); break;
                case 0x8000:
                    switch (op.N)
                    {
                        case 0x0: LDREG(op.X, op.Y); break;
                        case 0x1: OR(op.X, op.Y); break;
                        case 0x2: AND(op.X, op.Y); break;
                        case 0x3: XOR(op.X, op.Y); break;
                        case 0x4: ADDREG(op.X, op.Y); break;
                        case 0x5: SUB(op.X, op.Y); break;
                        case 0x6: SHR(op.X); break;
                        case 0x7: SUBN(op.X, op.Y); break;
                        case 0xE: SHL(op.X); break;
                        default: throw new InvalidOperationException($"Unknown opcode: {op.Opcode:X4}");
                    }
                    break;
                case 0x9000: SNEREG(op.X, op.Y); break;
                case 0xA000: LDI(op.NNN); break;
                case 0xB000: JPV0(op.NNN); break;
                case 0xC000: RND(op.X, op.NN); break;
                case 0xD000: DRW(op.X, op.Y, op.N); break;
                case 0xE000:
                    switch (op.NN)
                    {
                        case 0x9E: SKP(op.X); break;
                        case 0xA1: SKNP(op.X); break;
                        default: throw new InvalidOperationException($"Unknown opcode: {op.Opcode:X4}");
                    }
                    break;
                case 0xF000:
                    switch (op.NN)
                    {
                        case 0x07: LDDT(op.X); break;
                        case 0x0A: LDK(op.X); break;
                        case 0x15: STDT(op.X); break;
                        case 0x18: STST(op.X); break;
                        case 0x1E: ADDI(op.X); break;
                        case 0x29: LDFNT(op.X); break;
                        case 0x33: LDBCD(op.X); break;
                        case 0x55: STREGI(op.X); break;
                        case 0x65: LDREGI(op.X); break;
                        default: throw new InvalidOperationException($"Unknown opcode: {op.Opcode:X4}");
                    }
                    break;
            }
        }

        private ushort FetchOpcode()
        {
            byte highByte = _memoryBus.Read(_registers.PC);
            byte lowByte = _memoryBus.Read((ushort)(_registers.PC + 1));

            _registers.IncrementPC();

            return (ushort)((highByte << 8) | lowByte);
        }

        // 0nnn
        private void SYS(ushort addr)
        {
            // ignored by modern interpreters
            return;
        }

        // 00E0 - CLS
        private void CLS()
        {
            _display.Clear();
            return;
        }

        // 00EE - RET
        private void RET()
        {
            var nxt = _registers.Pop();
            _registers.SetPC(nxt);
            return;
        }

        // 1nnn - JP addr
        private void JP(ushort nnn)
        {
            _registers.SetPC(nnn);
            return;
        }

        // 2nnn - CALL addr
        private void CALL(ushort addr)
        {
            _registers.Push(_registers.PC);
            _registers.SetPC(addr);
            return;
        }

        // 3xkk - SE Vx, byte
        private void SEBYTE(byte x, byte kk)
        {
            if (_registers.GetV(x) == kk)
            {
                _registers.IncrementPC();
            }
            return;
        }

        // 4xkk - SNE Vx, byte
        private void SNEBYTE(byte x, byte kk)
        {
            if (_registers.GetV(x) != kk)
            {
                _registers.IncrementPC();
            }
            return;
        }

        // 5xy0 - SE Vx, Vy
        private void SEREG(byte x, byte y)
        {
            if (_registers.GetV(x) == _registers.GetV(y))
            {
                _registers.IncrementPC();
            }
            return;
        }

        // 6xkk - LD Vx, byte
        private void LDBYTE(byte x, byte kk)
        {
            _registers.SetV(x, kk);
            return;
        }

        // 7xkk - ADD Vx, byte
        private void ADDBYTE(byte x, byte kk)
        {
            var currentValue = _registers.GetV(x);
            _registers.SetV(x, (byte)(currentValue + kk));
        }

        // 8xy0 - LD Vx, Vy
        private void LDREG(byte x, byte y)
        {
            _registers.SetV(x, _registers.GetV(y));
            return;
        }

        // 8xy1 - OR Vx, Vy
        private void OR(byte x, byte y)
        {
            var res = _registers.GetV(x) | _registers.GetV(y);
            _registers.SetV(x, (byte)res);
        }

        // 8xy2 - AND Vx, Vy
        private void AND(byte x, byte y)
        {
            var res = _registers.GetV(x) & _registers.GetV(y);
            _registers.SetV(x, (byte)res);
        }

        // 8xy3 - XOR Vx, Vy
        private void XOR(byte x, byte y)
        {
            var res = _registers.GetV(x) ^ _registers.GetV(y);
            _registers.SetV(x, (byte)res);
        }

        // 8xy4 - ADD Vx, Vy
        private void ADDREG(byte x, byte y)
        {
            var res = _registers.GetV(x) + _registers.GetV(y);
            var carry = res > 0xFF ? 1 : 0;

            var trunc = (byte)(res & 0xFF);
            _registers.SetV(x, trunc);
            _registers.SetV(0xF, (byte)carry);
        }

        // 8xy5 - SUB Vx, Vy
        private void SUB(byte x, byte y)
        {
            var borrow = _registers.GetV(x) > _registers.GetV(y) ? 1 : 0;

            // TODO validate negative handling
            var res = (byte)(_registers.GetV(x) - _registers.GetV(y));
            _registers.SetV(x, res);
            _registers.SetV(0xF, (byte)borrow);
            return;
        }

        // 8xy6 - SHR Vx {, Vy}
        private void SHR(byte x)
        {
            var lsb = (byte)(_registers.GetV(x) & 0x1);
            _registers.SetV(x, (byte)(_registers.GetV(x) >> 1));
            _registers.SetV(0xF, lsb);
            return;
        }

        // 8xy7 - SUBN Vx, Vy
        private void SUBN(byte x, byte y)
        {
            var borrow = _registers.GetV(y) > _registers.GetV(x) ? 1 : 0;
            var res = (byte)(_registers.GetV(y) - _registers.GetV(x));
            _registers.SetV(x, res);
            _registers.SetV(0xF, (byte)borrow);
             return;
        }

        // 8xyE - SHL Vx {, Vy}
        private void SHL(byte x)
        {
            // TODO validate behavior
            var msb = (byte)((_registers.GetV(x) & 0x80) >> 7);

            var val = (byte)(_registers.GetV(x));
            if(msb == 1)
            {
                val = (byte)(val & 0x7F);
            }

            _registers.SetV(x, val);
            _registers.SetV(0xF, msb);
             return;
        }

        // 9xy0 - SNE Vx, Vy
        private void SNEREG(byte x, byte y)
        {
            if (_registers.GetV(x) != _registers.GetV(y))
            {
                _registers.IncrementPC();
            }
             return;
        }

        // Annn - LD I, addr
        private void LDI(ushort nnn)
        {
            _registers.SetI(nnn);
            return;
        }

        // Bnnn - JP V0, addr
        private void JPV0(ushort nnn)
        {
            var target = (ushort)(nnn + _registers.GetV(0));
            _registers.SetPC(target);
             return;
        }

        // Cxkk - RND Vx, byte
        private void RND(byte x, byte kk)
        {
            var value = (byte)(_random.Next(0, 256) & kk);
            _registers.SetV(x, value);
            _randomCount++;
            return;
        }

        // Dxyn - DRW Vx, Vy, nibble
        private void DRW(byte x, byte y, byte n)
        {
            byte[] spriteDate = new byte[n];
            for (int i = 0; i < n; i++)
            {
                spriteDate[i] = _memoryBus.Read((ushort)(_registers.I + i));
            }

            for(int row = 0; row < n; row++)
            {
                byte spritRow = spriteDate[row];
                for(int col = 0; col < 8; col++)
                {
                    bool pixelOn = (spritRow & (0x80 >> col)) != 0;
                    if (pixelOn)
                    {
                        bool erased = _display.XorPixel((byte)(_registers.GetV(x) + col), (byte)(_registers.GetV(y) + row));
                        if (erased)
                        {
                            _registers.SetV(0xF, 1);
                        }
                    }
                }
            }

            return;
        }

        // ExE9E - SKP Vx
        private void SKP(byte x)
        {
            if(_keypad.IsPressed(_registers.GetV(x)))
            {
                _registers.IncrementPC();
            }
             return;
        }

        // ExA1 - SKNP Vx
        private void SKNP(byte x)
        {
            if(!_keypad.IsPressed(_registers.GetV(x)))
            {
                _registers.IncrementPC();
            }
             return;
        }

        // Fx07 - LD Vx, DT
        private void LDDT(byte x)
        {
            _registers.SetV(x, _timers.DelayTimer);
             return;
        }

        // Fx0A - LD Vx, K
        private void LDK(byte x)
        {
            var key = _keypad.FirstPressedKey;
            if (key.HasValue)
            {
                _registers.SetV(x, key.Value);
            }
             else
             {
                 // repeat this instruction until a key is pressed
                 _registers.DecrementPC();
             }
             return;
        }

        // Fx15 - LD DT, Vx
        private void STDT(byte x)
        {
            _timers.DelayTimer = _registers.GetV(x);
             return;
        }

        // Fx18 - LD ST, Vx
        private void STST(byte x)
        {
            _timers.SoundTimer = _registers.GetV(x);
             return;
        }

        // Fx1E - ADD I, Vx
        private void ADDI(byte x)
        {
            var res = _registers.I + _registers.GetV(x);
            _registers.SetI((ushort)res);
             return;
        }

        // Fx29 - LD F, Vx
        private void LDFNT(byte x)
        {
            var digit = _registers.GetV(x);
            var fontAddress = (ushort)(_memoryMap.FontStart + digit * _memoryMap.FontSize);
            _registers.SetI(fontAddress);
             return;
        }

        // Fx33 - LD B, Vx
        private void LDBCD(byte x)
        {
            var value = _registers.GetV(x);
            var hundreds = (byte)(value / 100);
            var tens = (byte)((value % 100) / 10);
            var ones = (byte)(value % 10);

            _memoryBus.Write(_registers.I, hundreds);
            _memoryBus.Write((ushort)(_registers.I + 1), tens);
            _memoryBus.Write((ushort)(_registers.I + 2), ones);
             return;
        }

        // Fx55 - LD [I], Vx
        private void STREGI(byte x)
        {
            for (int i = 0; i <= x; i++)
            {
                _memoryBus.Write((ushort)(_registers.I + i), _registers.GetV((byte)i));
            }
             return;
        }

        // Fx65 - LD Vx, [I]
        private void LDREGI(byte x)
        {
            for (int i = 0; i <= x; i++)
            {
                _registers.SetV((byte)i, _memoryBus.Read((ushort)(_registers.I + i)));
            }
             return;
        }
    }
}
