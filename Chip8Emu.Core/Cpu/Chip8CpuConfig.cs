namespace Chip8Emu.Core.Cpu
{
    public sealed record Chip8CpuConfig(
        bool ResetCarryFlagOnBitwiseOps = true,
        bool IncrementIOnStoreLoadMemoryOps = true,
        bool ShiftUsesVy = true,
        bool ClipSprites = true,
        bool JumpWithV0 = true)
    {
        public static Chip8CpuConfig Default { get; } = new();
    }
}
