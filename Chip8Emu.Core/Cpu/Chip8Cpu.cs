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

        private readonly bool _resetCarryFlagOnBitwiseOp;
        private readonly bool _incrementIOnStoreLoadMemoryOp;
        private readonly bool _shiftUsesVy;

        public Chip8Cpu(
            IMemoryBus memoryBus, 
            MemoryMap memoryMap, 
            IFrameBuffer display, 
            int randomSeed, 
            IKeypad keypad,
            Timers timers,
            bool resetCarryFlagOnBitwiseOp,
            bool incrementIOnStoreLoadMemoryOp,
            bool shiftUsesVy)
        {
            ArgumentNullException.ThrowIfNull(memoryBus);
            ArgumentNullException.ThrowIfNull(memoryMap);
            ArgumentNullException.ThrowIfNull(display);
            ArgumentNullException.ThrowIfNull(keypad);
            ArgumentNullException.ThrowIfNull(timers);

            _memoryMap = memoryMap;
            _memoryBus = memoryBus;
            _display = display;
            _registers = new Registers(memoryMap);
            _decoder = new InstructionDecoder();
            _random = new Random(randomSeed);
            _randomSeed = randomSeed;
            _keypad = keypad;
            _timers = timers;
            _resetCarryFlagOnBitwiseOp = resetCarryFlagOnBitwiseOp;
            _incrementIOnStoreLoadMemoryOp = incrementIOnStoreLoadMemoryOp;
            _shiftUsesVy = shiftUsesVy;
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
            var instruction = _decoder.DecodeOrThrow(opcode);
            Execute(instruction);
        }

        private void Execute(DecodedInstruction instruction)
        {
            var operands = instruction.Operands;
            var mnemonic = instruction.Definition.Mnemonic;

            switch (instruction.Definition.Pattern)
            {
                case Chip8InstructionSet.PatternCLS:
                    CLS();
                    break;
                case Chip8InstructionSet.PatternRET:
                    RET();
                    break;
                case Chip8InstructionSet.PatternSYS:
                    SYS(operands.RequireNNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternJP:
                    JP(operands.RequireNNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternCALL:
                    CALL(operands.RequireNNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSEByte:
                    SEBYTE(operands.RequireX(mnemonic), operands.RequireNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSNEByte:
                    SNEBYTE(operands.RequireX(mnemonic), operands.RequireNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSEReg:
                    SEREG(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDByte:
                    LDBYTE(operands.RequireX(mnemonic), operands.RequireNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternADDByte:
                    ADDBYTE(operands.RequireX(mnemonic), operands.RequireNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDReg:
                    LDREG(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternOR:
                    OR(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternAND:
                    AND(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternXOR:
                    XOR(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternADDReg:
                    ADDREG(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSUB:
                    SUB(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSHR:
                    SHR(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSUBN:
                    SUBN(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSHL:
                    SHL(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSNEReg:
                    SNEREG(operands.RequireX(mnemonic), operands.RequireY(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDI:
                    LDI(operands.RequireNNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternJPV0:
                    JPV0(operands.RequireNNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternRND:
                    RND(operands.RequireX(mnemonic), operands.RequireNN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternDRW:
                    DRW(operands.RequireX(mnemonic), operands.RequireY(mnemonic), operands.RequireN(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSKP:
                    SKP(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSKNP:
                    SKNP(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDDT:
                    LDDT(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDK:
                    LDK(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSTDT:
                    STDT(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSTST:
                    STST(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternADDI:
                    ADDI(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDFNT:
                    LDFNT(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDBCD:
                    LDBCD(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternSTREGI:
                    STREGI(operands.RequireX(mnemonic));
                    break;
                case Chip8InstructionSet.PatternLDREGI:
                    LDREGI(operands.RequireX(mnemonic));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown opcode pattern: {instruction.Opcode:X4}");
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
            if (_resetCarryFlagOnBitwiseOp)
            {
                _registers.SetV(0xF, 0);
            }
        }

        // 8xy2 - AND Vx, Vy
        private void AND(byte x, byte y)
        {
            var res = _registers.GetV(x) & _registers.GetV(y);
            _registers.SetV(x, (byte)res);
            if (_resetCarryFlagOnBitwiseOp)
            {
                _registers.SetV(0xF, 0);
            }
        }

        // 8xy3 - XOR Vx, Vy
        private void XOR(byte x, byte y)
        {
            var res = _registers.GetV(x) ^ _registers.GetV(y);
            _registers.SetV(x, (byte)res);
            if (_resetCarryFlagOnBitwiseOp)
            {
                _registers.SetV(0xF, 0);
            }
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
        private void SHR(byte x, byte y)
        {
            if (_shiftUsesVy)
            {
                _registers.SetV(x, _registers.GetV(y));
            }
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
        private void SHL(byte x, byte y)
        {
            if(_shiftUsesVy)
            {
                _registers.SetV(x, _registers.GetV(y));
            }
            var msb = (byte)((_registers.GetV(x) & 0x80) >> 7);

            var val = (byte)(_registers.GetV(x));
            if(msb == 1)
            {
                val = (byte)(val & 0x7F);
            }

            _registers.SetV(x, (byte)(val << 1));
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
            if(_incrementIOnStoreLoadMemoryOp)
            {
                _registers.SetI((ushort)(_registers.I + x + 1));
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
            if (_incrementIOnStoreLoadMemoryOp)
            {
                _registers.SetI((ushort)(_registers.I + x + 1));
            }
            return;
        }
    }
}
