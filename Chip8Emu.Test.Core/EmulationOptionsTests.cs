using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class EmulationOptionsTests
    {
        [Fact]
        public void Default_ShouldLockCurrentCompatibilityProfile()
        {
            var options = EmulationOptions.Default;

            Assert.Equal(10, options.InstructionsPerFrame);
            Assert.False(options.ShiftUsesVy);
            Assert.True(options.JumpWithV0);
            Assert.True(options.ClipSprites);
            Assert.True(options.ResetCarryFlagOnBitwiseOps);
            Assert.False(options.IncrementIOnStoreLoadMemoryOps);
            Assert.False(options.DisplayWaitOnDraw);
            Assert.Equal(DisplayWaitScope.AllDisplayModes, options.DisplayWaitScope);
        }

        [Fact]
        public void Default_ShouldReturnSingletonInstance()
        {
            Assert.Same(EmulationOptions.Default, EmulationOptions.Default);
        }
    }
}
