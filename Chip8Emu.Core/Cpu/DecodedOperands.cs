using System;

namespace Chip8Emu.Core.Cpu
{
    public sealed record DecodedOperands(
        byte? X = null,
        byte? Y = null,
        byte? N = null,
        byte? NN = null,
        ushort? NNN = null)
    {
        public static DecodedOperands Empty { get; } = new();

        public byte RequireX(string mnemonic) => RequireByteValue(X, nameof(X), mnemonic);
        public byte RequireY(string mnemonic) => RequireByteValue(Y, nameof(Y), mnemonic);
        public byte RequireN(string mnemonic) => RequireByteValue(N, nameof(N), mnemonic);
        public byte RequireNN(string mnemonic) => RequireByteValue(NN, nameof(NN), mnemonic);
        public ushort RequireNNN(string mnemonic) => RequireAddressValue(NNN, nameof(NNN), mnemonic);

        private static byte RequireByteValue(byte? value, string operandName, string mnemonic)
        {
            if (!value.HasValue)
            {
                throw new InvalidOperationException($"Decoded operand {operandName} is required for {mnemonic}.");
            }

            return value.Value;
        }

        private static ushort RequireAddressValue(ushort? value, string operandName, string mnemonic)
        {
            if (!value.HasValue)
            {
                throw new InvalidOperationException($"Decoded operand {operandName} is required for {mnemonic}.");
            }

            return value.Value;
        }
    }
}
