using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Test.Core
{
    public class InstructionDecoderTests
    {
        private static InstructionDecoder CreateDecoder()
        {
            return new InstructionDecoder();
        }

        [Fact]
        public void Decode_ShouldReturnValid_ForKnownOpcode()
        {
            var decoder = CreateDecoder();

            var result = decoder.Decode(0x6A42);

            var valid = Assert.IsType<InstructionDecodeResult.Valid>(result);
            Assert.Equal((ushort)0x6A42, valid.Instruction.Opcode);
            Assert.Equal(Chip8InstructionSet.PatternLDByte, valid.Instruction.Definition.Pattern);
            Assert.Equal("LD", valid.Instruction.Definition.Mnemonic);
            Assert.Equal((byte)0xA, valid.Instruction.Operands.X);
            Assert.Equal((byte)0x42, valid.Instruction.Operands.NN);
        }

        [Fact]
        public void Decode_ShouldPreferSpecificPattern_ForCLSOverSYS()
        {
            var decoder = CreateDecoder();

            var result = decoder.Decode(0x00E0);

            var valid = Assert.IsType<InstructionDecodeResult.Valid>(result);
            Assert.Equal(Chip8InstructionSet.PatternCLS, valid.Instruction.Definition.Pattern);
            Assert.Equal("CLS", valid.Instruction.Definition.Mnemonic);
            Assert.Equal(DecodedOperands.Empty, valid.Instruction.Operands);
        }

        [Fact]
        public void Decode_ShouldPreferSpecificPattern_ForRETOverSYS()
        {
            var decoder = CreateDecoder();

            var result = decoder.Decode(0x00EE);

            var valid = Assert.IsType<InstructionDecodeResult.Valid>(result);
            Assert.Equal(Chip8InstructionSet.PatternRET, valid.Instruction.Definition.Pattern);
            Assert.Equal("RET", valid.Instruction.Definition.Mnemonic);
            Assert.Equal(DecodedOperands.Empty, valid.Instruction.Operands);
        }

        [Fact]
        public void Decode_ShouldResolveSYS_ForGeneric0NNN()
        {
            var decoder = CreateDecoder();

            var result = decoder.Decode(0x0123);

            var valid = Assert.IsType<InstructionDecodeResult.Valid>(result);
            Assert.Equal(Chip8InstructionSet.PatternSYS, valid.Instruction.Definition.Pattern);
            Assert.Equal("SYS", valid.Instruction.Definition.Mnemonic);
            Assert.Equal((ushort)0x123, valid.Instruction.Operands.NNN);
        }

        [Theory]
        [InlineData(0x00FE, Chip8InstructionSet.PatternLOW, "LOW")]
        [InlineData(0x00FF, Chip8InstructionSet.PatternHIGH, "HIGH")]
        public void Decode_ShouldResolveSuperChipDisplayModeInstructions(
            ushort opcode,
            ushort expectedPattern,
            string expectedMnemonic)
        {
            var decoder = CreateDecoder();

            var result = decoder.Decode(opcode);

            var valid = Assert.IsType<InstructionDecodeResult.Valid>(result);
            Assert.Equal(expectedPattern, valid.Instruction.Definition.Pattern);
            Assert.Equal(expectedMnemonic, valid.Instruction.Definition.Mnemonic);
            Assert.Equal(DecodedOperands.Empty, valid.Instruction.Operands);
        }

        [Fact]
        public void Decode_ShouldReturnInvalid_ForUnknownOpcode()
        {
            var decoder = CreateDecoder();

            var result = decoder.Decode(0xF0F0);

            var invalid = Assert.IsType<InstructionDecodeResult.Invalid>(result);
            Assert.Equal((ushort)0xF0F0, invalid.Opcode);
            Assert.Contains("Unknown opcode", invalid.Reason);
        }

        [Fact]
        public void DecodeOrThrow_ShouldThrow_ForUnknownOpcode()
        {
            var decoder = CreateDecoder();

            var exception = Assert.Throws<InvalidOperationException>(() => decoder.DecodeOrThrow(0xF0F0));

            Assert.Contains("Unknown opcode", exception.Message);
            Assert.Contains("F0F0", exception.Message);
        }

        [Fact]
        public void DecodeOrThrow_ShouldReturnDecodedInstruction_ForKnownOpcode()
        {
            var decoder = CreateDecoder();

            var decoded = decoder.DecodeOrThrow(0xDAB5);

            Assert.Equal(Chip8InstructionSet.PatternDRW, decoded.Definition.Pattern);
            Assert.Equal((byte)0xA, decoded.Operands.X);
            Assert.Equal((byte)0xB, decoded.Operands.Y);
            Assert.Equal((byte)0x5, decoded.Operands.N);
        }

        [Fact]
        public void DefinitionEncodeDecode_ShouldRoundTrip_ForRepresentativeOpcodes()
        {
            var testCases = new (ushort Pattern, ParsedOperands Operands, ushort ExpectedOpcode)[]
            {
                (Chip8InstructionSet.PatternCLS, new ParsedOperands(), 0x00E0),
                (Chip8InstructionSet.PatternLOW, new ParsedOperands(), 0x00FE),
                (Chip8InstructionSet.PatternHIGH, new ParsedOperands(), 0x00FF),
                (Chip8InstructionSet.PatternJP, new ParsedOperands(NNN: 0x234), 0x1234),
                (Chip8InstructionSet.PatternSEByte, new ParsedOperands(X: 0xA, NN: 0x42), 0x3A42),
                (Chip8InstructionSet.PatternSEReg, new ParsedOperands(X: 0xA, Y: 0xB), 0x5AB0),
                (Chip8InstructionSet.PatternDRW, new ParsedOperands(X: 0xA, Y: 0xB, N: 0x5), 0xDAB5),
                (Chip8InstructionSet.PatternSKP, new ParsedOperands(X: 0xA), 0xEA9E),
                (Chip8InstructionSet.PatternSTREGI, new ParsedOperands(X: 0xA), 0xFA55)
            };

            foreach (var testCase in testCases)
            {
                var definition = Chip8InstructionSet.Definitions.Single(d => d.Pattern == testCase.Pattern);

                var encoded = definition.Encode(testCase.Operands);
                Assert.Equal(testCase.ExpectedOpcode, encoded);

                var decodedOperands = definition.Decode(encoded);
                var reEncoded = definition.Encode(new ParsedOperands(
                    X: decodedOperands.X,
                    Y: decodedOperands.Y,
                    N: decodedOperands.N,
                    NN: decodedOperands.NN,
                    NNN: decodedOperands.NNN));

                Assert.Equal(encoded, reEncoded);
            }
        }

        [Fact]
        public void DecodedInstruction_ShouldUseAlias_WhenConfiguredForOpcodeVariant()
        {
            var decoder = CreateDecoder();
            var decoded = decoder.DecodeOrThrow(0x8126);

            var aliasOptions = new InstructionAliasOptions(new Dictionary<InstructionVariantKey, string>
            {
                [new InstructionVariantKey(0xF00F, Chip8InstructionSet.PatternSHR)] = "SRL"
            });

            Assert.Equal("SHR", decoded.ResolveMnemonic());
            Assert.Equal("SRL", decoded.ResolveMnemonic(aliasOptions));
        }

        [Fact]
        public void Definitions_ShouldContainUniqueMaskPatternPairs()
        {
            var duplicates = Chip8InstructionSet.Definitions
                .GroupBy(definition => new InstructionVariantKey(definition.Mask, definition.Pattern))
                .Where(group => group.Count() > 1)
                .ToList();

            Assert.Empty(duplicates);
        }
    }
}
