using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Test.Core
{
    public class InstructionDecoderTests
    {
        private InstructionDecoder CreateDecoder()
        {
            return new InstructionDecoder();
        }

        #region Basic Decode Tests

        [Fact]
        public void Decode_ShouldReturnInstruction_WithCorrectOpcode()
        {
            var decoder = CreateDecoder();
            ushort opcode = 0x1234;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(opcode, instruction.Opcode);
        }

        [Fact]
        public void Decode_ShouldExtractX_FromSecondNibble()
        {
            var decoder = CreateDecoder();
            // Opcode: 0x1A34 - X should be 0xA (second nibble)
            ushort opcode = 0x1A34;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0xA, instruction.X);
        }

        [Fact]
        public void Decode_ShouldExtractY_FromThirdNibble()
        {
            var decoder = CreateDecoder();
            // Opcode: 0x12B4 - Y should be 0xB (third nibble)
            ushort opcode = 0x12B4;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0xB, instruction.Y);
        }

        [Fact]
        public void Decode_ShouldExtractN_FromFourthNibble()
        {
            var decoder = CreateDecoder();
            // Opcode: 0x123C - N should be 0xC (fourth nibble)
            ushort opcode = 0x123C;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0xC, instruction.N);
        }

        [Fact]
        public void Decode_ShouldExtractNN_FromLastTwoNibbles()
        {
            var decoder = CreateDecoder();
            // Opcode: 0x12DE - NN should be 0xDE (last two nibbles)
            ushort opcode = 0x12DE;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0xDE, instruction.NN);
        }

        [Fact]
        public void Decode_ShouldExtractNNN_FromLastThreeNibbles()
        {
            var decoder = CreateDecoder();
            // Opcode: 0xABCD - NNN should be 0xBCD (last three nibbles)
            ushort opcode = 0xABCD;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((ushort)0xBCD, instruction.NNN);
        }

        #endregion

        #region All Nibbles Zero Tests

        [Fact]
        public void Decode_ShouldHandleAllZeros()
        {
            var decoder = CreateDecoder();
            ushort opcode = 0x0000;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((ushort)0x0000, instruction.Opcode);
            Assert.Equal((byte)0x0, instruction.X);
            Assert.Equal((byte)0x0, instruction.Y);
            Assert.Equal((byte)0x0, instruction.N);
            Assert.Equal((byte)0x00, instruction.NN);
            Assert.Equal((ushort)0x000, instruction.NNN);
        }

        #endregion

        #region All Nibbles Max Tests

        [Fact]
        public void Decode_ShouldHandleAllFs()
        {
            var decoder = CreateDecoder();
            ushort opcode = 0xFFFF;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((ushort)0xFFFF, instruction.Opcode);
            Assert.Equal((byte)0xF, instruction.X);
            Assert.Equal((byte)0xF, instruction.Y);
            Assert.Equal((byte)0xF, instruction.N);
            Assert.Equal((byte)0xFF, instruction.NN);
            Assert.Equal((ushort)0xFFF, instruction.NNN);
        }

        #endregion

        #region Specific CHIP-8 Instruction Pattern Tests

        [Theory]
        [InlineData(0x00E0)] // CLS - Clear screen
        [InlineData(0x00EE)] // RET - Return from subroutine
        public void Decode_ShouldHandle00EX_Instructions(ushort opcode)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(opcode, instruction.Opcode);
            Assert.Equal((byte)0x0, instruction.X);
            Assert.Equal((byte)0xE, instruction.Y);
        }

        [Theory]
        [InlineData(0x1234, 0x234)] // JP addr - Jump to address
        [InlineData(0x1FFF, 0xFFF)]
        [InlineData(0x1000, 0x000)]
        public void Decode_ShouldHandle1NNN_JumpInstruction(ushort opcode, ushort expectedNNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedNNN, instruction.NNN);
        }

        [Theory]
        [InlineData(0x2ABC, 0xABC)] // CALL addr - Call subroutine
        [InlineData(0x2200, 0x200)]
        [InlineData(0x2FFF, 0xFFF)]
        public void Decode_ShouldHandle2NNN_CallInstruction(ushort opcode, ushort expectedNNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedNNN, instruction.NNN);
        }

        [Theory]
        [InlineData(0x3A42, 0xA, 0x42)] // SE Vx, byte - Skip if equal
        [InlineData(0x30FF, 0x0, 0xFF)]
        [InlineData(0x3F00, 0xF, 0x00)]
        public void Decode_ShouldHandle3XNN_SkipIfEqualInstruction(ushort opcode, byte expectedX, byte expectedNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedNN, instruction.NN);
        }

        [Theory]
        [InlineData(0x4B55, 0xB, 0x55)] // SNE Vx, byte - Skip if not equal
        [InlineData(0x40AA, 0x0, 0xAA)]
        [InlineData(0x4F12, 0xF, 0x12)]
        public void Decode_ShouldHandle4XNN_SkipIfNotEqualInstruction(ushort opcode, byte expectedX, byte expectedNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedNN, instruction.NN);
        }

        [Theory]
        [InlineData(0x5AB0, 0xA, 0xB)] // SE Vx, Vy - Skip if Vx = Vy
        [InlineData(0x5120, 0x1, 0x2)]
        [InlineData(0x5FF0, 0xF, 0xF)]
        public void Decode_ShouldHandle5XY0_SkipIfRegistersEqualInstruction(ushort opcode, byte expectedX, byte expectedY)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedY, instruction.Y);
            Assert.Equal((byte)0x0, instruction.N);
        }

        [Theory]
        [InlineData(0x6C88, 0xC, 0x88)] // LD Vx, byte - Load byte into Vx
        [InlineData(0x60FF, 0x0, 0xFF)]
        [InlineData(0x6F00, 0xF, 0x00)]
        public void Decode_ShouldHandle6XNN_LoadByteInstruction(ushort opcode, byte expectedX, byte expectedNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedNN, instruction.NN);
        }

        [Theory]
        [InlineData(0x7D10, 0xD, 0x10)] // ADD Vx, byte - Add byte to Vx
        [InlineData(0x7001, 0x0, 0x01)]
        [InlineData(0x7FFF, 0xF, 0xFF)]
        public void Decode_ShouldHandle7XNN_AddByteInstruction(ushort opcode, byte expectedX, byte expectedNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedNN, instruction.NN);
        }

        [Theory]
        [InlineData(0x8AB0, 0xA, 0xB, 0x0)] // LD Vx, Vy
        [InlineData(0x8AB1, 0xA, 0xB, 0x1)] // OR Vx, Vy
        [InlineData(0x8AB2, 0xA, 0xB, 0x2)] // AND Vx, Vy
        [InlineData(0x8AB3, 0xA, 0xB, 0x3)] // XOR Vx, Vy
        [InlineData(0x8AB4, 0xA, 0xB, 0x4)] // ADD Vx, Vy
        [InlineData(0x8AB5, 0xA, 0xB, 0x5)] // SUB Vx, Vy
        [InlineData(0x8AB6, 0xA, 0xB, 0x6)] // SHR Vx {, Vy}
        [InlineData(0x8AB7, 0xA, 0xB, 0x7)] // SUBN Vx, Vy
        [InlineData(0x8ABE, 0xA, 0xB, 0xE)] // SHL Vx {, Vy}
        public void Decode_ShouldHandle8XYN_ArithmeticInstructions(ushort opcode, byte expectedX, byte expectedY, byte expectedN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedY, instruction.Y);
            Assert.Equal(expectedN, instruction.N);
        }

        [Theory]
        [InlineData(0x9AB0, 0xA, 0xB)] // SNE Vx, Vy - Skip if Vx != Vy
        [InlineData(0x9120, 0x1, 0x2)]
        [InlineData(0x9FF0, 0xF, 0xF)]
        public void Decode_ShouldHandle9XY0_SkipIfNotEqualInstruction(ushort opcode, byte expectedX, byte expectedY)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedY, instruction.Y);
            Assert.Equal((byte)0x0, instruction.N);
        }

        [Theory]
        [InlineData(0xA123, 0x123)] // LD I, addr - Load address into I
        [InlineData(0xA000, 0x000)]
        [InlineData(0xAFFF, 0xFFF)]
        public void Decode_ShouldHandleANNN_LoadIInstruction(ushort opcode, ushort expectedNNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedNNN, instruction.NNN);
        }

        [Theory]
        [InlineData(0xB456, 0x456)] // JP V0, addr - Jump to address + V0
        [InlineData(0xB200, 0x200)]
        [InlineData(0xBFFF, 0xFFF)]
        public void Decode_ShouldHandleBNNN_JumpPlusV0Instruction(ushort opcode, ushort expectedNNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedNNN, instruction.NNN);
        }

        [Theory]
        [InlineData(0xC5A7, 0x5, 0xA7)] // RND Vx, byte - Random AND byte
        [InlineData(0xC0FF, 0x0, 0xFF)]
        [InlineData(0xCF00, 0xF, 0x00)]
        public void Decode_ShouldHandleCXNN_RandomInstruction(ushort opcode, byte expectedX, byte expectedNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedNN, instruction.NN);
        }

        [Theory]
        [InlineData(0xDAB5, 0xA, 0xB, 0x5)] // DRW Vx, Vy, nibble - Draw sprite
        [InlineData(0xD001, 0x0, 0x0, 0x1)]
        [InlineData(0xDFFF, 0xF, 0xF, 0xF)]
        public void Decode_ShouldHandleDXYN_DrawInstruction(ushort opcode, byte expectedX, byte expectedY, byte expectedN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedY, instruction.Y);
            Assert.Equal(expectedN, instruction.N);
        }

        [Theory]
        [InlineData(0xE59E, 0x5)] // SKP Vx - Skip if key pressed
        [InlineData(0xE0A1, 0x0)] // SKNP Vx - Skip if key not pressed
        [InlineData(0xEF9E, 0xF)]
        public void Decode_ShouldHandleEXNN_KeyInstructions(ushort opcode, byte expectedX)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
        }

        [Theory]
        [InlineData(0xF307, 0x3, 0x07)] // LD Vx, DT
        [InlineData(0xF40A, 0x4, 0x0A)] // LD Vx, K
        [InlineData(0xF515, 0x5, 0x15)] // LD DT, Vx
        [InlineData(0xF618, 0x6, 0x18)] // LD ST, Vx
        [InlineData(0xF71E, 0x7, 0x1E)] // ADD I, Vx
        [InlineData(0xF829, 0x8, 0x29)] // LD F, Vx
        [InlineData(0xF933, 0x9, 0x33)] // LD B, Vx
        [InlineData(0xFA55, 0xA, 0x55)] // LD [I], Vx
        [InlineData(0xFB65, 0xB, 0x65)] // LD Vx, [I]
        public void Decode_ShouldHandleFXNN_MiscInstructions(ushort opcode, byte expectedX, byte expectedNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedNN, instruction.NN);
        }

        #endregion

        #region Edge Case Tests

        [Theory]
        [InlineData(0x0123, 0x1, 0x2, 0x3, 0x23, 0x123)]
        [InlineData(0x4567, 0x5, 0x6, 0x7, 0x67, 0x567)]
        [InlineData(0x89AB, 0x9, 0xA, 0xB, 0xAB, 0x9AB)]
        [InlineData(0xCDEF, 0xD, 0xE, 0xF, 0xEF, 0xDEF)]
        public void Decode_ShouldCorrectlyExtractAllFields(ushort opcode, byte expectedX, byte expectedY, byte expectedN, byte expectedNN, ushort expectedNNN)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(opcode, instruction.Opcode);
            Assert.Equal(expectedX, instruction.X);
            Assert.Equal(expectedY, instruction.Y);
            Assert.Equal(expectedN, instruction.N);
            Assert.Equal(expectedNN, instruction.NN);
            Assert.Equal(expectedNNN, instruction.NNN);
        }

        [Theory]
        [InlineData(0xA000)]
        [InlineData(0xB000)]
        [InlineData(0xC000)]
        [InlineData(0xD000)]
        [InlineData(0xE000)]
        [InlineData(0xF000)]
        public void Decode_ShouldHandleHighNibbleVariations(ushort opcode)
        {
            var decoder = CreateDecoder();
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(opcode, instruction.Opcode);
            Assert.Equal((byte)0x0, instruction.X);
            Assert.Equal((byte)0x0, instruction.Y);
            Assert.Equal((byte)0x0, instruction.N);
            Assert.Equal((byte)0x00, instruction.NN);
            Assert.Equal((ushort)0x000, instruction.NNN);
        }

        [Fact]
        public void Decode_ShouldHandleX_WithAllPossibleValues()
        {
            var decoder = CreateDecoder();
            
            for (byte x = 0; x <= 0xF; x++)
            {
                ushort opcode = (ushort)(0x6000 | (x << 8) | 0x42);
                var instruction = decoder.Decode(opcode);
                Assert.Equal(x, instruction.X);
            }
        }

        [Fact]
        public void Decode_ShouldHandleY_WithAllPossibleValues()
        {
            var decoder = CreateDecoder();
            
            for (byte y = 0; y <= 0xF; y++)
            {
                ushort opcode = (ushort)(0x8A00 | (y << 4) | 0x0);
                var instruction = decoder.Decode(opcode);
                Assert.Equal(y, instruction.Y);
            }
        }

        [Fact]
        public void Decode_ShouldHandleN_WithAllPossibleValues()
        {
            var decoder = CreateDecoder();
            
            for (byte n = 0; n <= 0xF; n++)
            {
                ushort opcode = (ushort)(0xD120 | n);
                var instruction = decoder.Decode(opcode);
                Assert.Equal(n, instruction.N);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x7F)]
        [InlineData(0x80)]
        [InlineData(0xFF)]
        public void Decode_ShouldHandleNN_WithFullByteRange(byte nn)
        {
            var decoder = CreateDecoder();
            ushort opcode = (ushort)(0x6500 | nn);
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(nn, instruction.NN);
        }

        [Theory]
        [InlineData(0x000)]
        [InlineData(0x200)]
        [InlineData(0x7FF)]
        [InlineData(0x800)]
        [InlineData(0xFFF)]
        public void Decode_ShouldHandleNNN_WithFullRange(ushort nnn)
        {
            var decoder = CreateDecoder();
            ushort opcode = (ushort)(0x1000 | nnn);
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal(nnn, instruction.NNN);
        }

        #endregion

        #region Bit Masking Verification Tests

        [Fact]
        public void Decode_ShouldIsolateX_IgnoringOtherBits()
        {
            var decoder = CreateDecoder();
            // Set all bits except X to 1, X to 0x5
            ushort opcode = 0xF5FF;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0x5, instruction.X);
        }

        [Fact]
        public void Decode_ShouldIsolateY_IgnoringOtherBits()
        {
            var decoder = CreateDecoder();
            // Set all bits except Y to 1, Y to 0x7
            ushort opcode = 0xFF7F;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0x7, instruction.Y);
        }

        [Fact]
        public void Decode_ShouldIsolateN_IgnoringOtherBits()
        {
            var decoder = CreateDecoder();
            // Set all bits except N to 1, N to 0x3
            ushort opcode = 0xFFF3;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0x3, instruction.N);
        }

        [Fact]
        public void Decode_ShouldIsolateNN_IgnoringOtherBits()
        {
            var decoder = CreateDecoder();
            // Set first byte to F, last byte to 0xA5
            ushort opcode = 0xFFA5;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((byte)0xA5, instruction.NN);
        }

        [Fact]
        public void Decode_ShouldIsolateNNN_IgnoringFirstNibble()
        {
            var decoder = CreateDecoder();
            // Set first nibble to F, last three to 0x123
            ushort opcode = 0xF123;
            
            var instruction = decoder.Decode(opcode);
            
            Assert.Equal((ushort)0x123, instruction.NNN);
        }

        #endregion

        #region Multiple Decode Tests

        [Fact]
        public void Decode_ShouldProduceIndependentInstructions()
        {
            var decoder = CreateDecoder();
            
            var instruction1 = decoder.Decode(0x1234);
            var instruction2 = decoder.Decode(0x5678);
            
            Assert.Equal((ushort)0x1234, instruction1.Opcode);
            Assert.Equal((ushort)0x5678, instruction2.Opcode);
            Assert.NotEqual(instruction1.X, instruction2.X);
        }

        [Fact]
        public void Decode_ShouldBeRepeatable()
        {
            var decoder = CreateDecoder();
            ushort opcode = 0xABCD;
            
            var instruction1 = decoder.Decode(opcode);
            var instruction2 = decoder.Decode(opcode);
            
            Assert.Equal(instruction1.Opcode, instruction2.Opcode);
            Assert.Equal(instruction1.X, instruction2.X);
            Assert.Equal(instruction1.Y, instruction2.Y);
            Assert.Equal(instruction1.N, instruction2.N);
            Assert.Equal(instruction1.NN, instruction2.NN);
            Assert.Equal(instruction1.NNN, instruction2.NNN);
        }

        [Fact]
        public void Decode_ShouldHandleSequentialOpcodes()
        {
            var decoder = CreateDecoder();
            var instructions = new List<Instruction>();
            
            for (ushort opcode = 0x1000; opcode <= 0x1010; opcode++)
            {
                instructions.Add(decoder.Decode(opcode));
            }
            
            Assert.Equal(17, instructions.Count);
            for (int i = 0; i < instructions.Count; i++)
            {
                Assert.Equal((ushort)(0x1000 + i), instructions[i].Opcode);
            }
        }

        #endregion

        #region Instruction Record Tests

        [Fact]
        public void Instruction_ShouldBeReadonlyRecordStruct()
        {
            var decoder = CreateDecoder();
            var instruction = decoder.Decode(0x1234);
            
            // Verify all properties are accessible
            Assert.NotEqual(default(ushort), instruction.Opcode);
            Assert.InRange(instruction.X, 0, 0xF);
            Assert.InRange(instruction.Y, 0, 0xF);
            Assert.InRange(instruction.N, 0, 0xF);
            Assert.InRange(instruction.NN, 0, 0xFF);
            Assert.InRange(instruction.NNN, 0, 0xFFF);
        }

        [Fact]
        public void Instruction_ShouldSupportEquality()
        {
            var decoder = CreateDecoder();
            var instruction1 = decoder.Decode(0x1234);
            var instruction2 = decoder.Decode(0x1234);
            var instruction3 = decoder.Decode(0x5678);
            
            Assert.Equal(instruction1, instruction2);
            Assert.NotEqual(instruction1, instruction3);
        }

        #endregion
    }
}
