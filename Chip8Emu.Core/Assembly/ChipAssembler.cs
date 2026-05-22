using Chip8Emu.Core.Assembly.Lexer;
using Chip8Emu.Core.Assembly.Parser;
using Chip8Emu.Core.Assembly.Codegen;
using Chip8Emu.Core.Assembly.Diagnostics;

namespace Chip8Emu.Core.Assembly
{
    public sealed class ChipAssembler
    {
        public AssemblyResult Assemble(string sourceCode, ushort startAddress = 0x200)
        {
            var diagnostics = new AssemblyDiagnostics();

            var lexer = new ChipAssemblyLexer(sourceCode);
            var tokens = lexer.Tokenize();

            var parser = new ChipAssemblyParser(tokens, diagnostics);
            var program = parser.Parse();

            if (diagnostics.HasErrors)
            {
                return new AssemblyResult(
                    Success: false,
                    Bytecode: null,
                    StartAddress: startAddress,
                    Diagnostics: diagnostics);
            }

            var codeGen = new AssemblyCodeGenerator(diagnostics);
            var result = codeGen.GenerateCode(program, startAddress);

            return result;
        }

        public AssemblyResult AssembleFile(string filePath, ushort startAddress = 0x200)
        {
            try
            {
                var sourceCode = File.ReadAllText(filePath);
                return Assemble(sourceCode, startAddress);
            }
            catch (Exception ex)
            {
                var diagnostics = new AssemblyDiagnostics();
                diagnostics.ReportError("FILE_ERROR", $"Failed to read file: {ex.Message}", 0, 0);

                return new AssemblyResult(
                    Success: false,
                    Bytecode: null,
                    StartAddress: startAddress,
                    Diagnostics: diagnostics);
            }
        }
    }
}
