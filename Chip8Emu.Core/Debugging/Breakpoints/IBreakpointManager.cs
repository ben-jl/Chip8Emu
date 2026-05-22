namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public interface IBreakpointManager
    {
        IReadOnlyList<BreakpointDefinition> All { get; }

        BreakpointDefinition AddAddressBreakpoint(ushort address, bool enabled = true);
        BreakpointDefinition AddOpcodeBreakpoint(ushort opcodeValue, ushort opcodeMask = 0xFFFF, bool enabled = true);

        bool Remove(Guid id);
        bool SetEnabled(Guid id, bool enabled);

        bool TryMatch(ushort programCounter, ushort opcode, out BreakpointMatch? match);
    }
}
