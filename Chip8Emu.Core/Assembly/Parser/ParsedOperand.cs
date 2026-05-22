namespace Chip8Emu.Core.Assembly.Parser
{
    public abstract record ParsedOperand;

    public sealed record RegisterOperand(string RegisterName) : ParsedOperand;

    public sealed record NumberOperand(int Value) : ParsedOperand;

    public sealed record LabelOperand(string LabelName) : ParsedOperand;
}
