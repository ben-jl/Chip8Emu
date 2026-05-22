namespace Chip8Emu.Core.Assembly.Lexer
{
    public sealed record Token(
        TokenKind Kind,
        string Lexeme,
        object? Value,
        int LineNumber,
        int ColumnNumber);
}
