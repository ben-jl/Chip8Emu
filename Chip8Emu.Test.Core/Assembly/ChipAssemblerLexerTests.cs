using Chip8Emu.Core.Assembly.Lexer;

namespace Chip8Emu.Test.Core.Assembly
{
    public class ChipAssemblerLexerTests
    {
        private static IReadOnlyList<Token> Tokenize(string source)
        {
            return new ChipAssemblyLexer(source).Tokenize();
        }

        private static IEnumerable<Token> Meaningful(IReadOnlyList<Token> tokens) =>
            tokens.Where(t => t.Kind != TokenKind.Newline && t.Kind != TokenKind.Eof);

        [Fact]
        public void Tokenize_EmptySource_ProducesOnlyEof()
        {
            var tokens = Tokenize("");

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Eof, tokens[0].Kind);
        }

        [Fact]
        public void Tokenize_Mnemonic_CLS()
        {
            var tokens = Meaningful(Tokenize("CLS")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Mnemonic, tokens[0].Kind);
            Assert.Equal("CLS", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_Mnemonic_IsCaseInsensitive()
        {
            var tokens = Meaningful(Tokenize("cls")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Mnemonic, tokens[0].Kind);
            Assert.Equal("CLS", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_VRegister_ProducesRegisterToken()
        {
            var tokens = Meaningful(Tokenize("V0")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Register, tokens[0].Kind);
            Assert.Equal("V0", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_VFRegister_ProducesRegisterToken()
        {
            var tokens = Meaningful(Tokenize("VF")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Register, tokens[0].Kind);
            Assert.Equal("VF", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_SpecialRegisters_ProduceRegisterTokens()
        {
            foreach (var reg in new[] { "I", "DT", "ST", "F", "K" })
            {
                var tokens = Meaningful(Tokenize(reg)).ToArray();
                Assert.Single(tokens);
                Assert.Equal(TokenKind.Register, tokens[0].Kind);
            }
        }

        [Fact]
        public void Tokenize_DecimalNumber_ProducesDecimalToken()
        {
            var tokens = Meaningful(Tokenize("123")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.DecimalNumber, tokens[0].Kind);
            Assert.Equal(123, tokens[0].Value);
        }

        [Fact]
        public void Tokenize_HexNumber_ProducesHexToken()
        {
            var tokens = Meaningful(Tokenize("0x1A2B")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.HexNumber, tokens[0].Kind);
            Assert.Equal(0x1A2B, tokens[0].Value);
        }

        [Fact]
        public void Tokenize_HexNumber_CaseInsensitivePrefix()
        {
            var upper = Meaningful(Tokenize("0XFF")).ToArray();
            var lower = Meaningful(Tokenize("0xff")).ToArray();

            Assert.Equal(0xFF, upper[0].Value);
            Assert.Equal(0xFF, lower[0].Value);
        }

        [Fact]
        public void Tokenize_Comment_IsDroppedFromOutput()
        {
            var tokens = Tokenize("; this is a comment").ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Eof, tokens[0].Kind);
        }

        [Fact]
        public void Tokenize_InlineComment_IsDropped()
        {
            var tokens = Meaningful(Tokenize("CLS ; clear screen")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Mnemonic, tokens[0].Kind);
        }

        [Fact]
        public void Tokenize_Label_ProducesIdentifierAndColon()
        {
            var tokens = Meaningful(Tokenize("LOOP:")).ToArray();

            Assert.Equal(2, tokens.Length);
            Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
            Assert.Equal("LOOP", tokens[0].Lexeme);
            Assert.Equal(TokenKind.Colon, tokens[1].Kind);
        }

        [Fact]
        public void Tokenize_OrgDirective_ProducesDirectiveToken()
        {
            var tokens = Meaningful(Tokenize("ORG")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Directive, tokens[0].Kind);
            Assert.Equal("ORG", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_DefineDirective_ProducesDirectiveToken()
        {
            var tokens = Meaningful(Tokenize("DEFINE")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Directive, tokens[0].Kind);
            Assert.Equal("DEFINE", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_Comma_ProducesCommaToken()
        {
            var tokens = Meaningful(Tokenize(",")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Comma, tokens[0].Kind);
        }

        [Fact]
        public void Tokenize_Newline_ProducesNewlineToken()
        {
            var tokens = Tokenize("CLS\nRET").Where(t => t.Kind == TokenKind.Newline).ToArray();

            Assert.Single(tokens);
        }

        [Fact]
        public void Tokenize_Instruction_LD_V0_Byte()
        {
            var tokens = Meaningful(Tokenize("LD V0, 0x42")).ToArray();

            Assert.Equal(4, tokens.Length);
            Assert.Equal(TokenKind.Mnemonic,      tokens[0].Kind);
            Assert.Equal(TokenKind.Register,      tokens[1].Kind);
            Assert.Equal(TokenKind.Comma,         tokens[2].Kind);
            Assert.Equal(TokenKind.HexNumber,     tokens[3].Kind);
            Assert.Equal("LD",  tokens[0].Value);
            Assert.Equal("V0",  tokens[1].Value);
            Assert.Equal(0x42,  tokens[3].Value);
        }

        [Fact]
        public void Tokenize_LineNumbersAreTracked()
        {
            var tokens = Tokenize("CLS\nRET").ToArray();

            var cls = tokens.First(t => t.Kind == TokenKind.Mnemonic && (string)t.Value! == "CLS");
            var ret = tokens.First(t => t.Kind == TokenKind.Mnemonic && (string)t.Value! == "RET");

            Assert.Equal(1, cls.LineNumber);
            Assert.Equal(2, ret.LineNumber);
        }

        [Fact]
        public void Tokenize_UnknownCharacter_ProducesUnknownToken()
        {
            var tokens = Meaningful(Tokenize("@")).ToArray();

            Assert.Single(tokens);
            Assert.Equal(TokenKind.Unknown, tokens[0].Kind);
        }
    }
}
