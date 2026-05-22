namespace Chip8Emu.Core.Assembly.Parser
{
    public abstract record ParsedStatement(int LineNumber);

    public sealed record ParsedLabel(
        string Name,
        int LineNumber) : ParsedStatement(LineNumber);

    public sealed record ParsedInstruction(
        string Mnemonic,
        ParsedOperand[] Operands,
        int LineNumber) : ParsedStatement(LineNumber);

    public sealed record ParsedDirective(
        string Name,
        string[] Arguments,
        int LineNumber) : ParsedStatement(LineNumber);
}
