using System.Collections.ObjectModel;

namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed class BreakpointManager : IBreakpointManager
    {
        private readonly List<BreakpointDefinition> _breakpoints = new();

        public IReadOnlyList<BreakpointDefinition> All => new ReadOnlyCollection<BreakpointDefinition>(_breakpoints);

        public BreakpointDefinition AddAddressBreakpoint(ushort address, bool enabled = true)
        {
            var breakpoint = BreakpointDefinition.CreateAddress(address, enabled);
            _breakpoints.Add(breakpoint);
            return breakpoint;
        }

        public BreakpointDefinition AddOpcodeBreakpoint(ushort opcodeValue, ushort opcodeMask = 0xFFFF, bool enabled = true)
        {
            var breakpoint = BreakpointDefinition.CreateOpcode(opcodeValue, opcodeMask, enabled);
            _breakpoints.Add(breakpoint);
            return breakpoint;
        }

        public bool Remove(Guid id)
        {
            var index = _breakpoints.FindIndex(b => b.Id == id);
            if (index < 0)
            {
                return false;
            }

            _breakpoints.RemoveAt(index);
            return true;
        }

        public bool SetEnabled(Guid id, bool enabled)
        {
            var index = _breakpoints.FindIndex(b => b.Id == id);
            if (index < 0)
            {
                return false;
            }

            _breakpoints[index] = _breakpoints[index] with { Enabled = enabled };
            return true;
        }

        public bool TryMatch(ushort programCounter, ushort opcode, out BreakpointMatch? match)
        {
            foreach (var breakpoint in _breakpoints)
            {
                if (!breakpoint.Enabled)
                {
                    continue;
                }

                if (breakpoint.Kind == BreakpointKind.Address && breakpoint.Address.HasValue)
                {
                    if (breakpoint.Address.Value == programCounter)
                    {
                        match = new BreakpointMatch(
                            breakpoint.Id,
                            BreakpointKind.Address,
                            programCounter,
                            opcode,
                            $"Address breakpoint at 0x{programCounter:X3}");
                        return true;
                    }

                    continue;
                }

                if (breakpoint.Kind == BreakpointKind.Opcode &&
                    breakpoint.OpcodeMask.HasValue &&
                    breakpoint.OpcodeValue.HasValue)
                {
                    var masked = (ushort)(opcode & breakpoint.OpcodeMask.Value);
                    if (masked == breakpoint.OpcodeValue.Value)
                    {
                        match = new BreakpointMatch(
                            breakpoint.Id,
                            BreakpointKind.Opcode,
                            programCounter,
                            opcode,
                            $"Opcode breakpoint: (0x{opcode:X4} & 0x{breakpoint.OpcodeMask.Value:X4}) == 0x{breakpoint.OpcodeValue.Value:X4}");
                        return true;
                    }
                }
            }

            match = null;
            return false;
        }
    }
}
