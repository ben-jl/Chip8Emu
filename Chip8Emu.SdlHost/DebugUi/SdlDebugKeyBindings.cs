using SDL3;

namespace Chip8Emu.SdlHost.Debug
{
    internal static class SdlDebugKeyBindings
    {
        public static bool TryMap(SDL.Keycode key, out SdlDebugCommand command)
        {
            command = key switch
            {
                SDL.Keycode.F1 => SdlDebugCommand.ToggleOverlay,
                SDL.Keycode.F5 => SdlDebugCommand.PauseResume,
                SDL.Keycode.F10 => SdlDebugCommand.StepInstruction,
                SDL.Keycode.Tab => SdlDebugCommand.NextPanel,
                SDL.Keycode.Backspace => SdlDebugCommand.PreviousPanel,
                SDL.Keycode.Up => SdlDebugCommand.MoveUp,
                SDL.Keycode.Down => SdlDebugCommand.MoveDown,
                SDL.Keycode.B => SdlDebugCommand.ToggleAddressBreakpoint,
                SDL.Keycode.O => SdlDebugCommand.ToggleOpcodeBreakpoint,
                SDL.Keycode.Delete => SdlDebugCommand.RemoveSelectedBreakpoint,
                _ => SdlDebugCommand.None
            };

            return command != SdlDebugCommand.None;
        }
    }
}
