using System;

namespace Chip8Emu.Core.Cpu
{
    public sealed record InstructionDefinition(
        string Mnemonic,
        ushort Mask,
        ushort Pattern,
        OperandPattern Operands,
        Func<ParsedOperands, ushort> Encode,
        Func<ushort, DecodedOperands> Decode
    );
}
