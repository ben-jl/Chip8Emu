using Chip8Emu.Core.Memory;

namespace Chip8Emu.Test.Core
{
    public class MemoryBusTests
    {
        [Fact]
        public void Read_ShouldReturnWrittenValue()
        {
            var memoryBus = new MemoryBus(4096);
            ushort address = 0x300;
            byte value = 0xAB;
            memoryBus.Write(address, value);
            byte readValue = memoryBus.Read(address);
            Assert.Equal(value, readValue);
        }

        [Fact]
        public void WriteBlock_ShouldWriteDataToMemory()
        {
            var memoryBus = new MemoryBus(4096);
            ushort startAddress = 0x300;
            byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            memoryBus.WriteBlock(startAddress, data);
            for (int i = 0; i < data.Length; i++)
            {
                byte readValue = memoryBus.Read((ushort)(startAddress + i));
                Assert.Equal(data[i], readValue);
            }
        }

        [Fact]
        public void Constructor_ShouldInitializeMemoryToZero()
        {
            var memoryBus = new MemoryBus(16);

            for (ushort address = 0; address < 16; address++)
            {
                Assert.Equal((byte)0, memoryBus.Read(address));
            }
        }

        [Fact]
        public void Clear_ShouldResetWrittenMemoryToZero()
        {
            var memoryBus = new MemoryBus(16);
            memoryBus.Write(0, 0xAA);
            memoryBus.Write(15, 0xBB);

            memoryBus.Clear();

            Assert.Equal((byte)0, memoryBus.Read(0));
            Assert.Equal((byte)0, memoryBus.Read(15));
        }

        [Fact]
        public void WriteBlock_ShouldAllowExactFitAtEndOfMemory()
        {
            var memoryBus = new MemoryBus(4);

            memoryBus.WriteBlock(2, [0xAA, 0xBB]);

            Assert.Equal((byte)0xAA, memoryBus.Read(2));
            Assert.Equal((byte)0xBB, memoryBus.Read(3));
        }

        [Fact]
        public void WriteBlock_ShouldAllowEmptyBlockAtMemoryEnd()
        {
            var memoryBus = new MemoryBus(4);

            memoryBus.WriteBlock(4, []);

            Assert.Equal((byte)0, memoryBus.Read(0));
            Assert.Equal((byte)0, memoryBus.Read(3));
        }

        [Fact]
        public void ReadAndWrite_ShouldAllowLastValidAddress()
        {
            var memoryBus = new MemoryBus(4);

            memoryBus.Write(3, 0xCC);

            Assert.Equal((byte)0xCC, memoryBus.Read(3));
        }

        [Fact]
        public void Read_ShouldThrowException_WhenAddressOutOfBounds()
        {
            var memoryBus = new MemoryBus(4096);
            ushort address = 65535; // Out of bounds
            Assert.Throws<IndexOutOfRangeException>(() => memoryBus.Read(address));
        }

        [Fact]
        public void Write_ShouldThrowException_WhenAddressOutOfBounds()
        {
            var memoryBus = new MemoryBus(4096);
            ushort address = 65535; // Out of bounds
            Assert.Throws<IndexOutOfRangeException>(() => memoryBus.Write(address, 0xFF));
        }

        [Fact]
        public void WriteBlock_ShouldThrowException_WhenDataExceedsMemoryBounds()
        {
            var memoryBus = new MemoryBus(4096);
            ushort startAddress = 4092; // Near the end of memory
            byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; // Exceeds memory bounds
            Assert.Throws<IndexOutOfRangeException>(() => memoryBus.WriteBlock(startAddress, data));
        }
    }
}
