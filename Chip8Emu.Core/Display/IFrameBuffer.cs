using System;

namespace Chip8Emu.Core.Display
{
    public interface IFrameBuffer
    {
        int Width { get; }
        int Height { get; }
        ReadOnlySpan<byte> Buffer { get; }

        bool XorPixel(int x, int y);
        void SetResolution(int width, int height);
        void Clear();
    }
}
