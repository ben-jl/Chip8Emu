using Chip8Emu.Core.Assembly.Parser;
using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Core.Assembly.Codegen
{
    internal sealed class InstructionEncoder
    {
        public ushort EncodeInstruction(string mnemonic, ParsedOperand[] operands, Symbols.AssemblySymbolTable symbols)
        {
            var instrDef = Chip8InstructionSet.Definitions
                .FirstOrDefault(i => i.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase));

            if (instrDef == null)
            {
                throw new InvalidOperationException($"Unknown mnemonic: {mnemonic}");
            }

            var parsedOperands = BuildParsedOperands(operands, symbols);
            return instrDef.Encode(parsedOperands);
        }

        private ParsedOperands BuildParsedOperands(ParsedOperand[] operands, Symbols.AssemblySymbolTable symbols)
        {
            byte? x = null;
            byte? y = null;
            byte? n = null;
            byte? nn = null;
            ushort? nnn = null;

            for (int i = 0; i < operands.Length; i++)
            {
                var value = ExtractOperandValue(operands[i], symbols);

                switch (i)
                {
                    case 0:
                        x = (byte)value;
                        break;
                    case 1:
                        y = (byte)value;
                        break;
                    case 2:
                        n = (byte)value;
                        break;
                    case 3:
                        nn = (byte)value;
                        break;
                    case 4:
                        nnn = (ushort)value;
                        break;
                    default:
                        throw new InvalidOperationException($"Too many operands at index {i}");
                }
            }

            return new ParsedOperands(x, y, n, nn, nnn);
        }

        private int ExtractOperandValue(ParsedOperand operand, Symbols.AssemblySymbolTable symbols)
        {
            return operand switch
            {
                RegisterOperand reg => ParseRegister(reg.RegisterName),
                NumberOperand num => num.Value,
                LabelOperand label => symbols.TryResolve(label.LabelName, out var sym) && sym != null ? sym.Value : 0,
                _ => throw new InvalidOperationException($"Unknown operand type: {operand.GetType().Name}")
            };
        }

        private int ParseRegister(string registerName)
        {
            return registerName.ToUpperInvariant() switch
            {
                "V0" => 0x0,
                "V1" => 0x1,
                "V2" => 0x2,
                "V3" => 0x3,
                "V4" => 0x4,
                "V5" => 0x5,
                "V6" => 0x6,
                "V7" => 0x7,
                "V8" => 0x8,
                "V9" => 0x9,
                "VA" => 0xA,
                "VB" => 0xB,
                "VC" => 0xC,
                "VD" => 0xD,
                "VE" => 0xE,
                "VF" => 0xF,
                "I" => 0x0,
                "DT" => 0x0,
                "ST" => 0x0,
                "F" => 0x0,
                "K" => 0x0,
                _ => throw new InvalidOperationException($"Unknown register: {registerName}")
            };
        }
    }
}
