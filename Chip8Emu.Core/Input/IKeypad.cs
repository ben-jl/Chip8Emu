using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Input
{
    public interface IKeypad
    {
        bool IsPressed(byte key);
        byte? FirstPressedKey { get; }
    }
}
