using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed record BreakpointEvaluationContext(
        ushort ProgramCounter,
        ushort Opcode,
        MachineSnapshot CurrentSnapshot,
        MachineSnapshot? PreviousSnapshot,
        IReadOnlyList<MemoryAccessSnapshot> MemoryAccesses);
}
