using System;

namespace Chip8Emu.Core.Cpu
{
    public sealed record ParsedOperands(
        byte? X = null,
        byte? Y = null,
        byte? N = null,
        byte? NN = null,
        ushort? NNN = null)
    {
        public byte RequireX(string mnemonic) => RequireByteValue(X, nameof(X), mnemonic);
        public byte RequireY(string mnemonic) => RequireByteValue(Y, nameof(Y), mnemonic);
        public byte RequireN(string mnemonic) => RequireNibbleValue(N, nameof(N), mnemonic);
        public byte RequireNN(string mnemonic) => RequireByteValue(NN, nameof(NN), mnemonic);
        public ushort RequireNNN(string mnemonic) => RequireAddressValue(NNN, nameof(NNN), mnemonic);

        private static byte RequireByteValue(byte? value, string operandName, string mnemonic)
        {
            if (!value.HasValue)
            {
                throw new ArgumentException($"Operand {operandName} is required for {mnemonic}.", nameof(value));
            }

            return value.Value;
        }

        private static byte RequireNibbleValue(byte? value, string operandName, string mnemonic)
        {
            var nibble = RequireByteValue(value, operandName, mnemonic);
            if (nibble > 0xF)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Operand {operandName} must be a nibble for {mnemonic}.");
            }

            return nibble;
        }

        private static ushort RequireAddressValue(ushort? value, string operandName, string mnemonic)
        {
            if (!value.HasValue)
            {
                throw new ArgumentException($"Operand {operandName} is required for {mnemonic}.", nameof(value));
            }

            if (value.Value > 0x0FFF)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Operand {operandName} must be between 0x000 and 0xFFF for {mnemonic}.");
            }

            return value.Value;
        }
    }
}
