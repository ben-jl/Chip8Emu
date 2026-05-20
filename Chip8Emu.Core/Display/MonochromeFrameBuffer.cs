using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Display
{
    public sealed class MonochromeFrameBuffer : IFrameBuffer
    {
        private readonly byte[] _buffer;

        public int Width { get; }
        public int Height { get; }

        public ReadOnlySpan<byte> Buffer => _buffer;

        public MonochromeFrameBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            _buffer = new byte[width * height];
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
    }
}
