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
        public MemoryBreakpointDefinition? Memory { get; init; }
        public RegisterBreakpointDefinition? Register { get; init; }
        public ConditionalBreakpointDefinition? Conditional { get; init; }

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

        public static BreakpointDefinition CreateMemory(
            ushort startAddress,
            ushort endAddress,
            bool breakOnRead = true,
            bool breakOnWrite = true,
            bool enabled = true)
        {
            if (!breakOnRead && !breakOnWrite)
            {
                throw new ArgumentException("At least one of breakOnRead or breakOnWrite must be true.");
            }

            if (endAddress < startAddress)
            {
                throw new ArgumentException("Memory breakpoint endAddress must be greater than or equal to startAddress.");
            }

            return new BreakpointDefinition
            {
                Id = Guid.NewGuid(),
                Kind = BreakpointKind.Memory,
                Enabled = enabled,
                Memory = new MemoryBreakpointDefinition(startAddress, endAddress, breakOnRead, breakOnWrite)
            };
        }

        public static BreakpointDefinition CreateRegisterEquals(string registerName, ushort compareValue, bool enabled = true)
        {
            var normalized = registerName.NormalizeRegisterName();
            return new BreakpointDefinition
            {
                Id = Guid.NewGuid(),
                Kind = BreakpointKind.Register,
                Enabled = enabled,
                Register = new RegisterBreakpointDefinition(normalized, RegisterComparisonKind.Equals, compareValue)
            };
        }

        public static BreakpointDefinition CreateRegisterChanged(string registerName, bool enabled = true)
        {
            var normalized = registerName.NormalizeRegisterName();
            return new BreakpointDefinition
            {
                Id = Guid.NewGuid(),
                Kind = BreakpointKind.Register,
                Enabled = enabled,
                Register = new RegisterBreakpointDefinition(normalized, RegisterComparisonKind.Changed, null)
            };
        }

        public static BreakpointDefinition CreateConditional(
            string expression,
            Func<BreakpointEvaluationContext, bool> predicate,
            bool enabled = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expression);
            ArgumentNullException.ThrowIfNull(predicate);

            return new BreakpointDefinition
            {
                Id = Guid.NewGuid(),
                Kind = BreakpointKind.Conditional,
                Enabled = enabled,
                Conditional = new ConditionalBreakpointDefinition(expression, predicate)
            };
        }
    }
}
