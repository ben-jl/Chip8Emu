namespace Chip8Emu.Core.Assembly.Lexer
{
    internal sealed class ChipAssemblyLexer
    {
        private readonly string _source;
        private int _position;
        private int _line = 1;
        private int _column = 1;

        private static readonly HashSet<string> Mnemonics = new(StringComparer.OrdinalIgnoreCase)
        {
            "CLS", "RET", "SYS", "JP", "CALL", "SE", "SNE", "LD", "ADD", "OR", "AND", "XOR", "SHR", "SUBN", "SHL",
            "RND", "DRW", "SKP", "SKNP", "LD", "LDV", "LDS", "LDDT", "LDK", "LDDT", "LDST", "LDST", "ADDI", "LDF",
            "LDB", "LDI", "LDRI", "LDIR", "LOW", "HIGH", "SUB"
        };

        private static readonly HashSet<string> Registers = new(StringComparer.OrdinalIgnoreCase)
        {
            "V0", "V1", "V2", "V3", "V4", "V5", "V6", "V7",
            "V8", "V9", "VA", "VB", "VC", "VD", "VE", "VF",
            "I", "DT", "ST", "F", "K"
        };

        private static readonly HashSet<string> Directives = new(StringComparer.OrdinalIgnoreCase)
        {
            "ORG", "DEFINE", "INCLUDE"
        };

        public ChipAssemblyLexer(string source)
        {
            _source = source;
            _position = 0;
        }

        public IReadOnlyList<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (!IsAtEnd())
            {
                SkipWhitespace();

                if (IsAtEnd())
                    break;

                var token = ScanToken();
                if (token.Kind != TokenKind.Comment)
                {
                    tokens.Add(token);
                }
            }

            tokens.Add(new Token(TokenKind.Eof, "", null, _line, _column));
            return tokens;
        }

        private Token ScanToken()
        {
            var startLine = _line;
            var startColumn = _column;
            var ch = CurrentChar();

            return ch switch
            {
                ',' => Advance(new Token(TokenKind.Comma, ",", null, startLine, startColumn)),
                '[' => Advance(new Token(TokenKind.LeftBracket, "[", null, startLine, startColumn)),
                ']' => Advance(new Token(TokenKind.RightBracket, "]", null, startLine, startColumn)),
                ':' => Advance(new Token(TokenKind.Colon, ":", null, startLine, startColumn)),
                ';' => ScanComment(startLine, startColumn),
                '\n' => Advance(new Token(TokenKind.Newline, "\n", null, startLine, startColumn)),
                '0' when Peek() == 'x' || Peek() == 'X' => ScanHexNumber(startLine, startColumn),
                _ when char.IsDigit(ch) => ScanDecimalNumber(startLine, startColumn),
                _ when char.IsLetter(ch) || ch == '_' => ScanIdentifierOrKeyword(startLine, startColumn),
                _ => Advance(new Token(TokenKind.Unknown, ch.ToString(), null, startLine, startColumn))
            };
        }

        private Token ScanHexNumber(int startLine, int startColumn)
        {
            var start = _position;
            Advance(); // skip '0'
            Advance(); // skip 'x'

            while (!IsAtEnd() && IsHexDigit(CurrentChar()))
            {
                Advance();
            }

            var lexeme = _source[start.._position];
            var value = int.Parse(lexeme, System.Globalization.NumberStyles.HexNumber);
            return new Token(TokenKind.HexNumber, lexeme, value, startLine, startColumn);
        }

        private Token ScanDecimalNumber(int startLine, int startColumn)
        {
            var start = _position;

            while (!IsAtEnd() && char.IsDigit(CurrentChar()))
            {
                Advance();
            }

            var lexeme = _source[start.._position];
            var value = int.Parse(lexeme);
            return new Token(TokenKind.DecimalNumber, lexeme, value, startLine, startColumn);
        }

        private Token ScanIdentifierOrKeyword(int startLine, int startColumn)
        {
            var start = _position;

            while (!IsAtEnd() && (char.IsLetterOrDigit(CurrentChar()) || CurrentChar() == '_'))
            {
                Advance();
            }

            var lexeme = _source[start.._position];

            if (Directives.Contains(lexeme))
            {
                return new Token(TokenKind.Directive, lexeme, lexeme.ToUpperInvariant(), startLine, startColumn);
            }

            if (Mnemonics.Contains(lexeme))
            {
                return new Token(TokenKind.Mnemonic, lexeme, lexeme.ToUpperInvariant(), startLine, startColumn);
            }

            if (Registers.Contains(lexeme))
            {
                return new Token(TokenKind.Register, lexeme, lexeme.ToUpperInvariant(), startLine, startColumn);
            }

            return new Token(TokenKind.Identifier, lexeme, lexeme, startLine, startColumn);
        }

        private Token ScanComment(int startLine, int startColumn)
        {
            var start = _position;
            while (!IsAtEnd() && CurrentChar() != '\n')
            {
                Advance();
            }

            var lexeme = _source[start.._position];
            return new Token(TokenKind.Comment, lexeme, null, startLine, startColumn);
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd())
            {
                var ch = CurrentChar();
                if (ch == ' ' || ch == '\t' || ch == '\r')
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
        }

        private Token Advance(Token token)
        {
            _position++;
            _column++;
            return token;
        }

        private void Advance()
        {
            if (CurrentChar() == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }

        private char CurrentChar()
        {
            return IsAtEnd() ? '\0' : _source[_position];
        }

        private char Peek()
        {
            return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
        }

        private bool IsHexDigit(char ch)
        {
            return char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
        }

        private bool IsAtEnd()
        {
            return _position >= _source.Length;
        }
    }
}
