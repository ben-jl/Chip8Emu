using Chip8Emu.Core.Debugging;
using Chip8Emu.Core.Debugging.Breakpoints;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class DebugControllerTests
    {
        [Fact]
        public void DebugController_Pause_ShouldStopFrameExecution()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            });
            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0x12, 0x00  // JP 0x200
            ]);
            var controller = new EmulatorDebugController(machine);

            controller.Pause();
            controller.Update();

            var snapshot = controller.MachineSnapshot;
            Assert.Equal((ushort)0x200, snapshot.Cpu.PC);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[0]);
            Assert.Equal(DebugExecutionMode.Paused, controller.State.Mode);
            Assert.Equal(DebugStopReason.UserPause, controller.State.StopReason);
        }

        [Fact]
        public void DebugController_Resume_ShouldContinueFrameExecution()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            });
            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0x12, 0x00  // JP 0x200
            ]);
            var controller = new EmulatorDebugController(machine);

            controller.Pause();
            controller.Resume();
            controller.Update();

            var snapshot = controller.MachineSnapshot;
            Assert.Equal((ushort)0x202, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[0]);
            Assert.Equal(DebugExecutionMode.Running, controller.State.Mode);
            Assert.Equal(DebugStopReason.None, controller.State.StopReason);
        }

        [Fact]
        public void DebugController_StepInstructionOnce_ShouldAdvanceOneInstructionWhilePaused()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0xAB, // LD V0, 0xAB
                0x61, 0xCD  // LD V1, 0xCD
            ]);
            var controller = new EmulatorDebugController(machine);

            controller.Pause();
            controller.StepInstructionOnce();

            var snapshot = controller.MachineSnapshot;
            Assert.Equal((ushort)0x202, snapshot.Cpu.PC);
            Assert.Equal((byte)0xAB, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
        }

        [Fact]
        public void DebugController_StepInstructionOnce_ShouldRemainPausedAfterStep()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0xAB, // LD V0, 0xAB
                0x61, 0xCD  // LD V1, 0xCD
            ]);
            var controller = new EmulatorDebugController(machine);

            controller.Pause();
            controller.StepInstructionOnce();
            controller.StepInstructionOnce();

            var snapshot = controller.MachineSnapshot;
            var state = controller.State;

            Assert.Equal((ushort)0x204, snapshot.Cpu.PC);
            Assert.Equal((byte)0xAB, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0xCD, snapshot.Cpu.V[1]);
            Assert.Equal(DebugExecutionMode.Paused, state.Mode);
            Assert.Equal(DebugStopReason.StepComplete, state.StopReason);
            Assert.Equal(2, state.InstructionSteps);
        }

        [Fact]
        public void DebugController_Update_ShouldPauseWhenAddressBreakpointMatches()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0xAB, // LD V0, 0xAB
                0x61, 0xCD  // LD V1, 0xCD
            ]);
            var controller = new EmulatorDebugController(machine);
            var breakpoint = controller.Breakpoints.AddAddressBreakpoint(0x200);

            controller.Update();

            var state = controller.State;
            var snapshot = controller.MachineSnapshot;
            Assert.Equal(DebugExecutionMode.Paused, state.Mode);
            Assert.Equal(DebugStopReason.BreakpointHit, state.StopReason);
            Assert.Equal((ushort)0x200, snapshot.Cpu.PC);
            Assert.NotNull(controller.LastBreakpointMatch);
            Assert.Equal(breakpoint.Id, controller.LastBreakpointMatch!.BreakpointId);
            Assert.Equal(BreakpointKind.Address, controller.LastBreakpointMatch.Kind);
        }

        [Fact]
        public void DebugController_Update_ShouldPauseWhenOpcodeBreakpointMatches()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0xAB, // LD V0, 0xAB
                0x61, 0xCD  // LD V1, 0xCD
            ]);
            var controller = new EmulatorDebugController(machine);
            var breakpoint = controller.Breakpoints.AddOpcodeBreakpoint(0x6000, 0xF000);

            controller.Update();

            var state = controller.State;
            Assert.Equal(DebugExecutionMode.Paused, state.Mode);
            Assert.Equal(DebugStopReason.BreakpointHit, state.StopReason);
            Assert.NotNull(controller.LastBreakpointMatch);
            Assert.Equal(breakpoint.Id, controller.LastBreakpointMatch!.BreakpointId);
            Assert.Equal(BreakpointKind.Opcode, controller.LastBreakpointMatch.Kind);
        }

        [Fact]
        public void DebugController_Update_ShouldIgnoreDisabledBreakpoints()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            });
            machine.LoadRom([
                0x60, 0xAB, // LD V0, 0xAB
                0x61, 0xCD  // LD V1, 0xCD
            ]);
            var controller = new EmulatorDebugController(machine);
            var breakpoint = controller.Breakpoints.AddAddressBreakpoint(0x200);
            _ = controller.Breakpoints.SetEnabled(breakpoint.Id, false);

            controller.Update();

            var state = controller.State;
            var snapshot = controller.MachineSnapshot;
            Assert.Equal(DebugExecutionMode.Running, state.Mode);
            Assert.Equal(DebugStopReason.None, state.StopReason);
            Assert.Equal((ushort)0x202, snapshot.Cpu.PC);
            Assert.Null(controller.LastBreakpointMatch);
        }
    }
}
