using Chip8Emu.Core.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Test.Core
{
    public class Chip8MachineDrawInstructionTests
    {
        [Fact]
        public void Chip8Machine_DrawZeroSprite_ShouldSetPixeslOn()
        {
            var machine = new Chip8Machine();

            machine.LoadRom([
                0xD0, 0x05 // DRW V0, V0, 5 ; Should be the zero sprite
                ]);

            machine.StepInstruction();

            var expected = new byte[40]
            {
                1, 1, 1, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 1, 0, 0, 0, 0
            };

            var buffer = machine.Display.Buffer;
            for (int i = 0; i < expected.Length; i++)
            {
                var bufferIdx = ((i / 8)*machine.Display.Width) + (i % 8);
                Assert.Equal(expected[i], buffer[bufferIdx]);
            }
        }

        [Fact]
        public void Chip8Machine_DrawZeroSprit_AfterSetV0To1_ShouldDrawAt11()
        {
            var machine = new Chip8Machine();

            machine.LoadRom([
                0x60, 0x01, // LD V0, 1
                0xD0, 0x05 // DRW V0, V0, 5 ; Should be the zero sprite
                ]);

            machine.StepInstruction(); // LD V0, 1
            machine.StepInstruction(); // DRW V0, V0, 5

            var buffer = machine.Display.Buffer;
            var expected = new byte[40]
            {
                1, 1, 1, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 0, 0, 0, 0,
                1, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 1, 0, 0, 0, 0
            };

            for (int i = 0; i < expected.Length; i++)
            {
                var expR = (i / 8) + 1;
                var expC = 1 + (i % 8);

                var bufferIdx = (expR * machine.Display.Width) + expC;
                Assert.Equal(expected[i], buffer[bufferIdx]);
            }
        }
    }
}
