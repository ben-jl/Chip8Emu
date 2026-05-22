using Chip8Emu.Core.Assembly.Diagnostics;

namespace Chip8Emu.Core.Assembly.Codegen
{
    public sealed record AssemblyResult(
        bool Success,
        byte[]? Bytecode,
        ushort StartAddress,
        AssemblyDiagnostics Diagnostics);
}
