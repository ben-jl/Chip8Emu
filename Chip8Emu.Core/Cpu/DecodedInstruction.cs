using System;

namespace Chip8Emu.Core.Cpu
{
    public sealed record DecodedInstruction(
        ushort Opcode,
        InstructionDefinition Definition,
        DecodedOperands Operands)
    {
        public string ResolveMnemonic(InstructionAliasOptions? aliasOptions = null)
        {
            return aliasOptions?.ResolveMnemonic(Definition) ?? Definition.Mnemonic;
        }
    }
}
