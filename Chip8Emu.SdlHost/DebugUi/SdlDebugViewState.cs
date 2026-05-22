namespace Chip8Emu.SdlHost.Debug
{
    internal sealed class SdlDebugViewState
    {
        public bool OverlayVisible { get; set; }
        public SdlDebugPanel ActivePanel { get; set; } = SdlDebugPanel.Cpu;
        public int BreakpointSelection { get; set; }
        public int TraceOffset { get; set; }
        public string LastAction { get; set; } = "F1 overlay | F5 pause/resume | F10 step";

        public bool IsDebugInputMode => OverlayVisible;
    }
}
