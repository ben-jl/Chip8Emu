using Chip8Emu.Core.Assembly.Diagnostics;
using Chip8Emu.Core.Assembly.Lexer;
using Chip8Emu.Core.Assembly.Parser;

namespace Chip8Emu.Test.Core.Assembly
{
    public class ChipAssemblerParserTests
    {
        private static ParsedProgram Parse(string source)
        {
            var tokens = new ChipAssemblyLexer(source).Tokenize();
            var diagnostics = new AssemblyDiagnostics();
            return new ChipAssemblyParser(tokens, diagnostics).Parse();
        }

        [Fact]
        public void Parse_EmptySource_ProducesNoStatements()
        {
            var program = Parse("");

            Assert.Empty(program.Statements);
        }

        [Fact]
        public void Parse_CommentOnly_ProducesNoStatements()
        {
            var program = Parse("; just a comment");

            Assert.Empty(program.Statements);
        }

        [Fact]
        public void Parse_Label_ProducesParsedLabel()
        {
            var program = Parse("LOOP:");

            var label = Assert.Single(program.Statements);
            var parsedLabel = Assert.IsType<ParsedLabel>(label);
            Assert.Equal("LOOP", parsedLabel.Name);
        }

        [Fact]
        public void Parse_NoOperandInstruction_ProducesEmptyOperands()
        {
            var program = Parse("CLS");

            var stmt = Assert.Single(program.Statements);
            var instr = Assert.IsType<ParsedInstruction>(stmt);
            Assert.Equal("CLS", instr.Mnemonic);
            Assert.Empty(instr.Operands);
        }

        [Fact]
        public void Parse_AddressInstruction_ProducesNumberOperand()
        {
            var program = Parse("JP 0x200");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            Assert.Equal("JP", instr.Mnemonic);
            var operand = Assert.IsType<NumberOperand>(Assert.Single(instr.Operands));
            Assert.Equal(0x200, operand.Value);
        }

        [Fact]
        public void Parse_RegisterByteInstruction_ProducesRegisterAndNumberOperands()
        {
            var program = Parse("LD V5, 0x42");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            Assert.Equal("LD", instr.Mnemonic);
            Assert.Equal(2, instr.Operands.Length);
            var reg = Assert.IsType<RegisterOperand>(instr.Operands[0]);
            var num = Assert.IsType<NumberOperand>(instr.Operands[1]);
            Assert.Equal("V5", reg.RegisterName);
            Assert.Equal(0x42, num.Value);
        }

        [Fact]
        public void Parse_RegisterRegisterInstruction_ProducesTwoRegisterOperands()
        {
            var program = Parse("OR V1, V2");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            Assert.Equal("OR", instr.Mnemonic);
            var x = Assert.IsType<RegisterOperand>(instr.Operands[0]);
            var y = Assert.IsType<RegisterOperand>(instr.Operands[1]);
            Assert.Equal("V1", x.RegisterName);
            Assert.Equal("V2", y.RegisterName);
        }

        [Fact]
        public void Parse_ThreeOperandInstruction_DRW()
        {
            var program = Parse("DRW V0, VA, 5");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            Assert.Equal("DRW", instr.Mnemonic);
            Assert.Equal(3, instr.Operands.Length);
            Assert.IsType<RegisterOperand>(instr.Operands[0]);
            Assert.IsType<RegisterOperand>(instr.Operands[1]);
            var n = Assert.IsType<NumberOperand>(instr.Operands[2]);
            Assert.Equal(5, n.Value);
        }

        [Fact]
        public void Parse_LabelReferenceOperand_ProducesLabelOperand()
        {
            var program = Parse("JP LOOP");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            var operand = Assert.IsType<LabelOperand>(Assert.Single(instr.Operands));
            Assert.Equal("LOOP", operand.LabelName);
        }

        [Fact]
        public void Parse_IAddressLD_ProducesRegisterAndNumberOperands()
        {
            var program = Parse("LD I, 0x300");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            Assert.Equal("LD", instr.Mnemonic);
            var reg = Assert.IsType<RegisterOperand>(instr.Operands[0]);
            Assert.Equal("I", reg.RegisterName);
            var num = Assert.IsType<NumberOperand>(instr.Operands[1]);
            Assert.Equal(0x300, num.Value);
        }

        [Fact]
        public void Parse_OrgDirective_ProducesParsedDirective()
        {
            var program = Parse("ORG 0x200");

            var stmt = Assert.Single(program.Statements);
            var dir = Assert.IsType<ParsedDirective>(stmt);
            Assert.Equal("ORG", dir.Name);
            Assert.Equal("0x200", dir.Arguments[0]);
        }

        [Fact]
        public void Parse_DefineDirective_ProducesParsedDirective()
        {
            var program = Parse("DEFINE PADDLE_X 0x10");

            var stmt = Assert.Single(program.Statements);
            var dir = Assert.IsType<ParsedDirective>(stmt);
            Assert.Equal("DEFINE", dir.Name);
            Assert.Equal(2, dir.Arguments.Length);
            Assert.Equal("PADDLE_X", dir.Arguments[0]);
            Assert.Equal("0x10", dir.Arguments[1]);
        }

        [Fact]
        public void Parse_MultiLineProgram_ProducesAllStatements()
        {
            var source = """
                ORG 0x200
                LOOP:
                    CLS
                    LD V0, 0x01
                    JP LOOP
                """;

            var program = Parse(source);

            Assert.Equal(5, program.Statements.Length);
            Assert.IsType<ParsedDirective>(program.Statements[0]);
            Assert.IsType<ParsedLabel>(program.Statements[1]);
            Assert.IsType<ParsedInstruction>(program.Statements[2]);
            Assert.IsType<ParsedInstruction>(program.Statements[3]);
            Assert.IsType<ParsedInstruction>(program.Statements[4]);
        }

        [Fact]
        public void Parse_CommentsAreIgnored_BetweenInstructions()
        {
            var source = """
                CLS
                ; a comment
                RET
                """;

            var program = Parse(source);

            Assert.Equal(2, program.Statements.Length);
            Assert.All(program.Statements, s => Assert.IsType<ParsedInstruction>(s));
        }

        [Fact]
        public void Parse_DecimalNumber_PreservesValue()
        {
            var program = Parse("LD V0, 42");

            var instr = Assert.IsType<ParsedInstruction>(Assert.Single(program.Statements));
            var num = Assert.IsType<NumberOperand>(instr.Operands[1]);
            Assert.Equal(42, num.Value);
        }

        [Fact]
        public void Parse_LineNumbers_AreTracked()
        {
            var source = "CLS\nRET";
            var program = Parse(source);

            Assert.Equal(1, program.Statements[0].LineNumber);
            Assert.Equal(2, program.Statements[1].LineNumber);
        }
    }
}
