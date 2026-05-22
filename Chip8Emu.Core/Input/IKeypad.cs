using System;

namespace Chip8Emu.Core.Input
{
    public interface IKeypad
    {
        bool IsPressed(byte key);
        byte? FirstPressedKey { get; }
    }
}
