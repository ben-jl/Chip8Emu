namespace Chip8Emu.Core.Assembly.Symbols
{
    public sealed record Symbol(
        string Name,
        SymbolKind Kind,
        int Value,
        int DefinitionLine);
}
