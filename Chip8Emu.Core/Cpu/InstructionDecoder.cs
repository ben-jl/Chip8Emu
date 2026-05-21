using System;
using System.Collections.Generic;

namespace Chip8Emu.Core.Cpu
{
    internal sealed class InstructionDecoder
    {
        private readonly IReadOnlyList<InstructionDefinition> _definitions;

        public InstructionDecoder() : this(Chip8InstructionSet.Definitions)
        {
        }

        public InstructionDecoder(IReadOnlyList<InstructionDefinition> definitions)
        {
            ArgumentNullException.ThrowIfNull(definitions);
            _definitions = definitions;
        }

        public InstructionDecodeResult Decode(ushort opcode)
        {
            foreach (var definition in _definitions)
            {
                if ((opcode & definition.Mask) != definition.Pattern)
                {
                    continue;
                }

                var operands = definition.Decode(opcode);
                var decodedInstruction = new DecodedInstruction(opcode, definition, operands);
                return new InstructionDecodeResult.Valid(decodedInstruction);
            }

            return new InstructionDecodeResult.Invalid(opcode, $"Unknown opcode: {opcode:X4}");
        }

        public DecodedInstruction DecodeOrThrow(ushort opcode)
        {
            var result = Decode(opcode);
            return result switch
            {
                InstructionDecodeResult.Valid valid => valid.Instruction,
                InstructionDecodeResult.Invalid invalid => throw new InvalidOperationException(invalid.Reason),
                _ => throw new InvalidOperationException("Unexpected decode result.")
            };
        }
    }
}
