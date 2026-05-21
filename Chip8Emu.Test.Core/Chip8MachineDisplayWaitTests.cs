using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class Chip8MachineDisplayWaitTests
    {
        [Fact]
        public void Chip8Machine_StepFrame_ShouldBlockBeforeSecondDraw_WhenDisplayWaitOnDrawEnabled()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 6,
                DisplayWaitOnDraw = true,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x00, // LD V0, 0x00
                0x61, 0x00, // LD V1, 0x00
                0xD0, 0x11, // DRW V0, V1, 1
                0x62, 0x0A, // LD V2, 0x0A
                0xD0, 0x11, // DRW V0, V1, 1
                0x63, 0x0B  // LD V3, 0x0B
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x208, snapshot.Cpu.PC);
            Assert.Equal((byte)0x0A, snapshot.Cpu.V[2]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[3]);
        }

        [Fact]
        public void Chip8Machine_StepFrame_ShouldExecuteMultipleDraws_WhenDisplayWaitOnDrawDisabled()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 6,
                DisplayWaitOnDraw = false,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x00, // LD V0, 0x00
                0x61, 0x00, // LD V1, 0x00
                0xD0, 0x11, // DRW V0, V1, 1
                0x62, 0x0A, // LD V2, 0x0A
                0xD0, 0x11, // DRW V0, V1, 1
                0x63, 0x0B  // LD V3, 0x0B
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x20C, snapshot.Cpu.PC);
            Assert.Equal((byte)0x0A, snapshot.Cpu.V[2]);
            Assert.Equal((byte)0x0B, snapshot.Cpu.V[3]);
        }
    }
}
