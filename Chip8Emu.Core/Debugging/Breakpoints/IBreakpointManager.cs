namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public interface IBreakpointManager
    {
        IReadOnlyList<BreakpointDefinition> All { get; }

        BreakpointDefinition AddAddressBreakpoint(ushort address, bool enabled = true);
        BreakpointDefinition AddOpcodeBreakpoint(ushort opcodeValue, ushort opcodeMask = 0xFFFF, bool enabled = true);
        BreakpointDefinition AddMemoryBreakpoint(
            ushort startAddress,
            ushort endAddress,
            bool breakOnRead = true,
            bool breakOnWrite = true,
            bool enabled = true);
        BreakpointDefinition AddRegisterEqualsBreakpoint(string registerName, ushort value, bool enabled = true);
        BreakpointDefinition AddRegisterChangedBreakpoint(string registerName, bool enabled = true);
        BreakpointDefinition AddConditionalBreakpoint(string expression, bool enabled = true);

        bool Remove(Guid id);
        bool SetEnabled(Guid id, bool enabled);

        bool TryMatch(ushort programCounter, ushort opcode, out BreakpointMatch? match);
        bool TryMatch(BreakpointEvaluationContext context, out BreakpointMatch? match);
    }
}
