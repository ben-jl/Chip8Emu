namespace Chip8Emu.Core.Debugging.Breakpoints
{
    internal static class BreakpointRegisterResolver
    {
        public static string NormalizeRegisterName(this string registerName)
        {
            if (string.IsNullOrWhiteSpace(registerName))
            {
                throw new ArgumentException("Register name is required.", nameof(registerName));
            }

            return registerName.Trim().ToUpperInvariant();
        }

        public static ushort ResolveRegisterValue(BreakpointEvaluationContext context, string registerName)
        {
            var key = registerName.NormalizeRegisterName();
            var current = context.CurrentSnapshot;

            return key switch
            {
                "PC" => current.Cpu.PC,
                "I" => current.Cpu.I,
                "DT" => current.DelayTimer,
                "ST" => current.SoundTimer,
                "OPCODE" => context.Opcode,
                _ when IsVRegister(key, out var index) => current.Cpu.V[index],
                _ => throw new InvalidOperationException($"Unknown register identifier '{registerName}'.")
            };
        }

        public static ushort ResolvePreviousRegisterValue(BreakpointEvaluationContext context, string registerName)
        {
            if (context.PreviousSnapshot is null)
            {
                return ResolveRegisterValue(context, registerName);
            }

            var key = registerName.NormalizeRegisterName();
            var previous = context.PreviousSnapshot;

            return key switch
            {
                "PC" => previous.Cpu.PC,
                "I" => previous.Cpu.I,
                "DT" => previous.DelayTimer,
                "ST" => previous.SoundTimer,
                "OPCODE" => context.Opcode,
                _ when IsVRegister(key, out var index) => previous.Cpu.V[index],
                _ => throw new InvalidOperationException($"Unknown register identifier '{registerName}'.")
            };
        }

        private static bool IsVRegister(string key, out int index)
        {
            index = -1;
            if (key.Length != 2 || key[0] != 'V')
            {
                return false;
            }

            if (int.TryParse(key.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out var parsed) &&
                parsed >= 0 && parsed <= 0xF)
            {
                index = parsed;
                return true;
            }

            return false;
        }
    }
}
