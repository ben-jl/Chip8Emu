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
