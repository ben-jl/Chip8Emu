using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Display
{
    public interface IFrameBuffer
    {
        int Width { get; }
        int Height { get; }
        ReadOnlySpan<byte> Buffer { get; }

        bool XorPixel(int x, int y);
        void Clear();
    }
}
