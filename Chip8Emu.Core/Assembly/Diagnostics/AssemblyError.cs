namespace Chip8Emu.Core.Assembly.Diagnostics
{
    public sealed record AssemblyError(
        string Code,
        string Message,
        int LineNumber,
        int ColumnNumber);
}
