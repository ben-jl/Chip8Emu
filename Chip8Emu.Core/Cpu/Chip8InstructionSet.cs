using System;
using System.Collections.Generic;

namespace Chip8Emu.Core.Cpu
{
    internal static class Chip8InstructionSet
    {
        public const ushort PatternCLS = 0x00E0;
        public const ushort PatternRET = 0x00EE;
        public const ushort PatternLOW = 0x00FE;
        public const ushort PatternHIGH = 0x00FF;
        public const ushort PatternSYS = 0x0000;
        public const ushort PatternJP = 0x1000;
        public const ushort PatternCALL = 0x2000;
        public const ushort PatternSEByte = 0x3000;
        public const ushort PatternSNEByte = 0x4000;
        public const ushort PatternSEReg = 0x5000;
        public const ushort PatternLDByte = 0x6000;
        public const ushort PatternADDByte = 0x7000;
        public const ushort PatternLDReg = 0x8000;
        public const ushort PatternOR = 0x8001;
        public const ushort PatternAND = 0x8002;
        public const ushort PatternXOR = 0x8003;
        public const ushort PatternADDReg = 0x8004;
        public const ushort PatternSUB = 0x8005;
        public const ushort PatternSHR = 0x8006;
        public const ushort PatternSUBN = 0x8007;
        public const ushort PatternSHL = 0x800E;
        public const ushort PatternSNEReg = 0x9000;
        public const ushort PatternLDI = 0xA000;
        public const ushort PatternJPV0 = 0xB000;
        public const ushort PatternRND = 0xC000;
        public const ushort PatternDRW = 0xD000;
        public const ushort PatternSKP = 0xE09E;
        public const ushort PatternSKNP = 0xE0A1;
        public const ushort PatternLDDT = 0xF007;
        public const ushort PatternLDK = 0xF00A;
        public const ushort PatternSTDT = 0xF015;
        public const ushort PatternSTST = 0xF018;
        public const ushort PatternADDI = 0xF01E;
        public const ushort PatternLDFNT = 0xF029;
        public const ushort PatternLDBCD = 0xF033;
        public const ushort PatternSTREGI = 0xF055;
        public const ushort PatternLDREGI = 0xF065;

        private static readonly IReadOnlyList<InstructionDefinition> s_definitions = CreateDefinitions();

        public static IReadOnlyList<InstructionDefinition> Definitions => s_definitions;

        private static IReadOnlyList<InstructionDefinition> CreateDefinitions()
        {
            return new List<InstructionDefinition>
            {
                new("CLS", 0xFFFF, PatternCLS, OperandPattern.None, EncodeNone(PatternCLS), DecodeNone()),
                new("RET", 0xFFFF, PatternRET, OperandPattern.None, EncodeNone(PatternRET), DecodeNone()),
                new("LOW", 0xFFFF, PatternLOW, OperandPattern.None, EncodeNone(PatternLOW), DecodeNone()),
                new("HIGH", 0xFFFF, PatternHIGH, OperandPattern.None, EncodeNone(PatternHIGH), DecodeNone()),
                new("SYS", 0xF000, PatternSYS, OperandPattern.Address, EncodeAddress("SYS", PatternSYS), DecodeAddress()),
                new("JP", 0xF000, PatternJP, OperandPattern.Address, EncodeAddress("JP", PatternJP), DecodeAddress()),
                new("CALL", 0xF000, PatternCALL, OperandPattern.Address, EncodeAddress("CALL", PatternCALL), DecodeAddress()),
                new("SE", 0xF000, PatternSEByte, OperandPattern.RegisterByte, EncodeXByte("SE", PatternSEByte), DecodeXByte()),
                new("SNE", 0xF000, PatternSNEByte, OperandPattern.RegisterByte, EncodeXByte("SNE", PatternSNEByte), DecodeXByte()),
                new("SE", 0xF00F, PatternSEReg, OperandPattern.RegisterRegister, EncodeXY("SE", PatternSEReg), DecodeXY()),
                new("LD", 0xF000, PatternLDByte, OperandPattern.RegisterByte, EncodeXByte("LD", PatternLDByte), DecodeXByte()),
                new("ADD", 0xF000, PatternADDByte, OperandPattern.RegisterByte, EncodeXByte("ADD", PatternADDByte), DecodeXByte()),
                new("LD", 0xF00F, PatternLDReg, OperandPattern.RegisterRegister, EncodeXY("LD", PatternLDReg), DecodeXY()),
                new("OR", 0xF00F, PatternOR, OperandPattern.RegisterRegister, EncodeXY("OR", PatternOR), DecodeXY()),
                new("AND", 0xF00F, PatternAND, OperandPattern.RegisterRegister, EncodeXY("AND", PatternAND), DecodeXY()),
                new("XOR", 0xF00F, PatternXOR, OperandPattern.RegisterRegister, EncodeXY("XOR", PatternXOR), DecodeXY()),
                new("ADD", 0xF00F, PatternADDReg, OperandPattern.RegisterRegister, EncodeXY("ADD", PatternADDReg), DecodeXY()),
                new("SUB", 0xF00F, PatternSUB, OperandPattern.RegisterRegister, EncodeXY("SUB", PatternSUB), DecodeXY()),
                new("SHR", 0xF00F, PatternSHR, OperandPattern.RegisterRegister, EncodeXY("SHR", PatternSHR), DecodeXY()),
                new("SUBN", 0xF00F, PatternSUBN, OperandPattern.RegisterRegister, EncodeXY("SUBN", PatternSUBN), DecodeXY()),
                new("SHL", 0xF00F, PatternSHL, OperandPattern.RegisterRegister, EncodeXY("SHL", PatternSHL), DecodeXY()),
                new("SNE", 0xF00F, PatternSNEReg, OperandPattern.RegisterRegister, EncodeXY("SNE", PatternSNEReg), DecodeXY()),
                new("LD", 0xF000, PatternLDI, OperandPattern.IAddress, EncodeAddress("LD", PatternLDI), DecodeAddress()),
                new("JP", 0xF000, PatternJPV0, OperandPattern.V0Address, EncodeAddress("JP", PatternJPV0), DecodeAddress()),
                new("RND", 0xF000, PatternRND, OperandPattern.RegisterByte, EncodeXByte("RND", PatternRND), DecodeXByte()),
                new("DRW", 0xF000, PatternDRW, OperandPattern.RegisterNibble, EncodeXYN("DRW", PatternDRW), DecodeXYN()),
                new("SKP", 0xF0FF, PatternSKP, OperandPattern.Register, EncodeX("SKP", PatternSKP), DecodeX()),
                new("SKNP", 0xF0FF, PatternSKNP, OperandPattern.Register, EncodeX("SKNP", PatternSKNP), DecodeX()),
                new("LD", 0xF0FF, PatternLDDT, OperandPattern.RegisterDelayTimer, EncodeX("LD", PatternLDDT), DecodeX()),
                new("LD", 0xF0FF, PatternLDK, OperandPattern.RegisterKey, EncodeX("LD", PatternLDK), DecodeX()),
                new("LD", 0xF0FF, PatternSTDT, OperandPattern.DelayTimerRegister, EncodeX("LD", PatternSTDT), DecodeX()),
                new("LD", 0xF0FF, PatternSTST, OperandPattern.SoundTimerRegister, EncodeX("LD", PatternSTST), DecodeX()),
                new("ADD", 0xF0FF, PatternADDI, OperandPattern.IRegister, EncodeX("ADD", PatternADDI), DecodeX()),
                new("LD", 0xF0FF, PatternLDFNT, OperandPattern.FontRegister, EncodeX("LD", PatternLDFNT), DecodeX()),
                new("LD", 0xF0FF, PatternLDBCD, OperandPattern.BcdRegister, EncodeX("LD", PatternLDBCD), DecodeX()),
                new("LD", 0xF0FF, PatternSTREGI, OperandPattern.IndirectIRegister, EncodeX("LD", PatternSTREGI), DecodeX()),
                new("LD", 0xF0FF, PatternLDREGI, OperandPattern.RegisterIndirectI, EncodeX("LD", PatternLDREGI), DecodeX())
            };
        }

        private static Func<ParsedOperands, ushort> EncodeNone(ushort pattern)
        {
            return _ => pattern;
        }

        private static Func<ParsedOperands, ushort> EncodeAddress(string mnemonic, ushort pattern)
        {
            return operands =>
            {
                var nnn = operands.RequireNNN(mnemonic);
                return (ushort)(pattern | nnn);
            };
        }

        private static Func<ParsedOperands, ushort> EncodeXByte(string mnemonic, ushort pattern)
        {
            return operands =>
            {
                var x = RequireRegister(operands.RequireX(mnemonic), nameof(operands.X), mnemonic);
                var nn = operands.RequireNN(mnemonic);
                return (ushort)(pattern | (x << 8) | nn);
            };
        }

        private static Func<ParsedOperands, ushort> EncodeXY(string mnemonic, ushort pattern)
        {
            return operands =>
            {
                var x = RequireRegister(operands.RequireX(mnemonic), nameof(operands.X), mnemonic);
                var y = RequireRegister(operands.RequireY(mnemonic), nameof(operands.Y), mnemonic);
                return (ushort)(pattern | (x << 8) | (y << 4));
            };
        }

        private static Func<ParsedOperands, ushort> EncodeXYN(string mnemonic, ushort pattern)
        {
            return operands =>
            {
                var x = RequireRegister(operands.RequireX(mnemonic), nameof(operands.X), mnemonic);
                var y = RequireRegister(operands.RequireY(mnemonic), nameof(operands.Y), mnemonic);
                var n = operands.RequireN(mnemonic);
                return (ushort)(pattern | (x << 8) | (y << 4) | n);
            };
        }

        private static Func<ParsedOperands, ushort> EncodeX(string mnemonic, ushort pattern)
        {
            return operands =>
            {
                var x = RequireRegister(operands.RequireX(mnemonic), nameof(operands.X), mnemonic);
                return (ushort)(pattern | (x << 8));
            };
        }

        private static Func<ushort, DecodedOperands> DecodeNone()
        {
            return _ => DecodedOperands.Empty;
        }

        private static Func<ushort, DecodedOperands> DecodeAddress()
        {
            return opcode => new DecodedOperands(NNN: ExtractNNN(opcode));
        }

        private static Func<ushort, DecodedOperands> DecodeXByte()
        {
            return opcode => new DecodedOperands(X: ExtractX(opcode), NN: ExtractNN(opcode));
        }

        private static Func<ushort, DecodedOperands> DecodeXY()
        {
            return opcode => new DecodedOperands(X: ExtractX(opcode), Y: ExtractY(opcode));
        }

        private static Func<ushort, DecodedOperands> DecodeXYN()
        {
            return opcode => new DecodedOperands(X: ExtractX(opcode), Y: ExtractY(opcode), N: ExtractN(opcode));
        }

        private static Func<ushort, DecodedOperands> DecodeX()
        {
            return opcode => new DecodedOperands(X: ExtractX(opcode));
        }

        private static byte ExtractX(ushort opcode)
        {
            return (byte)((opcode & 0x0F00) >> 8);
        }

        private static byte ExtractY(ushort opcode)
        {
            return (byte)((opcode & 0x00F0) >> 4);
        }

        private static byte ExtractN(ushort opcode)
        {
            return (byte)(opcode & 0x000F);
        }

        private static byte ExtractNN(ushort opcode)
        {
            return (byte)(opcode & 0x00FF);
        }

        private static ushort ExtractNNN(ushort opcode)
        {
            return (ushort)(opcode & 0x0FFF);
        }

        private static byte RequireRegister(byte register, string operandName, string mnemonic)
        {
            if (register > 0xF)
            {
                throw new ArgumentOutOfRangeException(operandName, $"Operand {operandName} must be a CHIP-8 register (0x0-0xF) for {mnemonic}.");
            }

            return register;
        }
    }
}
