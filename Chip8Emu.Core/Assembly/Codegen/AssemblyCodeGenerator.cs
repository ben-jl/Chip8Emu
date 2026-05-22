using System.Globalization;
using Chip8Emu.Core.Assembly.Diagnostics;
using Chip8Emu.Core.Assembly.Parser;
using Chip8Emu.Core.Assembly.Symbols;

namespace Chip8Emu.Core.Assembly.Codegen
{
    internal sealed class AssemblyCodeGenerator
    {
        private readonly AssemblyDiagnostics _diagnostics;

        public AssemblyCodeGenerator(AssemblyDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public AssemblyResult GenerateCode(ParsedProgram program, ushort defaultStartAddress)
        {
            var symbols = new AssemblySymbolTable();
            ushort startAddress = defaultStartAddress;

            FirstPass(program, symbols, ref startAddress);

            if (_diagnostics.HasErrors)
            {
                return new AssemblyResult(
                    Success: false,
                    Bytecode: null,
                    StartAddress: startAddress,
                    Diagnostics: _diagnostics);
            }

            var bytecode = SecondPass(program, symbols);

            return new AssemblyResult(
                Success: !_diagnostics.HasErrors,
                Bytecode: _diagnostics.HasErrors ? null : bytecode,
                StartAddress: startAddress,
                Diagnostics: _diagnostics);
        }

        private void FirstPass(ParsedProgram program, AssemblySymbolTable symbols, ref ushort startAddress)
        {
            var currentAddress = startAddress;
            var orgApplied = false;

            foreach (var statement in program.Statements)
            {
                switch (statement)
                {
                    case ParsedLabel label:
                        symbols.DefineLabel(label.Name, currentAddress, label.LineNumber);
                        break;

                    case ParsedInstruction:
                        currentAddress += 2;
                        break;

                    case ParsedDirective dir:
                        HandleDirectiveFirstPass(dir, symbols, ref currentAddress, ref startAddress, ref orgApplied);
                        break;
                }
            }
        }

        private byte[] SecondPass(ParsedProgram program, AssemblySymbolTable symbols)
        {
            var bytecode = new List<byte>();
            var encoder = new InstructionEncoder(_diagnostics);

            foreach (var statement in program.Statements)
            {
                if (statement is not ParsedInstruction instr)
                    continue;

                var opcode = encoder.EncodeInstruction(instr.Mnemonic, instr.Operands, symbols, instr.LineNumber);
                if (opcode.HasValue)
                {
                    bytecode.Add((byte)((opcode.Value >> 8) & 0xFF));
                    bytecode.Add((byte)(opcode.Value & 0xFF));
                }
            }

            return bytecode.ToArray();
        }

        private void HandleDirectiveFirstPass(
            ParsedDirective directive,
            AssemblySymbolTable symbols,
            ref ushort currentAddress,
            ref ushort startAddress,
            ref bool orgApplied)
        {
            switch (directive.Name.ToUpperInvariant())
            {
                case "ORG":
                    if (directive.Arguments.Length > 0 && TryParseInteger(directive.Arguments[0], out var addr))
                    {
                        currentAddress = (ushort)addr;
                        if (!orgApplied)
                        {
                            startAddress = currentAddress;
                            orgApplied = true;
                        }
                    }
                    else
                    {
                        _diagnostics.ReportError("INVALID_DIRECTIVE", "ORG requires a valid address argument", directive.LineNumber, 0);
                    }
                    break;

                case "DEFINE":
                    if (directive.Arguments.Length >= 2 && TryParseInteger(directive.Arguments[1], out var value))
                    {
                        symbols.DefineConstant(directive.Arguments[0], value, directive.LineNumber);
                    }
                    else
                    {
                        _diagnostics.ReportError("INVALID_DIRECTIVE", "DEFINE requires a name and a numeric value", directive.LineNumber, 0);
                    }
                    break;

                case "INCLUDE":
                    _diagnostics.ReportError("NOT_IMPLEMENTED", "INCLUDE directive is not yet supported", directive.LineNumber, 0);
                    break;
            }
        }

        private static bool TryParseInteger(string s, out int value)
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
