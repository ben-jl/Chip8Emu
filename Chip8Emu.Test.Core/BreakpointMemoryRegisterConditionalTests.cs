using Chip8Emu.Core.Debugging.Breakpoints;
using Chip8Emu.Core.Debugging;
using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class BreakpointMemoryRegisterConditionalTests
    {
        [Fact]
        public void MemoryBreakpoint_ShouldMatchWriteAccess()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddMemoryBreakpoint(
                startAddress: 0x300,
                endAddress: 0x300,
                breakOnRead: false,
                breakOnWrite: true);

            var context = CreateContext(
                currentPc: 0x202,
                opcode: 0xF155,
                currentV0: 1,
                previousV0: 1,
                accesses: [
                    new MemoryAccessSnapshot(1, MemoryAccessKind.Write, 0x300, 0xAA)
                ]);

            var matched = manager.TryMatch(context, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(breakpoint.Id, match!.BreakpointId);
            Assert.Equal(BreakpointKind.Memory, match.Kind);
        }

        [Fact]
        public void RegisterBreakpoint_Equals_ShouldMatchCurrentValue()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddRegisterEqualsBreakpoint("V1", 0x42);
            var context = CreateContext(
                currentPc: 0x204,
                opcode: 0x6142,
                currentV0: 0,
                previousV0: 0,
                currentV1: 0x42,
                previousV1: 0x00);

            var matched = manager.TryMatch(context, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(breakpoint.Id, match!.BreakpointId);
            Assert.Equal(BreakpointKind.Register, match.Kind);
        }

        [Fact]
        public void RegisterBreakpoint_Changed_ShouldMatchWhenValueChanges()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddRegisterChangedBreakpoint("V0");
            var context = CreateContext(
                currentPc: 0x204,
                opcode: 0x7001,
                currentV0: 0x03,
                previousV0: 0x02);

            var matched = manager.TryMatch(context, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(breakpoint.Id, match!.BreakpointId);
            Assert.Equal(BreakpointKind.Register, match.Kind);
        }

        [Fact]
        public void ConditionalBreakpoint_ShouldMatchComplexExpression()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddConditionalBreakpoint("PC == 0x204 && V0 == 0x03");
            var context = CreateContext(
                currentPc: 0x204,
                opcode: 0x6003,
                currentV0: 0x03,
                previousV0: 0x02);

            var matched = manager.TryMatch(context, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(breakpoint.Id, match!.BreakpointId);
            Assert.Equal(BreakpointKind.Conditional, match.Kind);
        }

        [Fact]
        public void ConditionalBreakpoint_InvalidExpression_ShouldThrow()
        {
            var manager = new BreakpointManager();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                manager.AddConditionalBreakpoint("PC = 0x200"));

            Assert.Contains("Unexpected token", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DebugController_Update_ShouldPauseOnRegisterChangedBreakpoint_BeforeNextInstruction()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                RandomSeed = 1234
            });
            machine.LoadRom([
                0x60, 0x01, // LD V0, 1
                0x61, 0x02, // LD V1, 2
                0x62, 0x03  // LD V2, 3
            ]);
            var controller = new EmulatorDebugController(machine);
            _ = controller.Breakpoints.AddRegisterChangedBreakpoint("V0");

            controller.Update();

            var snapshot = controller.MachineSnapshot;
            var state = controller.State;
            Assert.Equal(DebugExecutionMode.Paused, state.Mode);
            Assert.Equal(DebugStopReason.BreakpointHit, state.StopReason);
            Assert.Equal((ushort)0x202, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
        }

        [Fact]
        public void DebugController_Update_ShouldPauseOnMemoryWriteBreakpoint_BeforeNextInstruction()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                RandomSeed = 1234
            });
            machine.LoadRom([
                0xA3, 0x00, // LD I, 0x300
                0x60, 0xAA, // LD V0, 0xAA
                0xF0, 0x55, // LD [I], V0
                0x61, 0x01  // LD V1, 0x01
            ]);
            var controller = new EmulatorDebugController(machine);
            _ = controller.Breakpoints.AddMemoryBreakpoint(
                startAddress: 0x300,
                endAddress: 0x300,
                breakOnRead: false,
                breakOnWrite: true);

            controller.Update();

            var snapshot = controller.MachineSnapshot;
            var state = controller.State;
            Assert.Equal(DebugExecutionMode.Paused, state.Mode);
            Assert.Equal(DebugStopReason.BreakpointHit, state.StopReason);
            Assert.Equal((ushort)0x206, snapshot.Cpu.PC);
            Assert.Equal((byte)0xAA, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
        }

        [Fact]
        public void DebugController_Update_ShouldPauseOnConditionalBreakpoint_BeforeNextInstruction()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                RandomSeed = 1234
            });
            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0x61, 0x02, // LD V1, 0x02
                0x62, 0x03  // LD V2, 0x03
            ]);
            var controller = new EmulatorDebugController(machine);
            _ = controller.Breakpoints.AddConditionalBreakpoint("PC == 0x202 && V0 == 0x01");

            controller.Update();

            var snapshot = controller.MachineSnapshot;
            var state = controller.State;
            Assert.Equal(DebugExecutionMode.Paused, state.Mode);
            Assert.Equal(DebugStopReason.BreakpointHit, state.StopReason);
            Assert.Equal((ushort)0x202, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
        }

        private static BreakpointEvaluationContext CreateContext(
            ushort currentPc,
            ushort opcode,
            byte currentV0,
            byte previousV0,
            byte currentV1 = 0,
            byte previousV1 = 0,
            IReadOnlyList<MemoryAccessSnapshot>? accesses = null)
        {
            var currentRegisters = new byte[16];
            currentRegisters[0] = currentV0;
            currentRegisters[1] = currentV1;

            var previousRegisters = new byte[16];
            previousRegisters[0] = previousV0;
            previousRegisters[1] = previousV1;

            var current = new MachineSnapshot(
                new CpuSnapshot(currentPc, 0, currentRegisters, 0, 0),
                0,
                0);
            var previous = new MachineSnapshot(
                new CpuSnapshot((ushort)(currentPc - 2), 0, previousRegisters, 0, 0),
                0,
                0);

            return new BreakpointEvaluationContext(
                currentPc,
                opcode,
                current,
                previous,
                accesses ?? Array.Empty<MemoryAccessSnapshot>());
        }
    }
}
