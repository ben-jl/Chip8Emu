using Chip8Emu.Core.Assembly.Lexer;
using Chip8Emu.Core.Assembly.Diagnostics;

namespace Chip8Emu.Core.Assembly.Parser
{
    internal sealed class ChipAssemblyParser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private readonly AssemblyDiagnostics _diagnostics;
        private int _position;

        public ChipAssemblyParser(IReadOnlyList<Token> tokens, AssemblyDiagnostics diagnostics)
        {
            _tokens = tokens;
            _diagnostics = diagnostics;
            _position = 0;
        }

        public ParsedProgram Parse()
        {
            var statements = new List<ParsedStatement>();

            while (!IsAtEnd())
            {
                SkipNewlines();

                if (IsAtEnd())
                    break;

                var stmt = ParseStatement();
                if (stmt != null)
                {
                    statements.Add(stmt);
                }

                SkipNewlines();
            }

            return new ParsedProgram(statements.ToArray());
        }

        private ParsedStatement? ParseStatement()
        {
            var currentToken = CurrentToken();

            if (currentToken.Kind == TokenKind.Eof)
                return null;

            return currentToken.Kind switch
            {
                TokenKind.Identifier when Peek()?.Kind == TokenKind.Colon => ParseLabel(),
                TokenKind.Mnemonic => ParseInstruction(),
                TokenKind.Directive => ParseDirective(),
                TokenKind.Identifier => ErrorWithCode("UNKNOWN_MNEMONIC", $"Unknown mnemonic: {currentToken.Lexeme}", null),
                _ => Error($"Unexpected token: {currentToken.Lexeme}", null)
            };
        }

        private ParsedLabel ParseLabel()
        {
            var labelToken = CurrentToken();
            var labelName = labelToken.Lexeme;
            Advance();

            if (CurrentToken().Kind != TokenKind.Colon)
            {
                _diagnostics.ReportError("SYNTAX_ERROR", "Expected ':' after label", labelToken.LineNumber, labelToken.ColumnNumber);
                return new ParsedLabel(labelName, labelToken.LineNumber);
            }

            Advance();
            return new ParsedLabel(labelName, labelToken.LineNumber);
        }

        private ParsedInstruction ParseInstruction()
        {
            var mnemonicToken = CurrentToken();
            var mnemonic = (string)mnemonicToken.Value!;
            var lineNum = mnemonicToken.LineNumber;
            Advance();

            var operands = new List<ParsedOperand>();

            while (!IsAtEnd() && CurrentToken().Kind != TokenKind.Newline && CurrentToken().Kind != TokenKind.Eof)
            {
                var operand = ParseOperand();
                if (operand != null)
                {
                    operands.Add(operand);
                }

                if (CurrentToken().Kind == TokenKind.Comma)
                {
                    Advance();
                }
                else if (CurrentToken().Kind != TokenKind.Newline && CurrentToken().Kind != TokenKind.Eof)
                {
                    break;
                }
            }

            return new ParsedInstruction(mnemonic, operands.ToArray(), lineNum);
        }

        private ParsedOperand? ParseOperand()
        {
            var token = CurrentToken();

            return token.Kind switch
            {
                TokenKind.Register => ParseRegisterOperand(),
                TokenKind.DecimalNumber or TokenKind.HexNumber => ParseNumberOperand(),
                TokenKind.Identifier => ParseLabelOrConstantOperand(),
                _ => null
            };
        }

        private ParsedOperand ParseRegisterOperand()
        {
            var token = CurrentToken();
            var registerName = (string)token.Value!;
            Advance();
            return new RegisterOperand(registerName);
        }

        private ParsedOperand ParseNumberOperand()
        {
            var token = CurrentToken();
            var value = (int)token.Value!;
            Advance();
            return new NumberOperand(value);
        }

        private ParsedOperand ParseLabelOrConstantOperand()
        {
            var token = CurrentToken();
            var name = token.Lexeme;
            Advance();
            return new LabelOperand(name);
        }

        private ParsedDirective ParseDirective()
        {
            var directiveToken = CurrentToken();
            var directiveName = (string)directiveToken.Value!;
            var lineNum = directiveToken.LineNumber;
            Advance();

            var arguments = new List<string>();

            while (!IsAtEnd() && CurrentToken().Kind != TokenKind.Newline && CurrentToken().Kind != TokenKind.Eof)
            {
                if (CurrentToken().Kind == TokenKind.Identifier || CurrentToken().Kind == TokenKind.HexNumber || CurrentToken().Kind == TokenKind.DecimalNumber)
                {
                    arguments.Add(CurrentToken().Lexeme);
                    Advance();
                }
                else if (CurrentToken().Kind == TokenKind.Comma)
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }

            return new ParsedDirective(directiveName, arguments.ToArray(), lineNum);
        }

        private void SkipNewlines()
        {
            while (!IsAtEnd() && CurrentToken().Kind == TokenKind.Newline)
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (!IsAtEnd())
            {
                _position++;
            }
        }

        private Token CurrentToken()
        {
            return _position < _tokens.Count ? _tokens[_position] : _tokens[_tokens.Count - 1];
        }

        private Token? Peek()
        {
            return _position + 1 < _tokens.Count ? _tokens[_position + 1] : null;
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count || _tokens[_position].Kind == TokenKind.Eof;
        }

        private ParsedStatement? Error(string message, ParsedStatement? recovery)
        {
            return ErrorWithCode("PARSE_ERROR", message, recovery);
        }

        private ParsedStatement? ErrorWithCode(string code, string message, ParsedStatement? recovery)
        {
            var token = CurrentToken();
            _diagnostics.ReportError(code, message, token.LineNumber, token.ColumnNumber);
            SkipToNextLine();
            return recovery;
        }

        private void SkipToNextLine()
        {
            while (!IsAtEnd() && CurrentToken().Kind != TokenKind.Newline)
            {
                Advance();
            }
        }
    }
}
