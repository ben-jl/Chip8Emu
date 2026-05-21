using Chip8Emu.Core.Memory;

namespace Chip8Emu.Test.Core
{
    public class MemoryMapTests
    {
        [Fact]
        public void Constructor_ShouldLockCurrentMemoryLayout()
        {
            var memoryMap = new MemoryMap();

            Assert.Equal((ushort)0x200, memoryMap.RomStart);
            Assert.Equal((ushort)0x000, memoryMap.FontStart);
            Assert.Equal((ushort)0x005, memoryMap.FontSize);
            Assert.Equal((ushort)4096, memoryMap.MemorySize);
        }

        [Theory]
        [InlineData(0x0, 0x0)]
        [InlineData(0x1, 0x5)]
        [InlineData(0x2, 0xA)]
        [InlineData(0x3, 0xF)]
        [InlineData(0x4, 0x14)]
        [InlineData(0x5, 0x19)]
        [InlineData(0x6, 0x1E)]
        [InlineData(0x7, 0x23)]
        [InlineData(0x8, 0x28)]
        [InlineData(0x9, 0x2D)]
        [InlineData(0xA, 0x32)]
        [InlineData(0xB, 0x37)]
        [InlineData(0xC, 0x3C)]
        [InlineData(0xD, 0x41)]
        [InlineData(0xE, 0x46)]
        [InlineData(0xF, 0x4B)]
        public void GetFontAddress_ValidCharacter_ReturnsCorrectAddress(ushort character, ushort expectedAddress)
        {
            var memoryMap = new MemoryMap();
            var address = memoryMap.GetFontAddress(character);
            Assert.Equal(expectedAddress, address);
        }

        [Theory]
        [InlineData(0x10)]
        [InlineData(0xFFFF)]
        public void GetFontAddress_InvalidCharacter_ShouldThrow(ushort character)
        {
            var memoryMap = new MemoryMap();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => memoryMap.GetFontAddress(character));
            Assert.Equal("character", exception.ParamName);
        }
    }
}
