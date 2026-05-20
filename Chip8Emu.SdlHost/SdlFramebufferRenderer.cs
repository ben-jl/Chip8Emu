using SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.SdlHost
{
    public sealed class SdlFramebufferRenderer
    {
        private readonly nint _renderer;
        private readonly int _scale;

        public SdlFramebufferRenderer(nint renderer, int scale)
        {
            _renderer = renderer;
            _scale = scale;
        }

        public void Render(ReadOnlySpan<byte> buffer, int width, int height)
        {
            SDL.SetRenderDrawColor(_renderer, 16, 16, 16, 255);
            SDL.RenderClear(_renderer);

            SDL.SetRenderDrawColor(_renderer, 230, 230, 230, 255);

            for(var y = 0; y < height; y++)
            {
                for(var x = 0; x < width; x++)
                {
                    var pixel = buffer[y * width + x];

                    if(pixel == 0)
                    {
                        continue;
                    }

                    var rect = new SDL.FRect
                    {
                        X = x * _scale,
                        Y = y * _scale,
                        W = _scale,
                        H = _scale
                    };

                    SDL.RenderFillRect(_renderer, ref rect);
                }
            }

            SDL.RenderPresent(_renderer);
        }
    }
}
