using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class EmulationOptionsTests
    {
        [Fact]
        public void Default_ShouldLockCurrentCompatibilityProfile()
        {
            var options = EmulationOptions.Default;

            Assert.Equal(50, options.InstructionsPerFrame);
            Assert.True(options.ShiftUsesVy);
            Assert.True(options.JumpWithV0);
            Assert.True(options.ClipSprites);
            Assert.True(options.ResetCarryFlagOnBitwiseOps);
            Assert.True(options.IncrementIOnStoreLoadMemoryOps);
            Assert.True(options.DisplayWaitOnDraw);
            Assert.Equal(DisplayWaitScope.AllDisplayModes, options.DisplayWaitScope);
        }

        [Fact]
        public void Default_ShouldReturnSingletonInstance()
        {
            Assert.Same(EmulationOptions.Default, EmulationOptions.Default);
        }
    }
}
