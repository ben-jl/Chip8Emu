using Chip8Emu.Core.Machine;
using SDL3;

namespace Chip8.SdlHost;

public static class SdlInput
{
    public static void HandleEvent(SDL.Event e, Chip8Machine machine)
    {
        var eventType = (SDL.EventType)e.Type;

        if (eventType != SDL.EventType.KeyDown &&
            eventType != SDL.EventType.KeyUp)
        {
            return;
        }

        if (!TryMapKey(e.Key.Key, out var chip8Key))
            return;

        bool pressed = eventType == SDL.EventType.KeyDown;

        machine.SetKeyState(chip8Key, pressed);
    }

    private static bool TryMapKey(SDL.Keycode key, out byte chip8Key)
    {
        chip8Key = key switch
        {
            SDL.Keycode.Alpha1 => 0x1,
            SDL.Keycode.Alpha2 => 0x2,
            SDL.Keycode.Alpha3 => 0x3,
            SDL.Keycode.Alpha4 => 0xC,

            SDL.Keycode.Q => 0x4,
            SDL.Keycode.W => 0x5,
            SDL.Keycode.E => 0x6,
            SDL.Keycode.R => 0xD,

            SDL.Keycode.A => 0x7,
            SDL.Keycode.S => 0x8,
            SDL.Keycode.D => 0x9,
            SDL.Keycode.F => 0xE,

            SDL.Keycode.Z => 0xA,
            SDL.Keycode.X => 0x0,
            SDL.Keycode.C => 0xB,
            SDL.Keycode.V => 0xF,

            _ => 0xFF
        };

        return chip8Key != 0xFF;
    }
}