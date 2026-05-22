namespace Chip8Emu.SdlHost.Debug
{
    internal enum SdlDebugCommand
    {
        None = 0,
        ToggleOverlay,
        PauseResume,
        StepInstruction,
        NextPanel,
        PreviousPanel,
        MoveUp,
        MoveDown,
        ToggleAddressBreakpoint,
        ToggleOpcodeBreakpoint,
        RemoveSelectedBreakpoint
    }
}
