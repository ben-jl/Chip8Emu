using Chip8Emu.Core.Debugging.Trace;
using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Display;
using Chip8Emu.Core.Input;
using Chip8Emu.Core.Memory;
using Chip8Emu.Core.Timing;

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
        private readonly ITraceSink? _traceSink;

        private readonly bool _resetCarryFlagOnBitwiseOp;
        private readonly bool _incrementIOnStoreLoadMemoryOp;
        private readonly bool _shiftUsesVy;
        private readonly bool _clipSprites;
        private readonly bool _jumpWithV0;

        private readonly Dictionary<ushort, Func<DecodedInstruction, bool>> _instructionHandlers;

        public Chip8Cpu(
            IMemoryBus memoryBus,
            MemoryMap memoryMap,
            IFrameBuffer display,
            int randomSeed,
            IKeypad keypad,
            Timers timers,
            Chip8CpuConfig cpuConfig,
            ITraceSink? traceSink = null)
        {
            ArgumentNullException.ThrowIfNull(memoryBus);
            ArgumentNullException.ThrowIfNull(memoryMap);
            ArgumentNullException.ThrowIfNull(display);
            ArgumentNullException.ThrowIfNull(keypad);
            ArgumentNullException.ThrowIfNull(timers);
            ArgumentNullException.ThrowIfNull(cpuConfig);

            _memoryMap = memoryMap;
            _memoryBus = memoryBus;
            _display = display;
            _registers = new Registers(memoryMap);
            _decoder = new InstructionDecoder();
            _random = new Random(randomSeed);
            _randomSeed = randomSeed;
            _keypad = keypad;
            _timers = timers;
            _traceSink = traceSink;
            _resetCarryFlagOnBitwiseOp = cpuConfig.ResetCarryFlagOnBitwiseOps;
            _incrementIOnStoreLoadMemoryOp = cpuConfig.IncrementIOnStoreLoadMemoryOps;
            _shiftUsesVy = cpuConfig.ShiftUsesVy;
            _clipSprites = cpuConfig.ClipSprites;
            _jumpWithV0 = cpuConfig.JumpWithV0;
            _instructionHandlers = BuildInstructionHandlers();
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

        private Dictionary<ushort, Func<DecodedInstruction, bool>> BuildInstructionHandlers()
        {
            return new Dictionary<ushort, Func<DecodedInstruction, bool>>
            {
                { Chip8InstructionSet.PatternCLS, instr => { CLS(); return false; } },
                { Chip8InstructionSet.PatternRET, instr => { RET(); return false; } },
                { Chip8InstructionSet.PatternLOW, instr => { LOW(); return false; } },
                { Chip8InstructionSet.PatternHIGH, instr => { HIGH(); return false; } },
                { Chip8InstructionSet.PatternSYS, instr => { SYS(instr.Operands.RequireNNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternJP, instr => { JP(instr.Operands.RequireNNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternCALL, instr => { CALL(instr.Operands.RequireNNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSEByte, instr => { SEBYTE(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSNEByte, instr => { SNEBYTE(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSEReg, instr => { SEREG(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDByte, instr => { LDBYTE(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternADDByte, instr => { ADDBYTE(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDReg, instr => { LDREG(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternOR, instr => { OR(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternAND, instr => { AND(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternXOR, instr => { XOR(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternADDReg, instr => { ADDREG(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSUB, instr => { SUB(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSHR, instr => { SHR(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSUBN, instr => { SUBN(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSHL, instr => { SHL(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSNEReg, instr => { SNEREG(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDI, instr => { LDI(instr.Operands.RequireNNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternJPV0, instr => { JPBase(instr.Operands.RequireNNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternRND, instr => { RND(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireNN(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternDRW, instr => { DRW(instr.Operands.RequireX(instr.Definition.Mnemonic), instr.Operands.RequireY(instr.Definition.Mnemonic), instr.Operands.RequireN(instr.Definition.Mnemonic)); return true; } },
                { Chip8InstructionSet.PatternSKP, instr => { SKP(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSKNP, instr => { SKNP(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDDT, instr => { LDDT(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDK, instr => { LDK(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSTDT, instr => { STDT(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSTST, instr => { STST(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternADDI, instr => { ADDI(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDFNT, instr => { LDFNT(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDBCD, instr => { LDBCD(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternSTREGI, instr => { STREGI(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
                { Chip8InstructionSet.PatternLDREGI, instr => { LDREGI(instr.Operands.RequireX(instr.Definition.Mnemonic)); return false; } },
            };
        }

        public CpuStepResult Step(bool allowDraw = true)
        {
            var pcBefore = _registers.PC;
            ushort opcode = ReadOpcodeAt(pcBefore);
            _traceSink?.Publish(new InstructionFetchedTraceEvent(pcBefore, opcode));

            DecodedInstruction instruction;
            try
            {
                instruction = _decoder.DecodeOrThrow(opcode);
            }
            catch (Exception ex)
            {
                _traceSink?.Publish(new InstructionFaultedTraceEvent(pcBefore, opcode, ex.Message));
                throw;
            }

            _traceSink?.Publish(new InstructionDecodedTraceEvent(
                pcBefore,
                opcode,
                instruction.Definition.Mnemonic,
                instruction.Operands));

            if (!allowDraw && instruction.Definition.Pattern == Chip8InstructionSet.PatternDRW)
            {
                var waitResult = new CpuStepResult(
                    DrewSprite: false,
                    WaitingForDrawVBlank: true);

                _traceSink?.Publish(new InstructionExecutedTraceEvent(
                    pcBefore,
                    _registers.PC,
                    opcode,
                    waitResult.DrewSprite,
                    waitResult.WaitingForDrawVBlank));
                return waitResult;
            }

            _registers.IncrementPC();
            var drewSprite = Execute(instruction);
            var stepResult = new CpuStepResult(
                DrewSprite: drewSprite,
                WaitingForDrawVBlank: false);

            _traceSink?.Publish(new InstructionExecutedTraceEvent(
                pcBefore,
                _registers.PC,
                opcode,
                stepResult.DrewSprite,
                stepResult.WaitingForDrawVBlank));

            return stepResult;
        }

        private bool Execute(DecodedInstruction instruction)
        {
            if (_instructionHandlers.TryGetValue(instruction.Definition.Pattern, out var handler))
            {
                return handler(instruction);
            }

            throw new InvalidOperationException($"Unknown opcode pattern: {instruction.Opcode:X4}");
        }

        private ushort ReadOpcodeAt(ushort pc)
        {
            byte highByte = _memoryBus.Read(pc);
            byte lowByte = _memoryBus.Read((ushort)(pc + 1));
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

        // 00FE - LOW
        private void LOW()
        {
            _display.SetResolution(64, 32);
        }

        // 00FF - HIGH
        private void HIGH()
        {
            _display.SetResolution(128, 64);
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
            // VF is set when no borrow occurs (Vx >= Vy).
            var borrow = _registers.GetV(x) >= _registers.GetV(y) ? 1 : 0;

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
            // VF is set when no borrow occurs (Vy >= Vx).
            var borrow = _registers.GetV(y) >= _registers.GetV(x) ? 1 : 0;
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

        // Bnnn - JP V0/Vx, addr
        private void JPBase(ushort nnn)
        {
            var baseRegister = _jumpWithV0
                ? (byte)0
                : (byte)((nnn & 0x0F00) >> 8);
            var target = (ushort)(nnn + _registers.GetV(baseRegister));
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
            _registers.SetV(0xF, 0);

            var baseX = _registers.GetV(x) % _display.Width;
            var baseY = _registers.GetV(y) % _display.Height;
            var spriteWidth = n == 0 && _display.Width == 128 && _display.Height == 64 ? 16 : 8;
            var spriteHeight = n == 0 && spriteWidth == 16 ? 16 : n;
            var bytesPerRow = spriteWidth / 8;

            byte[] spriteData = new byte[spriteHeight * bytesPerRow];
            for (int i = 0; i < spriteData.Length; i++)
            {
                spriteData[i] = _memoryBus.Read((ushort)(_registers.I + i));
            }

            for(int row = 0; row < spriteHeight; row++)
            {
                var targetY = baseY + row;
                if (_clipSprites && targetY >= _display.Height)
                {
                    continue;
                }

                for(int col = 0; col < spriteWidth; col++)
                {
                    var spriteByte = spriteData[(row * bytesPerRow) + (col / 8)];
                    bool pixelOn = (spriteByte & (0x80 >> (col % 8))) != 0;
                    if (pixelOn)
                    {
                        var targetX = baseX + col;
                        if (_clipSprites && targetX >= _display.Width)
                        {
                            continue;
                        }

                        bool erased = _display.XorPixel(targetX, targetY);
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





