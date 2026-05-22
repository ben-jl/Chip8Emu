using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Core.Debugging.Disassembly
{
    public sealed class Chip8Disassembler : IChip8Disassembler
    {
        private readonly InstructionDecoder _decoder;

        public Chip8Disassembler() : this(new InstructionDecoder())
        {
        }

        internal Chip8Disassembler(InstructionDecoder decoder)
        {
            ArgumentNullException.ThrowIfNull(decoder);
            _decoder = decoder;
        }

        public IReadOnlyList<DisassembledInstruction> Disassemble(ReadOnlySpan<byte> romData, ushort romStartAddress = 0x200)
        {
            var rows = new List<DisassembledInstruction>(romData.Length / 2 + 1);
            for (var offset = 0; offset < romData.Length; offset += 2)
            {
                var address = (ushort)(romStartAddress + offset);
                var high = romData[offset];
                if (offset + 1 >= romData.Length)
                {
                    var truncatedOpcode = (ushort)(high << 8);
                    rows.Add(new DisassembledInstruction(
                        address,
                        truncatedOpcode,
                        "INVALID",
                        string.Empty,
                        false,
                        "Truncated instruction: missing low byte."));
                    break;
                }

                var low = romData[offset + 1];
                var opcode = (ushort)((high << 8) | low);
                rows.Add(DisassembleOpcode(address, opcode));
            }

            return rows;
        }

        public DisassemblyWindow CreateWindow(IReadOnlyList<DisassembledInstruction> listing, ushort programCounter, int radius = 8)
        {
            ArgumentNullException.ThrowIfNull(listing);
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be non-negative.");
            }

            if (listing.Count == 0)
            {
                return new DisassemblyWindow(Array.Empty<DisassembledInstruction>(), -1, programCounter);
            }

            var nearestIndex = FindNearestIndex(listing, programCounter);
            var start = Math.Max(0, nearestIndex - radius);
            var end = Math.Min(listing.Count - 1, nearestIndex + radius);

            var rows = new List<DisassembledInstruction>(end - start + 1);
            for (var i = start; i <= end; i++)
            {
                rows.Add(listing[i]);
            }

            var selected = nearestIndex - start;
            return new DisassemblyWindow(rows, selected, programCounter);
        }

        private DisassembledInstruction DisassembleOpcode(ushort address, ushort opcode)
        {
            var decode = _decoder.Decode(opcode);
            if (decode is InstructionDecodeResult.Valid valid)
            {
                var operandsText = FormatOperands(valid.Instruction);
                return new DisassembledInstruction(
                    address,
                    opcode,
                    valid.Instruction.Definition.Mnemonic,
                    operandsText,
                    true,
                    null);
            }

            if (decode is InstructionDecodeResult.Invalid invalid)
            {
                return new DisassembledInstruction(
                    address,
                    opcode,
                    "INVALID",
                    string.Empty,
                    false,
                    invalid.Reason);
            }

            throw new InvalidOperationException("Unexpected decode result.");
        }

        private static int FindNearestIndex(IReadOnlyList<DisassembledInstruction> listing, ushort programCounter)
        {
            for (var i = 0; i < listing.Count; i++)
            {
                if (listing[i].Address == programCounter)
                {
                    return i;
                }
            }

            for (var i = listing.Count - 1; i >= 0; i--)
            {
                if (listing[i].Address < programCounter)
                {
                    return i;
                }
            }

            return 0;
        }

        private static string FormatOperands(DecodedInstruction instruction)
        {
            var operands = instruction.Operands;
            return instruction.Definition.Operands switch
            {
                OperandPattern.None => string.Empty,
                OperandPattern.Address => $"0x{operands.RequireNNN(instruction.Definition.Mnemonic):X3}",
                OperandPattern.V0Address => $"V0, 0x{operands.RequireNNN(instruction.Definition.Mnemonic):X3}",
                OperandPattern.IAddress => $"I, 0x{operands.RequireNNN(instruction.Definition.Mnemonic):X3}",
                OperandPattern.RegisterByte => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}, 0x{operands.RequireNN(instruction.Definition.Mnemonic):X2}",
                OperandPattern.RegisterRegister => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}, V{operands.RequireY(instruction.Definition.Mnemonic):X}",
                OperandPattern.RegisterNibble => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}, V{operands.RequireY(instruction.Definition.Mnemonic):X}, 0x{operands.RequireN(instruction.Definition.Mnemonic):X}",
                OperandPattern.Register => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.RegisterDelayTimer => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}, DT",
                OperandPattern.RegisterKey => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}, K",
                OperandPattern.DelayTimerRegister => $"DT, V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.SoundTimerRegister => $"ST, V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.IRegister => $"I, V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.FontRegister => $"F, V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.BcdRegister => $"B, V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.IndirectIRegister => $"[I], V{operands.RequireX(instruction.Definition.Mnemonic):X}",
                OperandPattern.RegisterIndirectI => $"V{operands.RequireX(instruction.Definition.Mnemonic):X}, [I]",
                _ => string.Empty
            };
        }
    }
}
