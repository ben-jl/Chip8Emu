using Chip8Emu.Core.Cpu;
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

            public TestableCpu()
            {
                var memoryMap = new MemoryMap();
                _memoryBus = new MemoryBus(memoryMap.MemorySize);
                _cpu = new Chip8Cpu(_memoryBus, memoryMap);
                
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

        #endregion
    }
}
