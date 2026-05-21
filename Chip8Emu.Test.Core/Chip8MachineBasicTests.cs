using Chip8Emu.Core.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Test.Core
{
    public class Chip8MachineBasicTests
    {
        [Fact]
        public void Chip8Machine_ShouldInitializeWithDefaultState()
        {
            var machine = new Chip8Machine();
            var snapshot = machine.CurrentSnapshot();
            Assert.Equal(0x200, snapshot.Cpu.PC); // Program counter should start at 0x200
            Assert.Equal(0, snapshot.Cpu.I); // Index register should start at 0
            Assert.All(snapshot.Cpu.V, v => Assert.Equal(0, v)); // All general-purpose registers should start at 0
        }

        [Fact]
        public void Chip8Machine_LDREG_ShouldSetRegister()
        {
            var machine = new Chip8Machine();
            machine.LoadRom(new byte[] { 0x60, 0xAB }); // LD V0, 0xAB
            machine.StepInstruction();
            var snapshot = machine.CurrentSnapshot();
            Assert.Equal(0xAB, snapshot.Cpu.V[0]); // V0 should be set to 0xAB
        }

        [Fact]
        public void Chip8Machine_LDREG_ADDREG_ShouldSetRegisterAndAddValue()
        {
            var machine = new Chip8Machine();
            machine.LoadRom([
                0x61, 0x05, // LD V1, 0x05
                0x71, 0x02, // ADD V1, 0x02
                ]);

            machine.StepInstruction(); // Execute LD V1, 0x05
            machine.StepInstruction(); // Execute ADD V1, 0x02

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal(0x07, snapshot.Cpu.V[1]); // V1 should be 0x05 + 0x02 = 0x07
        }

        [Fact]
        public void Chip8Machine_STDT_ThenLDDT_ShouldUseSharedTimers()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x05, // LD V0, 0x05
                0xF0, 0x15, // LD DT, V0
                0xF0, 0x07  // LD V0, DT
            ]);

            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x05, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x05, snapshot.DelayTimer);
        }

        [Fact]
        public void Chip8Machine_StepInstruction_ShouldTickTimers_OnInstructionCadence()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x03, // LD V0, 0x03
                0xF0, 0x15, // LD DT, V0
                0x60, 0x00, // LD V0, 0x00
                0xF0, 0x07  // LD V0, DT
            ]);

            machine.StepInstruction(); // DT stays 0
            machine.StepInstruction(); // DT set to 3 then ticked to 2
            machine.StepInstruction(); // DT ticked to 1
            machine.StepInstruction(); // V0 loaded with 1, then DT ticked to 0

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x01, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x00, snapshot.DelayTimer);
        }

        [Fact]
        public void Chip8Machine_StepFrame_ShouldTickTimersOncePerFrame()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x05, // LD V0, 0x05
                0xF0, 0x15  // LD DT, V0
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x04, snapshot.DelayTimer);
        }

        [Fact]
        public void Chip8Machine_StepFrame_ShouldTickTimersOnce_WhenDisplayWaitExitsEarly()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                DisplayWaitOnDraw = true,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x02, // LD V0, 0x02
                0xF0, 0x15, // LD DT, V0
                0xD0, 0x11, // DRW V0, V1, 1
                0x60, 0x09, // LD V0, 0x09
                0xD0, 0x11, // DRW V0, V1, 1
                0xF0, 0x07  // LD V0, DT (should execute next frame)
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x01, snapshot.DelayTimer);
            Assert.Equal((byte)0x09, snapshot.Cpu.V[0]);
            Assert.Equal((ushort)0x208, snapshot.Cpu.PC);
        }
    }
}
