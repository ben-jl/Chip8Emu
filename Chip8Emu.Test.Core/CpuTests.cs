using Chip8Emu.Core.Cpu;
using Chip8Emu.Core.Display;
using Chip8Emu.Core.Input;
using Chip8Emu.Core.Memory;

namespace Chip8Emu.Test.Core
{
    public class CpuTests
    {
        private class TestableRegisters : Registers
        {
            public TestableRegisters(MemoryMap memoryMap) : base(memoryMap) { }

            public new ushort PC => base.PC;
            public new ushort I => base.I;
            public new byte GetV(byte register) => base.GetV(register);
        }

        private class TestableCpu
        {
            private readonly Chip8Cpu _cpu;
            private readonly TestableRegisters _registers;
            private readonly MemoryBus _memoryBus;
            private readonly MonochromeFrameBuffer _display;

            public TestableCpu(
                bool resetCarryFlagOnBitwiseOps = true, 
                bool incrementIOnStoreLoadMemoryOps = false,
                bool shiftUsesVY = false,
                bool clipSprites = true)
            {
                var memoryMap = new MemoryMap();
                _memoryBus = new MemoryBus(memoryMap.MemorySize);
                _display = new MonochromeFrameBuffer(64, 32);
                _cpu = new Chip8Cpu(
                    _memoryBus, 
                    memoryMap, 
                    _display, 
                    randomSeed: 12345, 
                    new KeypadState(),
                    new Chip8Emu.Core.Timing.Timers(),
                    resetCarryFlagOnBitwiseOps,
                    incrementIOnStoreLoadMemoryOps,
                    shiftUsesVY,
                    clipSprites);
                
                // Use reflection to access private _registers field
                var registersField = typeof(Chip8Cpu).GetField("_registers", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var registersInstance = (Registers)registersField!.GetValue(_cpu)!;
                
                // Create testable wrapper
                _registers = new TestableRegisters(memoryMap);
                registersField.SetValue(_cpu, _registers);
            }

            public MemoryBus MemoryBus => _memoryBus;
            public ushort PC => _registers.PC;
            public ushort I => _registers.I;
            public byte GetV(byte register) => _registers.GetV(register);
            public byte GetPixel(int x, int y) => _display.Buffer[y * _display.Width + x];

            public void WriteOpcode(ushort address, ushort opcode)
            {
                _memoryBus.Write(address, (byte)(opcode >> 8));
                _memoryBus.Write((ushort)(address + 1), (byte)(opcode & 0xFF));
            }

            public void Step()
            {
                _cpu.Step();
            }
        }

        #region JP (1nnn) - Jump to address Tests

        [Theory]
        [InlineData(0x1200, 0x200)]
        [InlineData(0x1300, 0x300)]
        [InlineData(0x1FFF, 0xFFF)]
        [InlineData(0x1000, 0x000)]
        public void JP_ShouldSetPCToAddress(ushort opcode, ushort expectedAddress)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            Assert.Equal(expectedAddress, cpu.PC);
        }

        [Fact]
        public void JP_ShouldJumpToMinimumAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x1000);

            cpu.Step();

            Assert.Equal((ushort)0x000, cpu.PC);
        }

        [Fact]
        public void JP_ShouldJumpToMaximumAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x1FFF);

            cpu.Step();

            Assert.Equal((ushort)0xFFF, cpu.PC);
        }

        [Fact]
        public void JP_ShouldJumpToSameLocation()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x1200);

            cpu.Step();

            Assert.Equal((ushort)0x200, cpu.PC);
        }

        [Theory]
        [InlineData(0x1234)]
        [InlineData(0x1567)]
        [InlineData(0x189A)]
        public void JP_ShouldHandleArbitraryAddresses(ushort opcode)
        {
            var cpu = new TestableCpu();
            ushort expectedAddress = (ushort)(opcode & 0x0FFF);
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            Assert.Equal(expectedAddress, cpu.PC);
        }

        [Fact]
        public void JP_ShouldAllowJumpingBackwards()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x1100);

            cpu.Step();

            Assert.Equal((ushort)0x100, cpu.PC);
        }

        #endregion

        #region LDBYTE (6xkk) - Load byte into Vx Tests

        [Theory]
        [InlineData(0x6000, 0x0, 0x00)]
        [InlineData(0x6142, 0x1, 0x42)]
        [InlineData(0x65FF, 0x5, 0xFF)]
        [InlineData(0x6A7F, 0xA, 0x7F)]
        [InlineData(0x6F80, 0xF, 0x80)]
        public void LDBYTE_ShouldLoadByteIntoRegister(ushort opcode, byte register, byte expectedValue)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            Assert.Equal(expectedValue, cpu.GetV(register));
        }

        [Fact]
        public void LDBYTE_ShouldLoadZeroIntoRegister()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6300);

            cpu.Step();

            Assert.Equal((byte)0x00, cpu.GetV(0x3));
        }

        [Fact]
        public void LDBYTE_ShouldLoadMaxByteIntoRegister()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6CFF);

            cpu.Step();

            Assert.Equal((byte)0xFF, cpu.GetV(0xC));
        }

        [Fact]
        public void LDBYTE_ShouldOverwritePreviousValue()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6555);
            cpu.WriteOpcode(0x202, 0x65AA);

            cpu.Step();
            Assert.Equal((byte)0x55, cpu.GetV(0x5));

            cpu.Step();
            Assert.Equal((byte)0xAA, cpu.GetV(0x5));
        }

        [Fact]
        public void LDBYTE_ShouldNotAffectOtherRegisters()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6311);
            cpu.WriteOpcode(0x202, 0x6422);

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x11, cpu.GetV(0x3));
            Assert.Equal((byte)0x22, cpu.GetV(0x4));
            Assert.Equal((byte)0x00, cpu.GetV(0x0));
            Assert.Equal((byte)0x00, cpu.GetV(0x1));
        }

        [Fact]
        public void LDBYTE_ShouldWorkForAllRegisters()
        {
            var cpu = new TestableCpu();
            
            for (byte reg = 0; reg <= 0xF; reg++)
            {
                ushort opcode = (ushort)(0x6000 | (reg << 8) | reg);
                cpu.WriteOpcode((ushort)(0x200 + reg * 2), opcode);
            }

            for (byte reg = 0; reg <= 0xF; reg++)
            {
                cpu.Step();
                Assert.Equal(reg, cpu.GetV(reg));
            }
        }

        [Fact]
        public void LDBYTE_ShouldIncrementPC()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6542);

            cpu.Step();

            Assert.Equal((ushort)0x202, cpu.PC);
        }

        #endregion

        #region ADDBYTE (7xkk) - Add byte to Vx Tests

        [Theory]
        [InlineData(0x7001, 0x0, 0x00, 0x01)]
        [InlineData(0x7105, 0x1, 0x10, 0x15)]
        [InlineData(0x72FF, 0x2, 0x00, 0xFF)]
        [InlineData(0x7350, 0x3, 0x50, 0xA0)]
        [InlineData(0x7F80, 0xF, 0x80, 0x00)] // Overflow wraps
        public void ADDBYTE_ShouldAddByteToRegister(ushort opcode, byte register, byte initialValue, byte expectedValue)
        {
            var cpu = new TestableCpu();
            
            // Always load initial value first
            cpu.WriteOpcode(0x200, (ushort)(0x6000 | (register << 8) | initialValue));
            cpu.Step();
            
            cpu.WriteOpcode(0x202, opcode);
            cpu.Step();

            Assert.Equal(expectedValue, cpu.GetV(register));
        }

        [Fact]
        public void ADDBYTE_ShouldAddToZeroInitializedRegister()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x7042);

            cpu.Step();

            Assert.Equal((byte)0x42, cpu.GetV(0x0));
        }

        [Fact]
        public void ADDBYTE_ShouldHandleOverflow()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x65FF);
            cpu.WriteOpcode(0x202, 0x7501);

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x00, cpu.GetV(0x5));
        }

        [Fact]
        public void ADDBYTE_ShouldHandleOverflowWithLargeValue()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6580);
            cpu.WriteOpcode(0x202, 0x7590);

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x10, cpu.GetV(0x5));
        }

        [Fact]
        public void ADDBYTE_ShouldWorkForMultipleAdditions()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x7310);
            cpu.WriteOpcode(0x202, 0x7320);
            cpu.WriteOpcode(0x204, 0x7330);

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x60, cpu.GetV(0x3));
        }

        [Fact]
        public void ADDBYTE_ShouldNotAffectOtherRegisters()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6111);
            cpu.WriteOpcode(0x202, 0x6222);
            cpu.WriteOpcode(0x204, 0x7105);

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x16, cpu.GetV(0x1));
            Assert.Equal((byte)0x22, cpu.GetV(0x2));
        }

        [Fact]
        public void ADDBYTE_ShouldAddZero()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6442);
            cpu.WriteOpcode(0x202, 0x7400);

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x42, cpu.GetV(0x4));
        }

        [Fact]
        public void ADDBYTE_ShouldAddMaxByte()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6A01);
            cpu.WriteOpcode(0x202, 0x7AFF);

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x00, cpu.GetV(0xA));
        }

        [Fact]
        public void ADDBYTE_ShouldWorkForAllRegisters()
        {
            var cpu = new TestableCpu();
            
            for (byte reg = 0; reg <= 0xF; reg++)
            {
                ushort loadOpcode = (ushort)(0x6000 | (reg << 8) | 0x10);
                ushort addOpcode = (ushort)(0x7000 | (reg << 8) | 0x05);
                cpu.WriteOpcode((ushort)(0x200 + reg * 4), loadOpcode);
                cpu.WriteOpcode((ushort)(0x202 + reg * 4), addOpcode);
            }

            for (byte reg = 0; reg <= 0xF; reg++)
            {
                cpu.Step();
                cpu.Step();
                Assert.Equal((byte)0x15, cpu.GetV(reg));
            }
        }

        [Fact]
        public void ADDBYTE_ShouldIncrementPC()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x7542);

            cpu.Step();

            Assert.Equal((ushort)0x202, cpu.PC);
        }

        #endregion

        #region LDI (Annn) - Load address into I register Tests

        [Theory]
        [InlineData(0xA000, 0x000)]
        [InlineData(0xA200, 0x200)]
        [InlineData(0xA500, 0x500)]
        [InlineData(0xAFFF, 0xFFF)]
        [InlineData(0xA123, 0x123)]
        public void LDI_ShouldLoadAddressIntoIRegister(ushort opcode, ushort expectedAddress)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            Assert.Equal(expectedAddress, cpu.I);
        }

        [Fact]
        public void LDI_ShouldLoadMinimumAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA000);

            cpu.Step();

            Assert.Equal((ushort)0x000, cpu.I);
        }

        [Fact]
        public void LDI_ShouldLoadMaximumAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xAFFF);

            cpu.Step();

            Assert.Equal((ushort)0xFFF, cpu.I);
        }

        [Fact]
        public void LDI_ShouldOverwritePreviousValue()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA300);
            cpu.WriteOpcode(0x202, 0xA500);

            cpu.Step();
            Assert.Equal((ushort)0x300, cpu.I);

            cpu.Step();
            Assert.Equal((ushort)0x500, cpu.I);
        }

        [Fact]
        public void LDI_ShouldLoadTypicalRomAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA200);

            cpu.Step();

            Assert.Equal((ushort)0x200, cpu.I);
        }

        [Fact]
        public void LDI_ShouldLoadFontAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA050);

            cpu.Step();

            Assert.Equal((ushort)0x050, cpu.I);
        }

        [Theory]
        [InlineData(0xA100)]
        [InlineData(0xA234)]
        [InlineData(0xA567)]
        [InlineData(0xA89A)]
        [InlineData(0xABCD)]
        public void LDI_ShouldHandleArbitraryAddresses(ushort opcode)
        {
            var cpu = new TestableCpu();
            ushort expectedAddress = (ushort)(opcode & 0x0FFF);
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            Assert.Equal(expectedAddress, cpu.I);
        }

        [Fact]
        public void LDI_ShouldNotAffectVRegisters()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142);
            cpu.WriteOpcode(0x202, 0xA500);

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x42, cpu.GetV(0x1));
            Assert.Equal((ushort)0x500, cpu.I);
        }

        [Fact]
        public void LDI_ShouldNotAffectPC_ExceptIncrement()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA300);

            cpu.Step();

            Assert.Equal((ushort)0x202, cpu.PC);
        }

        [Fact]
        public void LDI_ShouldWorkInSequence()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA100);
            cpu.WriteOpcode(0x202, 0xA200);
            cpu.WriteOpcode(0x204, 0xA300);

            cpu.Step();
            Assert.Equal((ushort)0x100, cpu.I);

            cpu.Step();
            Assert.Equal((ushort)0x200, cpu.I);

            cpu.Step();
            Assert.Equal((ushort)0x300, cpu.I);
        }

        [Fact]
        public void LDI_ShouldIncrementPC()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA456);

            cpu.Step();

            Assert.Equal((ushort)0x202, cpu.PC);
        }

        #endregion

        #region RND (Cxkk) - Random AND byte Tests

        [Fact]
        public void RND_ShouldGenerateRandomValueAndWithMask()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC0FF); // RND V0, 0xFF

            cpu.Step();

            var value = cpu.GetV(0x0);
            Assert.InRange(value, (byte)0x00, (byte)0xFF);
        }

        [Fact]
        public void RND_WithMaskZero_ShouldAlwaysBeZero()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC000); // RND V0, 0x00
            cpu.WriteOpcode(0x202, 0xC100); // RND V1, 0x00
            cpu.WriteOpcode(0x204, 0xC200); // RND V2, 0x00

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x00, cpu.GetV(0x0));
            Assert.Equal((byte)0x00, cpu.GetV(0x1));
            Assert.Equal((byte)0x00, cpu.GetV(0x2));
        }

        [Fact]
        public void RND_WithMaskFF_ShouldAllowFullRange()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC0FF); // RND V0, 0xFF

            cpu.Step();

            var value = cpu.GetV(0x0);
            Assert.InRange(value, (byte)0x00, (byte)0xFF);
        }

        [Fact]
        public void RND_WithMask0F_ShouldOnlySetLowerNibble()
        {
            var cpu = new TestableCpu();
            
            for (int i = 0; i < 10; i++)
            {
                cpu.WriteOpcode((ushort)(0x200 + i * 2), 0xC00F); // RND V0, 0x0F
            }

            for (int i = 0; i < 10; i++)
            {
                cpu.Step();
                var value = cpu.GetV(0x0);
                Assert.InRange(value, (byte)0x00, (byte)0x0F);
                Assert.Equal(0, value & 0xF0);
            }
        }

        [Fact]
        public void RND_WithMaskF0_ShouldOnlySetUpperNibble()
        {
            var cpu = new TestableCpu();
            
            for (int i = 0; i < 10; i++)
            {
                cpu.WriteOpcode((ushort)(0x200 + i * 2), 0xC0F0); // RND V0, 0xF0
            }

            for (int i = 0; i < 10; i++)
            {
                cpu.Step();
                var value = cpu.GetV(0x0);
                Assert.InRange(value, (byte)0x00, (byte)0xFF);
                Assert.Equal(0, value & 0x0F);
            }
        }

        [Fact]
        public void RND_WithMask01_ShouldOnlyProduceZeroOrOne()
        {
            var cpu = new TestableCpu();
            
            for (int i = 0; i < 10; i++)
            {
                cpu.WriteOpcode((ushort)(0x200 + i * 2), 0xC001); // RND V0, 0x01
            }

            bool foundZero = false;
            bool foundOne = false;

            for (int i = 0; i < 10; i++)
            {
                cpu.Step();
                var value = cpu.GetV(0x0);
                Assert.InRange(value, (byte)0x00, (byte)0x01);
                
                if (value == 0) foundZero = true;
                if (value == 1) foundOne = true;
            }

            Assert.True(foundZero || foundOne);
        }

        [Theory]
        [InlineData(0xC00F, 0x0, 0x0F)]
        [InlineData(0xC1FF, 0x1, 0xFF)]
        [InlineData(0xC255, 0x2, 0x55)]
        [InlineData(0xC3AA, 0x3, 0xAA)]
        [InlineData(0xCF80, 0xF, 0x80)]
        public void RND_ShouldRespectMaskForDifferentRegisters(ushort opcode, byte register, byte mask)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            var value = cpu.GetV(register);
            Assert.Equal(0, value & ~mask);
        }

        [Fact]
        public void RND_ShouldWorkForAllRegisters()
        {
            var cpu = new TestableCpu();
            
            for (byte reg = 0; reg <= 0xF; reg++)
            {
                ushort opcode = (ushort)(0xC000 | (reg << 8) | 0xFF);
                cpu.WriteOpcode((ushort)(0x200 + reg * 2), opcode);
            }

            for (byte reg = 0; reg <= 0xF; reg++)
            {
                cpu.Step();
                var value = cpu.GetV(reg);
                Assert.InRange(value, (byte)0x00, (byte)0xFF);
            }
        }

        [Fact]
        public void RND_ShouldProduceDifferentValues()
        {
            var cpu = new TestableCpu();
            
            for (int i = 0; i < 5; i++)
            {
                cpu.WriteOpcode((ushort)(0x200 + i * 2), 0xC0FF); // RND V0, 0xFF
            }

            var values = new List<byte>();
            for (int i = 0; i < 5; i++)
            {
                cpu.Step();
                values.Add(cpu.GetV(0x0));
            }

            Assert.True(values.Distinct().Count() > 1, "RND should produce at least some different values");
        }

        [Fact]
        public void RND_ShouldNotAffectOtherRegisters()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x6255); // LD V2, 0x55
            cpu.WriteOpcode(0x204, 0xC0FF); // RND V0, 0xFF

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x42, cpu.GetV(0x1));
            Assert.Equal((byte)0x55, cpu.GetV(0x2));
        }

        [Fact]
        public void RND_ShouldOverwritePreviousValue()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x63AA); // LD V3, 0xAA
            cpu.WriteOpcode(0x202, 0xC3FF); // RND V3, 0xFF

            cpu.Step();
            Assert.Equal((byte)0xAA, cpu.GetV(0x3));

            cpu.Step();
            var newValue = cpu.GetV(0x3);
            Assert.InRange(newValue, (byte)0x00, (byte)0xFF);
        }

        [Fact]
        public void RND_ShouldIncrementPC()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC5FF); // RND V5, 0xFF

            cpu.Step();

            Assert.Equal((ushort)0x202, cpu.PC);
        }

        [Fact]
        public void RND_WithSameMask_CanProduceDifferentResults()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC0FF); // RND V0, 0xFF
            cpu.WriteOpcode(0x202, 0xC0FF); // RND V0, 0xFF
            cpu.WriteOpcode(0x204, 0xC0FF); // RND V0, 0xFF

            cpu.Step();
            var value1 = cpu.GetV(0x0);

            cpu.Step();
            var value2 = cpu.GetV(0x0);

            cpu.Step();
            var value3 = cpu.GetV(0x0);

            bool allSame = (value1 == value2) && (value2 == value3);
            Assert.False(allSame, "RND should produce some variation across multiple calls");
        }

        [Theory]
        [InlineData(0x03)]
        [InlineData(0x07)]
        [InlineData(0x1F)]
        [InlineData(0x3F)]
        [InlineData(0x7F)]
        public void RND_WithVariousMasks_ShouldRespectBitLimits(byte mask)
        {
            var cpu = new TestableCpu();
            ushort opcode = (ushort)(0xC000 | mask);
            
            for (int i = 0; i < 5; i++)
            {
                cpu.WriteOpcode((ushort)(0x200 + i * 2), opcode);
            }

            for (int i = 0; i < 5; i++)
            {
                cpu.Step();
                var value = cpu.GetV(0x0);
                Assert.Equal(0, value & ~mask);
                Assert.InRange(value, (byte)0x00, mask);
            }
        }

        [Fact]
        public void RND_MultipleInstructions_ShouldIndependentlyRandomize()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC0FF); // RND V0, 0xFF
            cpu.WriteOpcode(0x202, 0xC1FF); // RND V1, 0xFF
            cpu.WriteOpcode(0x204, 0xC2FF); // RND V2, 0xFF

            cpu.Step();
            cpu.Step();
            cpu.Step();

            var v0 = cpu.GetV(0x0);
            var v1 = cpu.GetV(0x1);
            var v2 = cpu.GetV(0x2);

            Assert.InRange(v0, (byte)0x00, (byte)0xFF);
            Assert.InRange(v1, (byte)0x00, (byte)0xFF);
            Assert.InRange(v2, (byte)0x00, (byte)0xFF);
        }

        [Fact]
        public void RND_WithMask80_ShouldOnlySetMSB()
        {
            var cpu = new TestableCpu();
            
            for (int i = 0; i < 10; i++)
            {
                cpu.WriteOpcode((ushort)(0x200 + i * 2), 0xC080); // RND V0, 0x80
            }

            for (int i = 0; i < 10; i++)
            {
                cpu.Step();
                var value = cpu.GetV(0x0);
                Assert.True(value == 0x00 || value == 0x80);
            }
        }

        [Fact]
        public void RND_SequentialCalls_ShouldAllStayWithinMaskBounds()
        {
            var cpu = new TestableCpu();
            byte mask = 0x3F;
            
            for (int i = 0; i < 20; i++)
            {
                ushort opcode = (ushort)(0xC000 | mask);
                cpu.WriteOpcode((ushort)(0x200 + i * 2), opcode);
            }

            for (int i = 0; i < 20; i++)
            {
                cpu.Step();
                var value = cpu.GetV(0x0);
                Assert.InRange(value, (byte)0x00, mask);
            }
        }

        #endregion

        #region CALL (2nnn) and RET (00EE) - Subroutine Tests

        [Fact]
        public void CALL_ShouldPushPCAndJumpToAddress()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x2300); // CALL 0x300

            cpu.Step();

            Assert.Equal((ushort)0x300, cpu.PC);
        }

        [Fact]
        public void CALL_ThenRET_ShouldReturnToNextInstruction()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x2300); // CALL 0x300
            cpu.WriteOpcode(0x300, 0x00EE); // RET

            cpu.Step();
            Assert.Equal((ushort)0x300, cpu.PC);

            cpu.Step();
            Assert.Equal((ushort)0x202, cpu.PC);
        }

        [Fact]
        public void CALL_MultipleNested_ShouldMaintainStack()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x2300); // CALL 0x300
            cpu.WriteOpcode(0x300, 0x2400); // CALL 0x400
            cpu.WriteOpcode(0x400, 0x00EE); // RET
            cpu.WriteOpcode(0x302, 0x00EE); // RET

            cpu.Step();
            Assert.Equal((ushort)0x300, cpu.PC);

            cpu.Step();
            Assert.Equal((ushort)0x400, cpu.PC);

            cpu.Step();
            Assert.Equal((ushort)0x302, cpu.PC);

            cpu.Step();
            Assert.Equal((ushort)0x202, cpu.PC);
        }

        [Theory]
        [InlineData(0x2250)]
        [InlineData(0x2400)]
        [InlineData(0x2FFF)]
        public void CALL_ToVariousAddresses_ShouldWork(ushort opcode)
        {
            var cpu = new TestableCpu();
            ushort expectedAddress = (ushort)(opcode & 0x0FFF);
            cpu.WriteOpcode(0x200, opcode);

            cpu.Step();

            Assert.Equal(expectedAddress, cpu.PC);
        }

        #endregion

        #region SEBYTE (3xkk) - Skip if Vx == byte Tests

        [Fact]
        public void SEBYTE_WhenEqual_ShouldSkipNextInstruction()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x3142); // SE V1, 0x42
            cpu.WriteOpcode(0x204, 0x6255); // Should be skipped

            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x206, cpu.PC);
        }

        [Fact]
        public void SEBYTE_WhenNotEqual_ShouldNotSkip()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x3143); // SE V1, 0x43

            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x204, cpu.PC);
        }

        [Fact]
        public void SEBYTE_WithZero_ShouldWork()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x3000); // SE V0, 0x00

            cpu.Step();

            Assert.Equal((ushort)0x204, cpu.PC);
        }

        #endregion

        #region SNEBYTE (4xkk) - Skip if Vx != byte Tests

        [Fact]
        public void SNEBYTE_WhenNotEqual_ShouldSkipNextInstruction()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x4143); // SNE V1, 0x43

            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x206, cpu.PC);
        }

        [Fact]
        public void SNEBYTE_WhenEqual_ShouldNotSkip()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x4142); // SNE V1, 0x42

            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x204, cpu.PC);
        }

        #endregion

        #region SEREG (5xy0) - Skip if Vx == Vy Tests

        [Fact]
        public void SEREG_WhenEqual_ShouldSkipNextInstruction()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x6242); // LD V2, 0x42
            cpu.WriteOpcode(0x204, 0x5120); // SE V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x208, cpu.PC);
        }

        [Fact]
        public void SEREG_WhenNotEqual_ShouldNotSkip()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x6243); // LD V2, 0x43
            cpu.WriteOpcode(0x204, 0x5120); // SE V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x206, cpu.PC);
        }

        #endregion

        #region LDREG (8xy0) - Load Vx = Vy Tests

        [Fact]
        public void LDREG_ShouldCopyValueFromVyToVx()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6242); // LD V2, 0x42
            cpu.WriteOpcode(0x202, 0x8120); // LD V1, V2

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x42, cpu.GetV(0x1));
            Assert.Equal((byte)0x42, cpu.GetV(0x2));
        }

        [Fact]
        public void LDREG_ShouldOverwriteVxValue()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6155); // LD V1, 0x55
            cpu.WriteOpcode(0x202, 0x6242); // LD V2, 0x42
            cpu.WriteOpcode(0x204, 0x8120); // LD V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x42, cpu.GetV(0x1));
        }

        #endregion

        #region OR (8xy1) - OR Vx, Vy Tests

        [Theory]
        [InlineData(0x00, 0x00, 0x00)]
        [InlineData(0xFF, 0x00, 0xFF)]
        [InlineData(0xF0, 0x0F, 0xFF)]
        [InlineData(0x12, 0x34, 0x36)]
        public void OR_ShouldPerformBitwiseOR(byte vxValue, byte vyValue, byte expected)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, (ushort)(0x6200 | vyValue)); // LD V2, vyValue
            cpu.WriteOpcode(0x204, 0x8121); // OR V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expected, cpu.GetV(0x1));
        }

        #endregion

        #region AND (8xy2) - AND Vx, Vy Tests

        [Theory]
        [InlineData(0xFF, 0xFF, 0xFF)]
        [InlineData(0xFF, 0x00, 0x00)]
        [InlineData(0xF0, 0x0F, 0x00)]
        [InlineData(0xF0, 0xF0, 0xF0)]
        [InlineData(0x3C, 0x66, 0x24)]
        public void AND_ShouldPerformBitwiseAND(byte vxValue, byte vyValue, byte expected)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, (ushort)(0x6200 | vyValue)); // LD V2, vyValue
            cpu.WriteOpcode(0x204, 0x8122); // AND V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expected, cpu.GetV(0x1));
        }

        #endregion

        #region XOR (8xy3) - XOR Vx, Vy Tests

        [Theory]
        [InlineData(0x00, 0x00, 0x00)]
        [InlineData(0xFF, 0xFF, 0x00)]
        [InlineData(0xFF, 0x00, 0xFF)]
        [InlineData(0xF0, 0x0F, 0xFF)]
        [InlineData(0xAA, 0x55, 0xFF)]
        public void XOR_ShouldPerformBitwiseXOR(byte vxValue, byte vyValue, byte expected)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, (ushort)(0x6200 | vyValue)); // LD V2, vyValue
            cpu.WriteOpcode(0x204, 0x8123); // XOR V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expected, cpu.GetV(0x1));
        }

        #endregion

        #region Bitwise VF Quirk (COSMAC) Tests

        [Theory]
        [InlineData(true, 0x8121, 0x00)] // OR V1, V2
        [InlineData(true, 0x8122, 0x00)] // AND V1, V2
        [InlineData(true, 0x8123, 0x00)] // XOR V1, V2
        [InlineData(false, 0x8121, 0x01)] // OR V1, V2
        [InlineData(false, 0x8122, 0x01)] // AND V1, V2
        [InlineData(false, 0x8123, 0x01)] // XOR V1, V2
        public void BitwiseOps_ShouldRespectResetCarryFlagOnBitwiseOps(bool resetCarryFlagOnBitwiseOps, ushort bitwiseOpcode, byte expectedVf)
        {
            var cpu = new TestableCpu(resetCarryFlagOnBitwiseOps);
            cpu.WriteOpcode(0x200, 0x6F01); // LD VF, 0x01
            cpu.WriteOpcode(0x202, 0x61AA); // LD V1, 0xAA
            cpu.WriteOpcode(0x204, 0x6255); // LD V2, 0x55
            cpu.WriteOpcode(0x206, bitwiseOpcode);

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedVf, cpu.GetV(0xF));
        }

        [Theory]
        [InlineData(0x8121, 0xBE)] // OR 0xAA, 0x14
        [InlineData(0x8122, 0x00)] // AND 0xAA, 0x14
        [InlineData(0x8123, 0xBE)] // XOR 0xAA, 0x14
        public void BitwiseOps_ShouldNotChangeResult_WhenResetCarryFlagOnBitwiseOpsToggles(ushort bitwiseOpcode, byte expectedVx)
        {
            var cpuWithReset = new TestableCpu(true);
            cpuWithReset.WriteOpcode(0x200, 0x61AA); // LD V1, 0xAA
            cpuWithReset.WriteOpcode(0x202, 0x6214); // LD V2, 0x14
            cpuWithReset.WriteOpcode(0x204, bitwiseOpcode);

            cpuWithReset.Step();
            cpuWithReset.Step();
            cpuWithReset.Step();

            var cpuWithoutReset = new TestableCpu(false);
            cpuWithoutReset.WriteOpcode(0x200, 0x61AA); // LD V1, 0xAA
            cpuWithoutReset.WriteOpcode(0x202, 0x6214); // LD V2, 0x14
            cpuWithoutReset.WriteOpcode(0x204, bitwiseOpcode);

            cpuWithoutReset.Step();
            cpuWithoutReset.Step();
            cpuWithoutReset.Step();

            Assert.Equal(expectedVx, cpuWithReset.GetV(0x1));
            Assert.Equal(expectedVx, cpuWithoutReset.GetV(0x1));
        }

        #endregion

        #region ADDREG (8xy4) - Add Vx, Vy with carry Tests

        [Theory]
        [InlineData(0x10, 0x20, 0x30, 0)]
        [InlineData(0xFF, 0x01, 0x00, 1)]
        [InlineData(0x80, 0x80, 0x00, 1)]
        [InlineData(0x00, 0x00, 0x00, 0)]
        public void ADDREG_ShouldAddWithCarryFlag(byte vxValue, byte vyValue, byte expectedSum, byte expectedCarry)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, (ushort)(0x6200 | vyValue)); // LD V2, vyValue
            cpu.WriteOpcode(0x204, 0x8124); // ADD V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedSum, cpu.GetV(0x1));
            Assert.Equal(expectedCarry, cpu.GetV(0xF));
        }

        #endregion

        #region SUB (8xy5) - Subtract Vy from Vx Tests

        [Theory]
        [InlineData(0x20, 0x10, 0x10, 1)]
        [InlineData(0x10, 0x20, 0xF0, 0)]
        [InlineData(0x00, 0x00, 0x00, 0)]
        [InlineData(0xFF, 0x01, 0xFE, 1)]
        public void SUB_ShouldSubtractWithBorrowFlag(byte vxValue, byte vyValue, byte expectedResult, byte expectedBorrow)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, (ushort)(0x6200 | vyValue)); // LD V2, vyValue
            cpu.WriteOpcode(0x204, 0x8125); // SUB V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedResult, cpu.GetV(0x1));
            Assert.Equal(expectedBorrow, cpu.GetV(0xF));
        }

        #endregion

        #region SHR (8xy6) - Shift right Tests

        [Theory]
        [InlineData(0x02, 0x01, 0)]
        [InlineData(0x03, 0x01, 1)]
        [InlineData(0xFF, 0x7F, 1)]
        [InlineData(0x00, 0x00, 0)]
        public void SHR_ShouldShiftRightAndSetLSB(byte vxValue, byte expectedResult, byte expectedLSB)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, 0x8106); // SHR V1

            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedResult, cpu.GetV(0x1));
            Assert.Equal(expectedLSB, cpu.GetV(0xF));
        }

        #endregion

        #region SUBN (8xy7) - Subtract Vx from Vy Tests

        [Theory]
        [InlineData(0x10, 0x20, 0x10, 1)]
        [InlineData(0x20, 0x10, 0xF0, 0)]
        [InlineData(0x00, 0x00, 0x00, 0)]
        [InlineData(0x01, 0xFF, 0xFE, 1)]
        public void SUBN_ShouldSubtractVxFromVyWithBorrowFlag(byte vxValue, byte vyValue, byte expectedResult, byte expectedBorrow)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, (ushort)(0x6200 | vyValue)); // LD V2, vyValue
            cpu.WriteOpcode(0x204, 0x8127); // SUBN V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedResult, cpu.GetV(0x1));
            Assert.Equal(expectedBorrow, cpu.GetV(0xF));
        }

        #endregion

        #region SHL (8xyE) - Shift left Tests

        [Theory]
        [InlineData(0x01, 0x02, 0)]
        [InlineData(0x80, 0x00, 1)]
        [InlineData(0xFF, 0xFE, 1)]
        [InlineData(0x00, 0x00, 0)]
        public void SHL_ShouldShiftLeftAndSetMSB(byte vxValue, byte expectedResult, byte expectedMSB)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x202, 0x810E); // SHL V1

            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedResult, cpu.GetV(0x1));
            Assert.Equal(expectedMSB, cpu.GetV(0xF));
        }

        #endregion

        #region ShiftUsesVY Quirk Tests

        [Theory]
        [InlineData(false, 0x02, 0x00)] // Uses Vx=0x04: 0x04 >> 1 => 0x02, LSB=0
        [InlineData(true, 0x01, 0x01)]  // Uses Vy=0x03: 0x03 >> 1 => 0x01, LSB=1
        public void SHR_ShouldRespectShiftUsesVY(bool shiftUsesVY, byte expectedV1, byte expectedVf)
        {
            var cpu = new TestableCpu(
                resetCarryFlagOnBitwiseOps: true,
                incrementIOnStoreLoadMemoryOps: false,
                shiftUsesVY: shiftUsesVY);
            cpu.WriteOpcode(0x200, 0x6104); // LD V1, 0x04
            cpu.WriteOpcode(0x202, 0x6203); // LD V2, 0x03
            cpu.WriteOpcode(0x204, 0x8126); // SHR V1 {, V2}

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedV1, cpu.GetV(0x1));
            Assert.Equal(expectedVf, cpu.GetV(0xF));
            Assert.Equal((byte)0x03, cpu.GetV(0x2)); // Vy should remain unchanged
        }

        [Theory]
        [InlineData(false, 0x02, 0x00)] // Uses Vx=0x01: 0x01 << 1 => 0x02, MSB=0
        [InlineData(true, 0x00, 0x01)]  // Uses Vy=0x80: 0x80 << 1 => 0x00, MSB=1
        public void SHL_ShouldRespectShiftUsesVY(bool shiftUsesVY, byte expectedV1, byte expectedVf)
        {
            var cpu = new TestableCpu(
                resetCarryFlagOnBitwiseOps: true,
                incrementIOnStoreLoadMemoryOps: false,
                shiftUsesVY: shiftUsesVY);
            cpu.WriteOpcode(0x200, 0x6101); // LD V1, 0x01
            cpu.WriteOpcode(0x202, 0x6280); // LD V2, 0x80
            cpu.WriteOpcode(0x204, 0x812E); // SHL V1 {, V2}

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedV1, cpu.GetV(0x1));
            Assert.Equal(expectedVf, cpu.GetV(0xF));
            Assert.Equal((byte)0x80, cpu.GetV(0x2)); // Vy should remain unchanged
        }

        #endregion

        #region SNEREG (9xy0) - Skip if Vx != Vy Tests

        [Fact]
        public void SNEREG_WhenNotEqual_ShouldSkipNextInstruction()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x6243); // LD V2, 0x43
            cpu.WriteOpcode(0x204, 0x9120); // SNE V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x208, cpu.PC);
        }

        [Fact]
        public void SNEREG_WhenEqual_ShouldNotSkip()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x6242); // LD V2, 0x42
            cpu.WriteOpcode(0x204, 0x9120); // SNE V1, V2

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((ushort)0x206, cpu.PC);
        }

        #endregion

        #region JPV0 (Bnnn) - Jump to V0 + addr Tests

        [Theory]
        [InlineData(0x00, 0x300, 0x300)]
        [InlineData(0x10, 0x300, 0x310)]
        [InlineData(0xFF, 0x100, 0x1FF)]
        public void JPV0_ShouldJumpToAddressPlusV0(byte v0Value, ushort address, ushort expectedPC)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6000 | v0Value)); // LD V0, v0Value
            cpu.WriteOpcode(0x202, (ushort)(0xB000 | address)); // JP V0, address

            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedPC, cpu.PC);
        }

        #endregion

        #region ADDI (Fx1E) - Add Vx to I Tests

        [Theory]
        [InlineData(0x100, 0x50, 0x150)]
        [InlineData(0x000, 0xFF, 0x0FF)]
        [InlineData(0xF00, 0x50, 0xF50)]
        public void ADDI_ShouldAddVxToI(ushort initialI, byte vxValue, ushort expectedI)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0xA000 | initialI)); // LD I, initialI
            cpu.WriteOpcode(0x202, (ushort)(0x6100 | vxValue)); // LD V1, vxValue
            cpu.WriteOpcode(0x204, 0xF11E); // ADD I, V1

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedI, cpu.I);
        }

        #endregion

        #region LDFNT (Fx29) - Load font address Tests

        [Theory]
        [InlineData(0x0, 0x000)]
        [InlineData(0x1, 0x005)]
        [InlineData(0x5, 0x019)]
        [InlineData(0xF, 0x04B)]
        public void LDFNT_ShouldLoadFontAddressForDigit(byte digit, ushort expectedAddress)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, (ushort)(0x6100 | digit)); // LD V1, digit
            cpu.WriteOpcode(0x202, 0xF129); // LD F, V1

            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedAddress, cpu.I);
        }

        #endregion

        #region LDBCD (Fx33) - Store BCD representation Tests

        [Theory]
        [InlineData(123, 1, 2, 3)]
        [InlineData(255, 2, 5, 5)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(99, 0, 9, 9)]
        public void LDBCD_ShouldStoreBCDRepresentation(byte value, byte expectedHundreds, byte expectedTens, byte expectedOnes)
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x202, (ushort)(0x6100 | value)); // LD V1, value
            cpu.WriteOpcode(0x204, 0xF133); // LD B, V1

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal(expectedHundreds, cpu.MemoryBus.Read(0x300));
            Assert.Equal(expectedTens, cpu.MemoryBus.Read(0x301));
            Assert.Equal(expectedOnes, cpu.MemoryBus.Read(0x302));
        }

        #endregion

        #region STREGI (Fx55) - Store registers to memory Tests

        [Fact]
        public void STREGI_ShouldStoreRegistersV0ToVx()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6011); // LD V0, 0x11
            cpu.WriteOpcode(0x202, 0x6122); // LD V1, 0x22
            cpu.WriteOpcode(0x204, 0x6233); // LD V2, 0x33
            cpu.WriteOpcode(0x206, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x208, 0xF255); // LD [I], V2

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x11, cpu.MemoryBus.Read(0x300));
            Assert.Equal((byte)0x22, cpu.MemoryBus.Read(0x301));
            Assert.Equal((byte)0x33, cpu.MemoryBus.Read(0x302));
        }

        [Fact]
        public void STREGI_WithV0_ShouldStoreOnlyV0()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6055); // LD V0, 0x55
            cpu.WriteOpcode(0x202, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x204, 0xF055); // LD [I], V0

            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x55, cpu.MemoryBus.Read(0x300));
        }

        [Theory]
        [InlineData(false, 0x300)]
        [InlineData(true, 0x303)]
        public void STREGI_ShouldRespectIncrementIOnStoreLoadMemoryOps(bool incrementIOnStoreLoadMemoryOps, ushort expectedI)
        {
            var cpu = new TestableCpu(
                resetCarryFlagOnBitwiseOps: true,
                incrementIOnStoreLoadMemoryOps: incrementIOnStoreLoadMemoryOps);
            cpu.WriteOpcode(0x200, 0x6011); // LD V0, 0x11
            cpu.WriteOpcode(0x202, 0x6122); // LD V1, 0x22
            cpu.WriteOpcode(0x204, 0x6233); // LD V2, 0x33
            cpu.WriteOpcode(0x206, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x208, 0xF255); // LD [I], V2

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x11, cpu.MemoryBus.Read(0x300));
            Assert.Equal((byte)0x22, cpu.MemoryBus.Read(0x301));
            Assert.Equal((byte)0x33, cpu.MemoryBus.Read(0x302));
            Assert.Equal(expectedI, cpu.I);
        }

        #endregion

        #region LDREGI (Fx65) - Load registers from memory Tests

        [Fact]
        public void LDREGI_ShouldLoadRegistersV0ToVx()
        {
            var cpu = new TestableCpu();
            cpu.MemoryBus.Write(0x300, 0xAA);
            cpu.MemoryBus.Write(0x301, 0xBB);
            cpu.MemoryBus.Write(0x302, 0xCC);
            cpu.WriteOpcode(0x200, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x202, 0xF265); // LD V2, [I]

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0xAA, cpu.GetV(0x0));
            Assert.Equal((byte)0xBB, cpu.GetV(0x1));
            Assert.Equal((byte)0xCC, cpu.GetV(0x2));
        }

        [Fact]
        public void LDREGI_WithV0_ShouldLoadOnlyV0()
        {
            var cpu = new TestableCpu();
            cpu.MemoryBus.Write(0x300, 0x77);
            cpu.WriteOpcode(0x200, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x202, 0xF065); // LD V0, [I]

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x77, cpu.GetV(0x0));
        }

        [Theory]
        [InlineData(false, 0x300)]
        [InlineData(true, 0x303)]
        public void LDREGI_ShouldRespectIncrementIOnStoreLoadMemoryOps(bool incrementIOnStoreLoadMemoryOps, ushort expectedI)
        {
            var cpu = new TestableCpu(
                resetCarryFlagOnBitwiseOps: true,
                incrementIOnStoreLoadMemoryOps: incrementIOnStoreLoadMemoryOps);
            cpu.MemoryBus.Write(0x300, 0xAA);
            cpu.MemoryBus.Write(0x301, 0xBB);
            cpu.MemoryBus.Write(0x302, 0xCC);
            cpu.WriteOpcode(0x200, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x202, 0xF265); // LD V2, [I]

            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0xAA, cpu.GetV(0x0));
            Assert.Equal((byte)0xBB, cpu.GetV(0x1));
            Assert.Equal((byte)0xCC, cpu.GetV(0x2));
            Assert.Equal(expectedI, cpu.I);
        }

        #endregion

        #region DRW ClipSprites Quirk Tests

        [Theory]
        [InlineData(true, 0x00, 0x00)]
        [InlineData(false, 0x01, 0x01)]
        public void DRW_ShouldRespectClipSprites_ForHorizontalOverflow(bool clipSprites, byte expectedWrappedPixel0, byte expectedWrappedPixel1)
        {
            var cpu = new TestableCpu(clipSprites: clipSprites);
            cpu.MemoryBus.Write(0x300, 0xF0);
            cpu.WriteOpcode(0x200, 0x603E); // LD V0, 62
            cpu.WriteOpcode(0x202, 0x6100); // LD V1, 0
            cpu.WriteOpcode(0x204, 0xA300); // LD I, 0x300 (test sprite row)
            cpu.WriteOpcode(0x206, 0xD011); // DRW V0, V1, 1

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x01, cpu.GetPixel(62, 0));
            Assert.Equal((byte)0x01, cpu.GetPixel(63, 0));
            Assert.Equal(expectedWrappedPixel0, cpu.GetPixel(0, 0));
            Assert.Equal(expectedWrappedPixel1, cpu.GetPixel(1, 0));
        }

        [Theory]
        [InlineData(true, 0x00, 0x00)]
        [InlineData(false, 0x01, 0x01)]
        public void DRW_ShouldRespectClipSprites_ForVerticalOverflow(bool clipSprites, byte expectedWrappedPixel0, byte expectedWrappedPixel3)
        {
            var cpu = new TestableCpu(clipSprites: clipSprites);
            cpu.MemoryBus.Write(0x300, 0xF0);
            cpu.MemoryBus.Write(0x301, 0x90);
            cpu.WriteOpcode(0x200, 0x6000); // LD V0, 0
            cpu.WriteOpcode(0x202, 0x611F); // LD V1, 31
            cpu.WriteOpcode(0x204, 0xA300); // LD I, 0x300 (test sprite rows)
            cpu.WriteOpcode(0x206, 0xD012); // DRW V0, V1, 2

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x01, cpu.GetPixel(0, 31));
            Assert.Equal((byte)0x01, cpu.GetPixel(3, 31));
            Assert.Equal(expectedWrappedPixel0, cpu.GetPixel(0, 0));
            Assert.Equal(expectedWrappedPixel3, cpu.GetPixel(3, 0));
        }

        [Theory]
        [InlineData(true, 0x00)]
        [InlineData(false, 0x01)]
        public void DRW_ShouldWrapStartingCoordinates_BeforeApplyingClipBehavior(bool clipSprites, byte expectedWrappedXPixel)
        {
            var cpu = new TestableCpu(clipSprites: clipSprites);
            cpu.MemoryBus.Write(0x300, 0xC0); // 1100_0000
            cpu.WriteOpcode(0x200, 0x60FF); // LD V0, 255 -> x = 255 % 64 = 63
            cpu.WriteOpcode(0x202, 0x617F); // LD V1, 127 -> y = 127 % 32 = 31
            cpu.WriteOpcode(0x204, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x206, 0xD011); // DRW V0, V1, 1

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x01, cpu.GetPixel(63, 31));
            Assert.Equal(expectedWrappedXPixel, cpu.GetPixel(0, 31));
        }

        #endregion

        #region Combined Instruction Tests

        [Fact]
        public void CombinedInstructions_ShouldExecuteSequentially()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6142); // LD V1, 0x42
            cpu.WriteOpcode(0x202, 0x7105); // ADD V1, 0x05
            cpu.WriteOpcode(0x204, 0xA300); // LD I, 0x300
            cpu.WriteOpcode(0x206, 0x1208); // JP 0x208

            cpu.Step();
            Assert.Equal((byte)0x42, cpu.GetV(0x1));

            cpu.Step();
            Assert.Equal((byte)0x47, cpu.GetV(0x1));

            cpu.Step();
            Assert.Equal((ushort)0x300, cpu.I);

            cpu.Step();
            Assert.Equal((ushort)0x208, cpu.PC);
        }

        [Fact]
        public void CombinedInstructions_ShouldHandleJumpAndLoad()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x1204); // JP 0x204
            cpu.WriteOpcode(0x202, 0x6555); // This should be skipped
            cpu.WriteOpcode(0x204, 0x6AAA); // LD VA, 0xAA

            cpu.Step();
            Assert.Equal((ushort)0x204, cpu.PC);

            cpu.Step();
            Assert.Equal((byte)0xAA, cpu.GetV(0xA));
            Assert.Equal((byte)0x00, cpu.GetV(0x5));
        }

        [Fact]
        public void CombinedInstructions_ShouldHandleMultipleLoadsAndAdds()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0x6010); // LD V0, 0x10
            cpu.WriteOpcode(0x202, 0x6120); // LD V1, 0x20
            cpu.WriteOpcode(0x204, 0x7005); // ADD V0, 0x05
            cpu.WriteOpcode(0x206, 0x710A); // ADD V1, 0x0A

            cpu.Step();
            cpu.Step();
            cpu.Step();
            cpu.Step();

            Assert.Equal((byte)0x15, cpu.GetV(0x0));
            Assert.Equal((byte)0x2A, cpu.GetV(0x1));
        }

        [Fact]
        public void CombinedInstructions_RNDAndADD_ShouldWorkTogether()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC00F); // RND V0, 0x0F (0-15)
            cpu.WriteOpcode(0x202, 0x7010); // ADD V0, 0x10

            cpu.Step();
            var randomValue = cpu.GetV(0x0);
            Assert.InRange(randomValue, (byte)0x00, (byte)0x0F);

            cpu.Step();
            var finalValue = cpu.GetV(0x0);
            Assert.Equal((byte)(randomValue + 0x10), finalValue);
        }

        [Fact]
        public void CombinedInstructions_RNDToMultipleRegisters_ShouldIndependentlySet()
        {
            var cpu = new TestableCpu();
            cpu.WriteOpcode(0x200, 0xC0FF); // RND V0, 0xFF
            cpu.WriteOpcode(0x202, 0xC1FF); // RND V1, 0xFF
            cpu.WriteOpcode(0x204, 0xC27F); // RND V2, 0x7F

            cpu.Step();
            cpu.Step();
            cpu.Step();

            var v0 = cpu.GetV(0x0);
            var v1 = cpu.GetV(0x1);
            var v2 = cpu.GetV(0x2);

            Assert.InRange(v0, (byte)0x00, (byte)0xFF);
            Assert.InRange(v1, (byte)0x00, (byte)0xFF);
            Assert.InRange(v2, (byte)0x00, (byte)0x7F);
        }

        #endregion
    }
}
