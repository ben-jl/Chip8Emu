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
    }
}
