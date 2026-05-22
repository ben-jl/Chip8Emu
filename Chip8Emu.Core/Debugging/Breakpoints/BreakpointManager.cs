using Chip8Emu.Core.Debugging.Breakpoints.Condition;
using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Machine;
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

        public BreakpointDefinition AddMemoryBreakpoint(
            ushort startAddress,
            ushort endAddress,
            bool breakOnRead = true,
            bool breakOnWrite = true,
            bool enabled = true)
        {
            var breakpoint = BreakpointDefinition.CreateMemory(startAddress, endAddress, breakOnRead, breakOnWrite, enabled);
            _breakpoints.Add(breakpoint);
            return breakpoint;
        }

        public BreakpointDefinition AddRegisterEqualsBreakpoint(string registerName, ushort value, bool enabled = true)
        {
            var breakpoint = BreakpointDefinition.CreateRegisterEquals(registerName, value, enabled);
            _breakpoints.Add(breakpoint);
            return breakpoint;
        }

        public BreakpointDefinition AddRegisterChangedBreakpoint(string registerName, bool enabled = true)
        {
            var breakpoint = BreakpointDefinition.CreateRegisterChanged(registerName, enabled);
            _breakpoints.Add(breakpoint);
            return breakpoint;
        }

        public BreakpointDefinition AddConditionalBreakpoint(string expression, bool enabled = true)
        {
            var predicate = BreakpointConditionCompiler.Compile(expression);
            var breakpoint = BreakpointDefinition.CreateConditional(expression, predicate, enabled);
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
            var current = new MachineSnapshot(
                new CpuSnapshot(programCounter, 0, new byte[16], 0, 0),
                0,
                0);
            var context = new BreakpointEvaluationContext(
                programCounter,
                opcode,
                current,
                null,
                Array.Empty<MemoryAccessSnapshot>());
            return TryMatch(context, out match);
        }

        public bool TryMatch(BreakpointEvaluationContext context, out BreakpointMatch? match)
        {
            foreach (var breakpoint in _breakpoints)
            {
                if (!breakpoint.Enabled)
                {
                    continue;
                }

                if (TryMatchAddress(breakpoint, context, out match))
                {
                    return true;
                }

                if (TryMatchOpcode(breakpoint, context, out match))
                {
                    return true;
                }

                if (TryMatchMemory(breakpoint, context, out match))
                {
                    return true;
                }

                if (TryMatchRegister(breakpoint, context, out match))
                {
                    return true;
                }

                if (TryMatchConditional(breakpoint, context, out match))
                {
                    return true;
                }
            }

            match = null;
            return false;
        }

        private static bool TryMatchAddress(BreakpointDefinition breakpoint, BreakpointEvaluationContext context, out BreakpointMatch? match)
        {
            if (breakpoint.Kind != BreakpointKind.Address || !breakpoint.Address.HasValue)
            {
                match = null;
                return false;
            }

            if (breakpoint.Address.Value != context.ProgramCounter)
            {
                match = null;
                return false;
            }

            match = new BreakpointMatch(
                breakpoint.Id,
                BreakpointKind.Address,
                context.ProgramCounter,
                context.Opcode,
                $"Address breakpoint at 0x{context.ProgramCounter:X3}");
            return true;
        }

        private static bool TryMatchOpcode(BreakpointDefinition breakpoint, BreakpointEvaluationContext context, out BreakpointMatch? match)
        {
            if (breakpoint.Kind != BreakpointKind.Opcode ||
                !breakpoint.OpcodeMask.HasValue ||
                !breakpoint.OpcodeValue.HasValue)
            {
                match = null;
                return false;
            }

            var masked = (ushort)(context.Opcode & breakpoint.OpcodeMask.Value);
            if (masked != breakpoint.OpcodeValue.Value)
            {
                match = null;
                return false;
            }

            match = new BreakpointMatch(
                breakpoint.Id,
                BreakpointKind.Opcode,
                context.ProgramCounter,
                context.Opcode,
                $"Opcode breakpoint: (0x{context.Opcode:X4} & 0x{breakpoint.OpcodeMask.Value:X4}) == 0x{breakpoint.OpcodeValue.Value:X4}");
            return true;
        }

        private static bool TryMatchMemory(BreakpointDefinition breakpoint, BreakpointEvaluationContext context, out BreakpointMatch? match)
        {
            if (breakpoint.Kind != BreakpointKind.Memory || breakpoint.Memory is null)
            {
                match = null;
                return false;
            }

            foreach (var access in context.MemoryAccesses)
            {
                if (!breakpoint.Memory.Matches(access))
                {
                    continue;
                }

                match = new BreakpointMatch(
                    breakpoint.Id,
                    BreakpointKind.Memory,
                    context.ProgramCounter,
                    context.Opcode,
                    $"Memory {access.Kind} breakpoint at 0x{access.Address:X3}");
                return true;
            }

            match = null;
            return false;
        }

        private static bool TryMatchRegister(BreakpointDefinition breakpoint, BreakpointEvaluationContext context, out BreakpointMatch? match)
        {
            if (breakpoint.Kind != BreakpointKind.Register || breakpoint.Register is null)
            {
                match = null;
                return false;
            }

            var register = breakpoint.Register;
            var currentValue = BreakpointRegisterResolver.ResolveRegisterValue(context, register.RegisterName);
            var previousValue = BreakpointRegisterResolver.ResolvePreviousRegisterValue(context, register.RegisterName);

            var matched = register.Comparison switch
            {
                RegisterComparisonKind.Equals => register.CompareValue.HasValue && currentValue == register.CompareValue.Value,
                RegisterComparisonKind.Changed => currentValue != previousValue,
                _ => false
            };

            if (!matched)
            {
                match = null;
                return false;
            }

            match = new BreakpointMatch(
                breakpoint.Id,
                BreakpointKind.Register,
                context.ProgramCounter,
                context.Opcode,
                $"Register breakpoint on {register.RegisterName}: current=0x{currentValue:X}");
            return true;
        }

        private static bool TryMatchConditional(BreakpointDefinition breakpoint, BreakpointEvaluationContext context, out BreakpointMatch? match)
        {
            if (breakpoint.Kind != BreakpointKind.Conditional || breakpoint.Conditional is null)
            {
                match = null;
                return false;
            }

            if (!breakpoint.Conditional.Predicate(context))
            {
                match = null;
                return false;
            }

            match = new BreakpointMatch(
                breakpoint.Id,
                BreakpointKind.Conditional,
                context.ProgramCounter,
                context.Opcode,
                $"Conditional breakpoint matched: {breakpoint.Conditional.Expression}");
            return true;
        }
    }
}
