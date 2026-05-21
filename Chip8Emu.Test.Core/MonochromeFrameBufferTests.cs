using Chip8Emu.Core.Display;

namespace Chip8Emu.Test.Core
{
    public class MonochromeFrameBufferTests
    {
        [Fact]
        public void Constructor_ShouldExposeDimensionsAndZeroedBuffer()
        {
            var frameBuffer = new MonochromeFrameBuffer(64, 32);

            Assert.Equal(64, frameBuffer.Width);
            Assert.Equal(32, frameBuffer.Height);
            Assert.Equal(64 * 32, frameBuffer.Buffer.Length);
            Assert.True(frameBuffer.Buffer.ToArray().All(pixel => pixel == 0));
        }

        [Fact]
        public void XorPixel_ShouldTogglePixelAndReturnWhetherPixelWasErased()
        {
            var frameBuffer = new MonochromeFrameBuffer(64, 32);

            var firstErased = frameBuffer.XorPixel(3, 4);
            var secondErased = frameBuffer.XorPixel(3, 4);

            Assert.False(firstErased);
            Assert.True(secondErased);
            Assert.Equal((byte)0, frameBuffer.Buffer[4 * 64 + 3]);
        }

        [Fact]
        public void XorPixel_ShouldWrapPositiveCoordinates()
        {
            var frameBuffer = new MonochromeFrameBuffer(64, 32);

            frameBuffer.XorPixel(64 + 5, 32 + 7);

            Assert.Equal((byte)1, frameBuffer.Buffer[7 * 64 + 5]);
        }

        [Fact]
        public void Clear_ShouldResetEveryPixel()
        {
            var frameBuffer = new MonochromeFrameBuffer(64, 32);
            frameBuffer.XorPixel(0, 0);
            frameBuffer.XorPixel(63, 31);

            frameBuffer.Clear();

            Assert.True(frameBuffer.Buffer.ToArray().All(pixel => pixel == 0));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        public void XorPixel_ShouldThrowForNegativeCoordinates(int x, int y)
        {
            var frameBuffer = new MonochromeFrameBuffer(64, 32);

            Assert.Throws<IndexOutOfRangeException>(() => frameBuffer.XorPixel(x, y));
        }
    }
}
