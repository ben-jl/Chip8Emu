using Chip8Emu.Core.Assembly.Parser;
using Chip8Emu.Core.Assembly.Symbols;
using Chip8Emu.Core.Assembly.Diagnostics;

namespace Chip8Emu.Core.Assembly.Codegen
{
    internal sealed class AssemblyCodeGenerator
    {
        private readonly AssemblyDiagnostics _diagnostics;
        private readonly InstructionEncoder _encoder;

        public AssemblyCodeGenerator(AssemblyDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
            _encoder = new InstructionEncoder();
        }

        public AssemblyResult GenerateCode(ParsedProgram program, ushort startAddress)
        {
            var symbols = new AssemblySymbolTable();
            var currentAddress = startAddress;

            FirstPass(program, symbols, ref currentAddress);

            if (_diagnostics.HasErrors)
            {
                return new AssemblyResult(
                    Success: false,
                    Bytecode: null,
                    StartAddress: startAddress,
                    Diagnostics: _diagnostics);
            }

            var bytecode = SecondPass(program, symbols, startAddress);

            return new AssemblyResult(
                Success: true,
                Bytecode: bytecode,
                StartAddress: startAddress,
                Diagnostics: _diagnostics);
        }

        private void FirstPass(ParsedProgram program, AssemblySymbolTable symbols, ref ushort currentAddress)
        {
            foreach (var statement in program.Statements)
            {
                switch (statement)
                {
                    case ParsedLabel label:
                        symbols.DefineLabel(label.Name, currentAddress, label.LineNumber);
                        break;

                    case ParsedInstruction instr:
                        currentAddress += 2;
                        break;

                    case ParsedDirective dir:
                        HandleDirective(dir, symbols, ref currentAddress);
                        break;
                }
            }
        }

        private byte[] SecondPass(ParsedProgram program, AssemblySymbolTable symbols, ushort startAddress)
        {
            var bytecode = new List<byte>();
            var currentAddress = startAddress;

            foreach (var statement in program.Statements)
            {
                switch (statement)
                {
                    case ParsedLabel:
                        break;

                    case ParsedInstruction instr:
                        try
                        {
                            var opcode = _encoder.EncodeInstruction(instr.Mnemonic, instr.Operands, symbols);
                            bytecode.Add((byte)((opcode >> 8) & 0xFF));
                            bytecode.Add((byte)(opcode & 0xFF));
                            currentAddress += 2;
                        }
                        catch (Exception ex)
                        {
                            _diagnostics.ReportError("ENCODE_ERROR", ex.Message, instr.LineNumber, 0);
                        }
                        break;

                    case ParsedDirective:
                        break;
                }
            }

            return bytecode.ToArray();
        }

        private void HandleDirective(ParsedDirective directive, AssemblySymbolTable symbols, ref ushort currentAddress)
        {
            switch (directive.Name.ToUpperInvariant())
            {
                case "ORG":
                    if (directive.Arguments.Length > 0 && ushort.TryParse(
                        directive.Arguments[0].StartsWith("0x") || directive.Arguments[0].StartsWith("0X")
                            ? directive.Arguments[0][2..]
                            : directive.Arguments[0],
                        System.Globalization.NumberStyles.HexNumber,
                        null,
                        out var addr))
                    {
                        currentAddress = addr;
                    }
                    break;

                case "DEFINE":
                    if (directive.Arguments.Length >= 2 &&
                        int.TryParse(
                            directive.Arguments[1].StartsWith("0x") || directive.Arguments[1].StartsWith("0X")
                                ? directive.Arguments[1][2..]
                                : directive.Arguments[1],
                            System.Globalization.NumberStyles.HexNumber,
                            null,
                            out var value))
                    {
                        symbols.DefineConstant(directive.Arguments[0], value, directive.LineNumber);
                    }
                    break;

                case "INCLUDE":
                    _diagnostics.ReportError("NOT_IMPLEMENTED", "INCLUDE directive not yet supported", directive.LineNumber, 0);
                    break;
            }
        }
    }
}
