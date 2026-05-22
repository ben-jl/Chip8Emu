namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed record BreakpointDefinition
    {
        private BreakpointDefinition()
        {
        }

        public Guid Id { get; init; }
        public BreakpointKind Kind { get; init; }
        public bool Enabled { get; init; }
        public ushort? Address { get; init; }
        public ushort? OpcodeValue { get; init; }
        public ushort? OpcodeMask { get; init; }

        public static BreakpointDefinition CreateAddress(ushort address, bool enabled = true)
        {
            return new BreakpointDefinition
            {
                Id = Guid.NewGuid(),
                Kind = BreakpointKind.Address,
                Enabled = enabled,
                Address = address
            };
        }

        public static BreakpointDefinition CreateOpcode(ushort opcodeValue, ushort opcodeMask = 0xFFFF, bool enabled = true)
        {
            return new BreakpointDefinition
            {
                Id = Guid.NewGuid(),
                Kind = BreakpointKind.Opcode,
                Enabled = enabled,
                OpcodeValue = (ushort)(opcodeValue & opcodeMask),
                OpcodeMask = opcodeMask
            };
        }
    }
}
