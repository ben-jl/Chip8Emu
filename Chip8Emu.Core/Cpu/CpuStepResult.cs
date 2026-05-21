namespace Chip8Emu.Core.Cpu
{
    internal readonly record struct CpuStepResult(
        bool DrewSprite,
        bool WaitingForDrawVBlank)
    {
        public static CpuStepResult None => new(false, false);
    }
}
