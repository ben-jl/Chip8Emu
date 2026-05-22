namespace Chip8Emu.Core.Assembly.Lexer
{
    public enum TokenKind
    {
        Mnemonic,
        Register,
        DecimalNumber,
        HexNumber,
        Comma,
        LeftBracket,
        RightBracket,
        Identifier,
        Colon,
        Directive,
        Comment,
        Newline,
        Eof,
        Unknown
    }
}
