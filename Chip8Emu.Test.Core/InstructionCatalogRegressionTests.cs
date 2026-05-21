using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Test.Core
{
    public class InstructionCatalogRegressionTests
    {
        public static TheoryData<string, ushort, ushort, OperandPattern> ExpectedDefinitions()
        {
            return new TheoryData<string, ushort, ushort, OperandPattern>
            {
                { "CLS", 0xFFFF, Chip8InstructionSet.PatternCLS, OperandPattern.None },
                { "RET", 0xFFFF, Chip8InstructionSet.PatternRET, OperandPattern.None },
                { "LOW", 0xFFFF, Chip8InstructionSet.PatternLOW, OperandPattern.None },
                { "HIGH", 0xFFFF, Chip8InstructionSet.PatternHIGH, OperandPattern.None },
                { "SYS", 0xF000, Chip8InstructionSet.PatternSYS, OperandPattern.Address },
                { "JP", 0xF000, Chip8InstructionSet.PatternJP, OperandPattern.Address },
                { "CALL", 0xF000, Chip8InstructionSet.PatternCALL, OperandPattern.Address },
                { "SE", 0xF000, Chip8InstructionSet.PatternSEByte, OperandPattern.RegisterByte },
                { "SNE", 0xF000, Chip8InstructionSet.PatternSNEByte, OperandPattern.RegisterByte },
                { "SE", 0xF00F, Chip8InstructionSet.PatternSEReg, OperandPattern.RegisterRegister },
                { "LD", 0xF000, Chip8InstructionSet.PatternLDByte, OperandPattern.RegisterByte },
                { "ADD", 0xF000, Chip8InstructionSet.PatternADDByte, OperandPattern.RegisterByte },
                { "LD", 0xF00F, Chip8InstructionSet.PatternLDReg, OperandPattern.RegisterRegister },
                { "OR", 0xF00F, Chip8InstructionSet.PatternOR, OperandPattern.RegisterRegister },
                { "AND", 0xF00F, Chip8InstructionSet.PatternAND, OperandPattern.RegisterRegister },
                { "XOR", 0xF00F, Chip8InstructionSet.PatternXOR, OperandPattern.RegisterRegister },
                { "ADD", 0xF00F, Chip8InstructionSet.PatternADDReg, OperandPattern.RegisterRegister },
                { "SUB", 0xF00F, Chip8InstructionSet.PatternSUB, OperandPattern.RegisterRegister },
                { "SHR", 0xF00F, Chip8InstructionSet.PatternSHR, OperandPattern.RegisterRegister },
                { "SUBN", 0xF00F, Chip8InstructionSet.PatternSUBN, OperandPattern.RegisterRegister },
                { "SHL", 0xF00F, Chip8InstructionSet.PatternSHL, OperandPattern.RegisterRegister },
                { "SNE", 0xF00F, Chip8InstructionSet.PatternSNEReg, OperandPattern.RegisterRegister },
                { "LD", 0xF000, Chip8InstructionSet.PatternLDI, OperandPattern.IAddress },
                { "JP", 0xF000, Chip8InstructionSet.PatternJPV0, OperandPattern.V0Address },
                { "RND", 0xF000, Chip8InstructionSet.PatternRND, OperandPattern.RegisterByte },
                { "DRW", 0xF000, Chip8InstructionSet.PatternDRW, OperandPattern.RegisterNibble },
                { "SKP", 0xF0FF, Chip8InstructionSet.PatternSKP, OperandPattern.Register },
                { "SKNP", 0xF0FF, Chip8InstructionSet.PatternSKNP, OperandPattern.Register },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternLDDT, OperandPattern.RegisterDelayTimer },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternLDK, OperandPattern.RegisterKey },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternSTDT, OperandPattern.DelayTimerRegister },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternSTST, OperandPattern.SoundTimerRegister },
                { "ADD", 0xF0FF, Chip8InstructionSet.PatternADDI, OperandPattern.IRegister },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternLDFNT, OperandPattern.FontRegister },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternLDBCD, OperandPattern.BcdRegister },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternSTREGI, OperandPattern.IndirectIRegister },
                { "LD", 0xF0FF, Chip8InstructionSet.PatternLDREGI, OperandPattern.RegisterIndirectI }
            };
        }

        [Fact]
        public void Definitions_ShouldRemainInCurrentDecodePrecedenceOrder()
        {
            var expected = ExpectedDefinitions()
                .Select(row => (
                    Mnemonic: (string)row[0],
                    Mask: (ushort)row[1],
                    Pattern: (ushort)row[2],
                    Operands: (OperandPattern)row[3]))
                .ToArray();

            Assert.Equal(expected.Length, Chip8InstructionSet.Definitions.Count);
            for (var index = 0; index < expected.Length; index++)
            {
                var definition = Chip8InstructionSet.Definitions[index];
                Assert.Equal(expected[index].Mnemonic, definition.Mnemonic);
                Assert.Equal(expected[index].Mask, definition.Mask);
                Assert.Equal(expected[index].Pattern, definition.Pattern);
                Assert.Equal(expected[index].Operands, definition.Operands);
            }
        }

        [Theory]
        [MemberData(nameof(ExpectedDefinitions))]
        public void Definitions_ShouldExposeExpectedOpcodeMetadata(
            string mnemonic,
            ushort mask,
            ushort pattern,
            OperandPattern operandPattern)
        {
            var definition = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Mask == mask && definition.Pattern == pattern);

            Assert.Equal(mnemonic, definition.Mnemonic);
            Assert.Equal(mask, definition.Mask);
            Assert.Equal(pattern, definition.Pattern);
            Assert.Equal(operandPattern, definition.Operands);
        }

        [Theory]
        [MemberData(nameof(ExpectedDefinitions))]
        public void Definitions_ShouldEncodeDecodeAndDecodeThroughInstructionDecoder(
            string mnemonic,
            ushort mask,
            ushort pattern,
            OperandPattern operandPattern)
        {
            var definition = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Mask == mask && definition.Pattern == pattern);
            Assert.Equal(mnemonic, definition.Mnemonic);

            var operands = CreateOperands(operandPattern);
            var encoded = definition.Encode(operands);

            Assert.Equal(pattern, (ushort)(encoded & mask));

            var definitionDecoded = definition.Decode(encoded);
            AssertDecodedOperandsMatchParsedOperands(operandPattern, operands, definitionDecoded);

            var decoder = new InstructionDecoder();
            var decoded = decoder.DecodeOrThrow(encoded);
            Assert.Equal(pattern, decoded.Definition.Pattern);
            AssertDecodedOperandsMatchParsedOperands(operandPattern, operands, decoded.Operands);
        }

        [Fact]
        public void InstructionAliasOptions_ShouldCopyAliasesAndIgnoreLaterDictionaryChanges()
        {
            var definition = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Pattern == Chip8InstructionSet.PatternSHR);
            var aliases = new Dictionary<InstructionVariantKey, string>
            {
                [new InstructionVariantKey(definition.Mask, definition.Pattern)] = "SRL"
            };

            var options = new InstructionAliasOptions(aliases);
            aliases[new InstructionVariantKey(definition.Mask, definition.Pattern)] = "SHIFT_RIGHT";

            Assert.Equal("SRL", options.ResolveMnemonic(definition));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void InstructionAliasOptions_ShouldFallbackToDefinitionMnemonic_ForBlankAliases(string alias)
        {
            var definition = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Pattern == Chip8InstructionSet.PatternSHL);
            var options = new InstructionAliasOptions(new Dictionary<InstructionVariantKey, string>
            {
                [new InstructionVariantKey(definition.Mask, definition.Pattern)] = alias
            });

            Assert.Equal("SHL", options.ResolveMnemonic(definition));
        }

        [Fact]
        public void ParsedOperands_ShouldThrowForMissingAndOutOfRangeOperands()
        {
            var operands = new ParsedOperands();

            Assert.Throws<ArgumentException>(() => operands.RequireX("TEST"));
            Assert.Throws<ArgumentException>(() => operands.RequireNN("TEST"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ParsedOperands(N: 0x10).RequireN("TEST"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ParsedOperands(NNN: 0x1000).RequireNNN("TEST"));
        }

        [Fact]
        public void DecodedOperands_ShouldThrowForMissingOperands()
        {
            var operands = DecodedOperands.Empty;

            Assert.Throws<InvalidOperationException>(() => operands.RequireX("TEST"));
            Assert.Throws<InvalidOperationException>(() => operands.RequireY("TEST"));
            Assert.Throws<InvalidOperationException>(() => operands.RequireN("TEST"));
            Assert.Throws<InvalidOperationException>(() => operands.RequireNN("TEST"));
            Assert.Throws<InvalidOperationException>(() => operands.RequireNNN("TEST"));
        }

        [Fact]
        public void Encode_ShouldRejectRegisterOperandsOutsideChip8RegisterRange()
        {
            var registerByte = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Pattern == Chip8InstructionSet.PatternLDByte);
            var registerRegister = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Pattern == Chip8InstructionSet.PatternLDReg);
            var registerNibble = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Pattern == Chip8InstructionSet.PatternDRW);
            var registerOnly = Chip8InstructionSet.Definitions.Single(definition =>
                definition.Pattern == Chip8InstructionSet.PatternSKP);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                registerByte.Encode(new ParsedOperands(X: 0x10, NN: 0x00)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                registerRegister.Encode(new ParsedOperands(X: 0x00, Y: 0x10)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                registerNibble.Encode(new ParsedOperands(X: 0x10, Y: 0x00, N: 0x01)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                registerOnly.Encode(new ParsedOperands(X: 0x10)));
        }

        [Fact]
        public void InstructionDecoder_ShouldThrowForNullDefinitions()
        {
            Assert.Throws<ArgumentNullException>(() => new InstructionDecoder(null!));
        }

        [Fact]
        public void InstructionAliasOptions_ShouldThrowForNullAliasDictionaryOrDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => new InstructionAliasOptions(null!));
            Assert.Throws<ArgumentNullException>(() => InstructionAliasOptions.Empty.ResolveMnemonic(null!));
        }

        private static ParsedOperands CreateOperands(OperandPattern operandPattern)
        {
            return operandPattern switch
            {
                OperandPattern.None => new ParsedOperands(),
                OperandPattern.Address => new ParsedOperands(NNN: 0x2AB),
                OperandPattern.RegisterByte => new ParsedOperands(X: 0xA, NN: 0x5C),
                OperandPattern.RegisterRegister => new ParsedOperands(X: 0xA, Y: 0xB),
                OperandPattern.RegisterNibble => new ParsedOperands(X: 0xA, Y: 0xB, N: 0xC),
                OperandPattern.Register => new ParsedOperands(X: 0xA),
                OperandPattern.V0Address => new ParsedOperands(NNN: 0x2AB),
                OperandPattern.IAddress => new ParsedOperands(NNN: 0x2AB),
                OperandPattern.RegisterDelayTimer => new ParsedOperands(X: 0xA),
                OperandPattern.RegisterKey => new ParsedOperands(X: 0xA),
                OperandPattern.DelayTimerRegister => new ParsedOperands(X: 0xA),
                OperandPattern.SoundTimerRegister => new ParsedOperands(X: 0xA),
                OperandPattern.IRegister => new ParsedOperands(X: 0xA),
                OperandPattern.FontRegister => new ParsedOperands(X: 0xA),
                OperandPattern.BcdRegister => new ParsedOperands(X: 0xA),
                OperandPattern.IndirectIRegister => new ParsedOperands(X: 0xA),
                OperandPattern.RegisterIndirectI => new ParsedOperands(X: 0xA),
                _ => throw new ArgumentOutOfRangeException(nameof(operandPattern), operandPattern, null)
            };
        }

        private static void AssertDecodedOperandsMatchParsedOperands(
            OperandPattern operandPattern,
            ParsedOperands parsed,
            DecodedOperands decoded)
        {
            Assert.Equal(parsed.X, decoded.X);
            Assert.Equal(parsed.Y, decoded.Y);
            Assert.Equal(parsed.N, decoded.N);
            Assert.Equal(parsed.NN, decoded.NN);
            Assert.Equal(parsed.NNN, decoded.NNN);

            if (operandPattern == OperandPattern.None)
            {
                Assert.Equal(DecodedOperands.Empty, decoded);
            }
        }
    }
}
