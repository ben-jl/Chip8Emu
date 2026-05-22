using System;

namespace Chip8Emu.Core.Display
{
    public sealed class MonochromeFrameBuffer : IFrameBuffer
    {
        private byte[] _buffer = Array.Empty<byte>();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public ReadOnlySpan<byte> Buffer => _buffer;

        public MonochromeFrameBuffer(int width, int height)
        {
            SetResolution(width, height);
        }

        public bool XorPixel(int x, int y)
        {
            x %= Width;
            y %= Height;

            int index = y * Width + x;

            bool erased = _buffer[index] == 1;
            _buffer[index] ^= 1;

            return erased;
        }

        public void Clear()
        {
            Array.Clear(_buffer);
        }

        public void SetResolution(int width, int height)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

            Width = width;
            Height = height;
            _buffer = new byte[width * height];
        }
    }
}
