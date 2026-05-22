using Chip8Emu.Core.Assembly.Diagnostics;
using Chip8Emu.Core.Assembly.Parser;
using Chip8Emu.Core.Assembly.Symbols;
using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Core.Assembly.Codegen
{
    internal sealed class InstructionEncoder
    {
        private readonly AssemblyDiagnostics _diagnostics;

        public InstructionEncoder(AssemblyDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public ushort? EncodeInstruction(string mnemonic, ParsedOperand[] operands, AssemblySymbolTable symbols, int lineNumber)
        {
            var candidates = Chip8InstructionSet.Definitions
                .Where(d => d.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count == 0)
            {
                _diagnostics.ReportError("UNKNOWN_MNEMONIC", $"Unknown mnemonic: '{mnemonic}'", lineNumber, 0);
                return null;
            }

            var best = candidates.FirstOrDefault(c => MatchesOperandTypes(c.Operands, operands));

            if (best == null)
            {
                _diagnostics.ReportError("OPERAND_MISMATCH", $"No matching variant of '{mnemonic}' for the given operands", lineNumber, 0);
                return null;
            }

            if (!ValidateSymbolRefs(operands, symbols, lineNumber))
                return null;

            var parsedOperands = BuildParsedOperands(best, operands, symbols);
            return best.Encode(parsedOperands);
        }

        private bool ValidateSymbolRefs(ParsedOperand[] operands, AssemblySymbolTable symbols, int lineNumber)
        {
            foreach (var operand in operands)
            {
                if (operand is LabelOperand label && !symbols.IsDefined(label.LabelName))
                {
                    _diagnostics.ReportError("UNDEFINED_SYMBOL", $"Undefined symbol: '{label.LabelName}'", lineNumber, 0);
                    return false;
                }
            }
            return true;
        }

        private bool MatchesOperandTypes(OperandPattern pattern, ParsedOperand[] ops) => pattern switch
        {
            OperandPattern.None              => ops.Length == 0,
            OperandPattern.Address           => ops.Length == 1 && IsNumericOrLabel(ops[0]),
            OperandPattern.Register          => ops.Length == 1 && IsVReg(ops[0]),
            OperandPattern.RegisterByte      => ops.Length == 2 && IsVReg(ops[0]) && IsNumericOrLabel(ops[1]),
            OperandPattern.RegisterRegister  => ops.Length == 2 && IsVReg(ops[0]) && IsVReg(ops[1]),
            OperandPattern.RegisterNibble    => ops.Length == 3 && IsVReg(ops[0]) && IsVReg(ops[1]) && IsNumericOrLabel(ops[2]),
            OperandPattern.V0Address         => ops.Length == 2 && IsSpecificReg(ops[0], "V0") && IsNumericOrLabel(ops[1]),
            OperandPattern.IAddress          => ops.Length == 2 && IsSpecificReg(ops[0], "I") && IsNumericOrLabel(ops[1]),
            OperandPattern.RegisterDelayTimer => ops.Length == 2 && IsVReg(ops[0]) && IsSpecificReg(ops[1], "DT"),
            OperandPattern.RegisterKey       => ops.Length == 2 && IsVReg(ops[0]) && IsSpecificReg(ops[1], "K"),
            OperandPattern.DelayTimerRegister => ops.Length == 2 && IsSpecificReg(ops[0], "DT") && IsVReg(ops[1]),
            OperandPattern.SoundTimerRegister => ops.Length == 2 && IsSpecificReg(ops[0], "ST") && IsVReg(ops[1]),
            OperandPattern.IRegister         => ops.Length == 2 && IsSpecificReg(ops[0], "I") && IsVReg(ops[1]),
            OperandPattern.FontRegister      => ops.Length == 2 && IsSpecificReg(ops[0], "F") && IsVReg(ops[1]),
            OperandPattern.BcdRegister       => ops.Length == 2 && IsSpecificReg(ops[0], "B") && IsVReg(ops[1]),
            OperandPattern.IndirectIRegister => false, // bracket syntax not in Phase 1
            OperandPattern.RegisterIndirectI => false,
            _ => false
        };

        private ParsedOperands BuildParsedOperands(InstructionDefinition def, ParsedOperand[] ops, AssemblySymbolTable symbols)
        {
            return def.Operands switch
            {
                OperandPattern.None =>
                    new ParsedOperands(),

                OperandPattern.Address =>
                    new ParsedOperands(NNN: (ushort)NumVal(ops[0], symbols)),

                OperandPattern.V0Address or OperandPattern.IAddress =>
                    new ParsedOperands(NNN: (ushort)NumVal(ops[1], symbols)),

                OperandPattern.RegisterByte =>
                    new ParsedOperands(X: VRegIdx(ops[0]), NN: (byte)NumVal(ops[1], symbols)),

                OperandPattern.RegisterRegister =>
                    new ParsedOperands(X: VRegIdx(ops[0]), Y: VRegIdx(ops[1])),

                OperandPattern.RegisterNibble =>
                    new ParsedOperands(X: VRegIdx(ops[0]), Y: VRegIdx(ops[1]), N: (byte)NumVal(ops[2], symbols)),

                OperandPattern.Register or OperandPattern.RegisterDelayTimer or OperandPattern.RegisterKey =>
                    new ParsedOperands(X: VRegIdx(ops[0])),

                OperandPattern.DelayTimerRegister or OperandPattern.SoundTimerRegister
                    or OperandPattern.IRegister or OperandPattern.FontRegister or OperandPattern.BcdRegister =>
                    new ParsedOperands(X: VRegIdx(ops[1])),

                _ => throw new InvalidOperationException($"Unsupported operand pattern: {def.Operands}")
            };
        }

        private bool IsVReg(ParsedOperand op) =>
            op is RegisterOperand r && r.RegisterName.StartsWith("V", StringComparison.OrdinalIgnoreCase);

        private bool IsSpecificReg(ParsedOperand op, string name) =>
            op is RegisterOperand r && r.RegisterName.Equals(name, StringComparison.OrdinalIgnoreCase);

        private bool IsNumericOrLabel(ParsedOperand op) =>
            op is NumberOperand or LabelOperand;

        private byte VRegIdx(ParsedOperand op)
        {
            if (op is RegisterOperand r && r.RegisterName.Length >= 2)
            {
                var suffix = r.RegisterName[1..].ToUpperInvariant();
                if (byte.TryParse(suffix, System.Globalization.NumberStyles.HexNumber, null, out var idx) && idx <= 0xF)
                    return idx;
            }
            throw new InvalidOperationException($"Expected V-register, got: {op}");
        }

        private int NumVal(ParsedOperand op, AssemblySymbolTable symbols) => op switch
        {
            NumberOperand n => n.Value,
            LabelOperand l when symbols.TryResolve(l.LabelName, out var sym) && sym != null => sym.Value,
            _ => 0
        };
    }
}
