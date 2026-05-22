namespace Chip8Emu.Core.Assembly.Parser
{
    public sealed record ParsedProgram(
        ParsedStatement[] Statements);
}
