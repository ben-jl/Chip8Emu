using Chip8Emu.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Cpu
{
    internal sealed class Cpu
    {
        private readonly Registers _registers;
        private readonly IMemoryBus _memoryBus;
        private readonly InstructionDecoder _decoder;
        public Cpu(IMemoryBus memoryBus, MemoryMap memoryMap)
        {
            _memoryBus = memoryBus;
            _registers = new Registers(memoryMap);
            _decoder = new InstructionDecoder();
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
            throw new NotImplementedException();
        }

        // 00EE - RET
        private void RET()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        // 3xkk - SE Vx, byte
        private void SEBYTE(byte x, byte kk)
        {
            throw new NotImplementedException();
        }

        // 4xkk - SNE Vx, byte
        private void SNEBYTE(byte x, byte kk)
        {
            throw new NotImplementedException();
        }

        // 5xy0 - SE Vx, Vy
        private void SEREG(byte x, byte y)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        // 8xy1 - OR Vx, Vy
        private void OR(byte x, byte y)
        {
            throw new NotImplementedException();
        }

        // 8xy2 - AND Vx, Vy
        private void AND(byte x, byte y)
        {
            throw new NotImplementedException();
        }

        // 8xy3 - XOR Vx, Vy
        private void XOR(byte x, byte y)
        {
            throw new NotImplementedException();
        }

        // 8xy4 - ADD Vx, Vy
        private void ADDREG(byte x, byte y)
        {
            throw new NotImplementedException();
        }

        // 8xy5 - SUB Vx, Vy
        private void SUB(byte x, byte y)
        {
            throw new NotImplementedException();
        }

        // 8xy6 - SHR Vx {, Vy}
        private void SHR(byte x)
        {
            throw new NotImplementedException();
        }

        // 8xy7 - SUBN Vx, Vy
        private void SUBN(byte x, byte y)
        {
            throw new NotImplementedException();
        }

        // 8xyE - SHL Vx {, Vy}
        private void SHL(byte x)
        {
            throw new NotImplementedException();
        }

        // 9xy0 - SNE Vx, Vy
        private void SNEREG(byte x, byte y)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        // Cxkk - RND Vx, byte
        private void RND(byte x, byte kk)
        {
            throw new NotImplementedException();
        }

        // Dxyn - DRW Vx, Vy, nibble
        private void DRW(byte x, byte y, byte n)
        {
            throw new NotImplementedException();
        }

        // ExE9E - SKP Vx
        private void SKP(byte x)
        {
            throw new NotImplementedException();
        }

        // ExA1 - SKNP Vx
        private void SKNP(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx07 - LD Vx, DT
        private void LDDT(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx0A - LD Vx, K
        private void LDK(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx15 - LD DT, Vx
        private void STDT(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx18 - LD ST, Vx
        private void STST(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx1E - ADD I, Vx
        private void ADDI(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx29 - LD F, Vx
        private void LDFNT(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx33 - LD B, Vx
        private void LDBCD(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx55 - LD [I], Vx
        private void STREGI(byte x)
        {
            throw new NotImplementedException();
        }

        // Fx65 - LD Vx, [I]
        private void LDREGI(byte x)
        {
            throw new NotImplementedException();
        }
    }
}
