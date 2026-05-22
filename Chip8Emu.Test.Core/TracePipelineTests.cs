using Chip8Emu.Core.Debugging.Trace;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class TracePipelineTests
    {
        [Fact]
        public void InMemoryTraceBuffer_ShouldEvictOldestEventsAtCapacity()
        {
            var buffer = new InMemoryTraceBuffer(capacity: 2);

            buffer.Publish(new FrameStartedTraceEvent(1));
            buffer.Publish(new FrameCompletedTraceEvent(1, false, false));
            buffer.Publish(new TimerTickedTraceEvent("Frame", 0, 0));

            var snapshot = buffer.Snapshot();
            Assert.Equal(2, snapshot.Count);
            Assert.IsType<FrameCompletedTraceEvent>(snapshot[0]);
            Assert.IsType<TimerTickedTraceEvent>(snapshot[1]);
            Assert.True(snapshot[0].Sequence < snapshot[1].Sequence);
        }

        [Fact]
        public void StepInstruction_ShouldCaptureInstructionAndMemoryEvents()
        {
            var buffer = new InMemoryTraceBuffer();
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 }, buffer);

            machine.LoadRom([
                0x60, 0xAB // LD V0, 0xAB
            ]);
            buffer.Clear();

            machine.StepInstruction();

            var snapshot = buffer.Snapshot();
            Assert.Contains(snapshot, e => e is InstructionFetchedTraceEvent);
            Assert.Contains(snapshot, e => e is InstructionDecodedTraceEvent);
            Assert.Contains(snapshot, e => e is InstructionExecutedTraceEvent);
            Assert.Contains(snapshot, e => e is MemoryReadTraceEvent);
        }

        [Fact]
        public void StepFrame_ShouldCaptureFrameAndTimerEvents()
        {
            var buffer = new InMemoryTraceBuffer();
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            }, buffer);

            machine.LoadRom([
                0x60, 0x01 // LD V0, 0x01
            ]);
            buffer.Clear();

            machine.StepFrame();

            var snapshot = buffer.Snapshot();
            Assert.Contains(snapshot, e => e is FrameStartedTraceEvent);
            Assert.Contains(snapshot, e => e is FrameCompletedTraceEvent);
            Assert.Contains(snapshot, e => e is TimerTickedTraceEvent tick && tick.Cadence == "Frame");
        }
    }
}
