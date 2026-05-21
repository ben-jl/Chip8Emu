using Chip8Emu.Core.Timing;

namespace Chip8Emu.Test.Core
{
    public class TimersTests
    {
        [Fact]
        public void Tick_ShouldLeaveZeroTimersAtZero()
        {
            var timers = new Timers();

            timers.Tick();

            Assert.Equal((byte)0, timers.DelayTimer);
            Assert.Equal((byte)0, timers.SoundTimer);
        }

        [Fact]
        public void Tick_ShouldDecrementNonZeroTimersAndClampAtZero()
        {
            var timers = new Timers
            {
                DelayTimer = 2,
                SoundTimer = 1
            };

            timers.Tick();
            timers.Tick();
            timers.Tick();

            Assert.Equal((byte)0, timers.DelayTimer);
            Assert.Equal((byte)0, timers.SoundTimer);
        }

        [Fact]
        public void Tick_ShouldDecrementFreshWritesImmediatelyByDefault()
        {
            var timers = new Timers
            {
                DelayTimer = 5,
                SoundTimer = 7
            };

            timers.Tick();

            Assert.Equal((byte)4, timers.DelayTimer);
            Assert.Equal((byte)6, timers.SoundTimer);
        }

        [Fact]
        public void Tick_ShouldSkipFreshWritesOnce_WhenRequested()
        {
            var timers = new Timers
            {
                DelayTimer = 5,
                SoundTimer = 7
            };

            timers.Tick(skipFreshWrites: true);
            Assert.Equal((byte)5, timers.DelayTimer);
            Assert.Equal((byte)7, timers.SoundTimer);

            timers.Tick(skipFreshWrites: true);
            Assert.Equal((byte)4, timers.DelayTimer);
            Assert.Equal((byte)6, timers.SoundTimer);
        }

        [Fact]
        public void Tick_ShouldTrackFreshWritesIndependentlyForDelayAndSound()
        {
            var timers = new Timers
            {
                DelayTimer = 3,
                SoundTimer = 3
            };

            timers.Tick(skipFreshWrites: true);
            timers.DelayTimer = 9;
            timers.Tick(skipFreshWrites: true);

            Assert.Equal((byte)9, timers.DelayTimer);
            Assert.Equal((byte)2, timers.SoundTimer);
        }

        [Fact]
        public void Reset_ShouldClearTimerValuesAndFreshWriteState()
        {
            var timers = new Timers
            {
                DelayTimer = 5,
                SoundTimer = 5
            };

            timers.Reset();
            timers.Tick(skipFreshWrites: true);

            Assert.Equal((byte)0, timers.DelayTimer);
            Assert.Equal((byte)0, timers.SoundTimer);
        }
    }
}
