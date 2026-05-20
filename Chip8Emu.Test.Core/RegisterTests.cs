using Chip8Emu.Core.Cpu;
using Chip8Emu.Core.Memory;

namespace Chip8Emu.Test.Core
{
    public class RegisterTests
    {
        private Registers CreateRegisters()
        {
            var memoryMap = new MemoryMap();
            return new Registers(memoryMap);
        }

        [Fact]
        public void Constructor_ShouldInitializeRegistersToZero()
        {
            var registers = CreateRegisters();

            for (byte i = 0; i <= 0xF; i++)
            {
                Assert.Equal(0, registers.GetV(i));
            }
        }

        [Fact]
        public void Constructor_ShouldInitializeIRegisterToZero()
        {
            var registers = CreateRegisters();
            Assert.Equal((ushort)0, registers.I);
        }

        [Fact]
        public void Constructor_ShouldInitializePCToRomStart()
        {
            var memoryMap = new MemoryMap();
            var registers = new Registers(memoryMap);
            Assert.Equal(memoryMap.RomStart, registers.PC);
        }

        [Fact]
        public void Constructor_ShouldInitializeSPToZero()
        {
            var registers = CreateRegisters();
            Assert.Equal((byte)0, registers.SP);
        }

        [Fact]
        public void Constructor_ShouldInitializeDelayTimerToZero()
        {
            var registers = CreateRegisters();
            Assert.Equal((byte)0, registers.Delay);
        }

        [Fact]
        public void Constructor_ShouldInitializeSoundTimerToZero()
        {
            var registers = CreateRegisters();
            Assert.Equal((byte)0, registers.Sound);
        }

        [Fact]
        public void Constructor_ShouldInitializeStackToZeros()
        {
            var registers = CreateRegisters();
            Assert.NotNull(registers.Stack);
            Assert.Equal(16, registers.Stack.Length);
            for (int i = 0; i < registers.Stack.Length; i++)
            {
                Assert.Equal((ushort)0, registers.Stack[i]);
            }
        }

        [Theory]
        [InlineData(0x0, 0x42)]
        [InlineData(0x1, 0xAB)]
        [InlineData(0x7, 0xFF)]
        [InlineData(0xE, 0x00)]
        [InlineData(0xF, 0x55)]
        public void SetV_ShouldSetRegisterValue(byte register, byte value)
        {
            var registers = CreateRegisters();
            byte result = registers.SetV(register, value);
            Assert.Equal(value, result);
            Assert.Equal(value, registers.GetV(register));
        }

        [Theory]
        [InlineData(0x0)]
        [InlineData(0x5)]
        [InlineData(0xA)]
        [InlineData(0xF)]
        public void GetV_ShouldReturnDefaultZero(byte register)
        {
            var registers = CreateRegisters();
            Assert.Equal((byte)0, registers.GetV(register));
        }

        [Theory]
        [InlineData(0x10)]
        [InlineData(0x20)]
        [InlineData(0xFF)]
        public void GetV_ShouldThrowException_WhenRegisterOutOfBounds(byte register)
        {
            var registers = CreateRegisters();
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => registers.GetV(register));
            Assert.Equal("register", exception.ParamName);
            Assert.Contains("Register must be between 0x0 and 0xF", exception.Message);
        }

        [Theory]
        [InlineData(0x10)]
        [InlineData(0x20)]
        [InlineData(0xFF)]
        public void SetV_ShouldThrowException_WhenRegisterOutOfBounds(byte register)
        {
            var registers = CreateRegisters();
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => registers.SetV(register, 0x42));
            Assert.Equal("register", exception.ParamName);
            Assert.Contains("Register must be between 0x0 and 0xF", exception.Message);
        }

        [Fact]
        public void SetV_ShouldNotAffectOtherRegisters()
        {
            var registers = CreateRegisters();
            registers.SetV(0x5, 0xAA);
            registers.SetV(0xA, 0xBB);
            
            Assert.Equal((byte)0xAA, registers.GetV(0x5));
            Assert.Equal((byte)0xBB, registers.GetV(0xA));
            Assert.Equal((byte)0, registers.GetV(0x0));
            Assert.Equal((byte)0, registers.GetV(0xF));
        }

        [Fact]
        public void SetV_ShouldOverwritePreviousValue()
        {
            var registers = CreateRegisters();
            registers.SetV(0x3, 0x11);
            registers.SetV(0x3, 0x22);
            Assert.Equal((byte)0x22, registers.GetV(0x3));
        }

        [Theory]
        [InlineData(0x200)]
        [InlineData(0x400)]
        [InlineData(0xFFF)]
        public void Push_ShouldAddAddressToStack(ushort address)
        {
            var registers = CreateRegisters();
            registers.Push(address);
            Assert.Equal((byte)1, registers.SP);
            Assert.Equal(address, registers.Stack[0]);
        }

        [Fact]
        public void Push_ShouldIncrementStackPointer()
        {
            var registers = CreateRegisters();
            registers.Push(0x200);
            registers.Push(0x300);
            registers.Push(0x400);
            
            Assert.Equal((byte)3, registers.SP);
            Assert.Equal((ushort)0x200, registers.Stack[0]);
            Assert.Equal((ushort)0x300, registers.Stack[1]);
            Assert.Equal((ushort)0x400, registers.Stack[2]);
        }

        [Fact]
        public void Push_ShouldFillStackUpToCapacity()
        {
            var registers = CreateRegisters();
            for (ushort i = 0; i < 16; i++)
            {
                registers.Push((ushort)(0x200 + i * 0x10));
            }
            
            Assert.Equal((byte)16, registers.SP);
            for (int i = 0; i < 16; i++)
            {
                Assert.Equal((ushort)(0x200 + i * 0x10), registers.Stack[i]);
            }
        }

        [Fact]
        public void Push_ShouldThrowException_WhenStackOverflows()
        {
            var registers = CreateRegisters();
            for (int i = 0; i < 16; i++)
            {
                registers.Push(0x200);
            }
            
            var exception = Assert.Throws<StackOverflowException>(() => registers.Push(0x200));
            Assert.Contains("Stack overflow: cannot push more than 16 addresses", exception.Message);
        }

        [Fact]
        public void Pop_ShouldReturnLastPushedAddress()
        {
            var registers = CreateRegisters();
            registers.Push(0x200);
            ushort address = registers.Pop();
            
            Assert.Equal((ushort)0x200, address);
            Assert.Equal((byte)0, registers.SP);
        }

        [Fact]
        public void Pop_ShouldDecrementStackPointer()
        {
            var registers = CreateRegisters();
            registers.Push(0x200);
            registers.Push(0x300);
            registers.Push(0x400);
            
            Assert.Equal((byte)3, registers.SP);
            registers.Pop();
            Assert.Equal((byte)2, registers.SP);
            registers.Pop();
            Assert.Equal((byte)1, registers.SP);
        }

        [Fact]
        public void Pop_ShouldReturnAddressesInLIFOOrder()
        {
            var registers = CreateRegisters();
            registers.Push(0x200);
            registers.Push(0x300);
            registers.Push(0x400);
            
            Assert.Equal((ushort)0x400, registers.Pop());
            Assert.Equal((ushort)0x300, registers.Pop());
            Assert.Equal((ushort)0x200, registers.Pop());
        }

        [Fact]
        public void Pop_ShouldThrowException_WhenStackIsEmpty()
        {
            var registers = CreateRegisters();
            var exception = Assert.Throws<InvalidOperationException>(() => registers.Pop());
            Assert.Contains("Stack underflow: cannot pop from an empty stack", exception.Message);
        }

        [Fact]
        public void Pop_ShouldThrowException_WhenPoppingAfterAllItemsRemoved()
        {
            var registers = CreateRegisters();
            registers.Push(0x200);
            registers.Pop();
            
            var exception = Assert.Throws<InvalidOperationException>(() => registers.Pop());
            Assert.Contains("Stack underflow: cannot pop from an empty stack", exception.Message);
        }

        [Fact]
        public void PushPop_ShouldWorkCorrectlyForComplexSequence()
        {
            var registers = CreateRegisters();
            
            registers.Push(0x200);
            registers.Push(0x300);
            Assert.Equal((ushort)0x300, registers.Pop());
            
            registers.Push(0x400);
            registers.Push(0x500);
            Assert.Equal((ushort)0x500, registers.Pop());
            Assert.Equal((ushort)0x400, registers.Pop());
            Assert.Equal((ushort)0x200, registers.Pop());
            
            Assert.Equal((byte)0, registers.SP);
        }

        [Fact]
        public void Stack_ShouldBeAccessibleDirectly()
        {
            var registers = CreateRegisters();
            registers.Push(0x200);
            registers.Push(0x300);
            
            Assert.Equal((ushort)0x200, registers.Stack[0]);
            Assert.Equal((ushort)0x300, registers.Stack[1]);
        }

        [Fact]
        public void AllRegisters_ShouldBeIndependent()
        {
            var registers = CreateRegisters();
            
            registers.SetV(0x0, 0x11);
            registers.SetV(0x1, 0x22);
            registers.SetV(0x2, 0x33);
            registers.SetV(0x3, 0x44);
            registers.SetV(0x4, 0x55);
            registers.SetV(0x5, 0x66);
            registers.SetV(0x6, 0x77);
            registers.SetV(0x7, 0x88);
            registers.SetV(0x8, 0x99);
            registers.SetV(0x9, 0xAA);
            registers.SetV(0xA, 0xBB);
            registers.SetV(0xB, 0xCC);
            registers.SetV(0xC, 0xDD);
            registers.SetV(0xD, 0xEE);
            registers.SetV(0xE, 0xFF);
            registers.SetV(0xF, 0x00);
            
            Assert.Equal((byte)0x11, registers.GetV(0x0));
            Assert.Equal((byte)0x22, registers.GetV(0x1));
            Assert.Equal((byte)0x33, registers.GetV(0x2));
            Assert.Equal((byte)0x44, registers.GetV(0x3));
            Assert.Equal((byte)0x55, registers.GetV(0x4));
            Assert.Equal((byte)0x66, registers.GetV(0x5));
            Assert.Equal((byte)0x77, registers.GetV(0x6));
            Assert.Equal((byte)0x88, registers.GetV(0x7));
            Assert.Equal((byte)0x99, registers.GetV(0x8));
            Assert.Equal((byte)0xAA, registers.GetV(0x9));
            Assert.Equal((byte)0xBB, registers.GetV(0xA));
            Assert.Equal((byte)0xCC, registers.GetV(0xB));
            Assert.Equal((byte)0xDD, registers.GetV(0xC));
            Assert.Equal((byte)0xEE, registers.GetV(0xD));
            Assert.Equal((byte)0xFF, registers.GetV(0xE));
            Assert.Equal((byte)0x00, registers.GetV(0xF));
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x7F)]
        [InlineData(0xFF)]
        public void SetV_ShouldHandleFullByteRange(byte value)
        {
            var registers = CreateRegisters();
            registers.SetV(0x5, value);
            Assert.Equal(value, registers.GetV(0x5));
        }

        [Theory]
        [InlineData(0x0000)]
        [InlineData(0x7FFF)]
        [InlineData(0xFFFF)]
        public void Push_ShouldHandleFullUshortRange(ushort address)
        {
            var registers = CreateRegisters();
            registers.Push(address);
            Assert.Equal(address, registers.Pop());
        }
    }
}
