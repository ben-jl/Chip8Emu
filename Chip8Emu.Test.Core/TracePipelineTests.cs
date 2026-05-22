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

            var fetchedIndex = IndexOf<InstructionFetchedTraceEvent>(snapshot);
            var decodedIndex = IndexOf<InstructionDecodedTraceEvent>(snapshot);
            var executedIndex = IndexOf<InstructionExecutedTraceEvent>(snapshot);
            Assert.True(fetchedIndex >= 0 && decodedIndex > fetchedIndex && executedIndex > decodedIndex);
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

        [Fact]
        public void StepInstruction_ShouldCaptureInstructionCadenceTimerTick_WhenCadenceThresholdReached()
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

            machine.StepInstruction();

            var snapshot = buffer.Snapshot();
            Assert.Contains(snapshot, e => e is TimerTickedTraceEvent tick && tick.Cadence == "Instruction");
        }

        [Fact]
        public void SetKeyState_ShouldCaptureKeyStateChangedEvent()
        {
            var buffer = new InMemoryTraceBuffer();
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 }, buffer);
            buffer.Clear();

            machine.SetKeyState(0xA, true);

            var snapshot = buffer.Snapshot();
            var keyEvent = Assert.Single(snapshot.OfType<KeyStateChangedTraceEvent>());
            Assert.Equal((byte)0xA, keyEvent.Key);
            Assert.True(keyEvent.IsPressed);
        }

        [Fact]
        public void UnknownOpcode_ShouldCaptureInstructionFaultTraceEvent()
        {
            var buffer = new InMemoryTraceBuffer();
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 }, buffer);
            machine.LoadRom([
                0xF0, 0xF0
            ]);
            buffer.Clear();

            _ = Assert.Throws<InvalidOperationException>(() => machine.StepInstruction());

            var snapshot = buffer.Snapshot();
            var faultEvent = Assert.Single(snapshot.OfType<InstructionFaultedTraceEvent>());
            Assert.Equal((ushort)0x200, faultEvent.ProgramCounter);
            Assert.Equal((ushort)0xF0F0, faultEvent.Opcode);
            Assert.Contains("Unknown opcode", faultEvent.ErrorMessage);
        }

        [Fact]
        public void PublishedEvents_ShouldHaveMonotonicSequenceNumbers()
        {
            var buffer = new InMemoryTraceBuffer();
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            }, buffer);

            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0x61, 0x02  // LD V1, 0x02
            ]);
            buffer.Clear();

            machine.StepFrame();
            machine.StepInstruction();
            machine.SetKeyState(0x1, true);

            var snapshot = buffer.Snapshot();
            Assert.NotEmpty(snapshot);
            for (var i = 1; i < snapshot.Count; i++)
            {
                Assert.True(snapshot[i - 1].Sequence < snapshot[i].Sequence);
            }
        }

        private static int IndexOf<TEvent>(IReadOnlyList<TraceEvent> events) where TEvent : TraceEvent
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] is TEvent)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
