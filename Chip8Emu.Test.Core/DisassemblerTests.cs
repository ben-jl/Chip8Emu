using Chip8Emu.Core.Debugging.Disassembly;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class DisassemblerTests
    {
        [Fact]
        public void Disassemble_ShouldDecodeLoadedRomIntoInstructionRows()
        {
            var disassembler = new Chip8Disassembler();

            var rows = disassembler.Disassemble([
                0x60, 0x0A, // LD V0, 0x0A
                0xA2, 0xF0, // LD I, 0x2F0
                0xD0, 0x15  // DRW V0, V1, 0x5
            ]);

            Assert.Equal(3, rows.Count);
            Assert.Equal((ushort)0x200, rows[0].Address);
            Assert.Equal("LD", rows[0].Mnemonic);
            Assert.Equal("V0, 0x0A", rows[0].OperandsText);
            Assert.True(rows[0].IsValid);

            Assert.Equal((ushort)0x202, rows[1].Address);
            Assert.Equal("LD", rows[1].Mnemonic);
            Assert.Equal("I, 0x2F0", rows[1].OperandsText);
            Assert.True(rows[1].IsValid);

            Assert.Equal((ushort)0x204, rows[2].Address);
            Assert.Equal("DRW", rows[2].Mnemonic);
            Assert.Equal("V0, V1, 0x5", rows[2].OperandsText);
            Assert.True(rows[2].IsValid);
        }

        [Fact]
        public void Disassemble_ShouldMarkUnknownOpcodesWithoutThrowing()
        {
            var disassembler = new Chip8Disassembler();

            var rows = disassembler.Disassemble([
                0xF0, 0xF0
            ]);

            var row = Assert.Single(rows);
            Assert.False(row.IsValid);
            Assert.Equal("INVALID", row.Mnemonic);
            Assert.NotNull(row.Error);
            Assert.Contains("Unknown opcode", row.Error);
        }

        [Fact]
        public void Disassemble_ShouldMarkTruncatedInstruction()
        {
            var disassembler = new Chip8Disassembler();

            var rows = disassembler.Disassemble([
                0x60
            ]);

            var row = Assert.Single(rows);
            Assert.False(row.IsValid);
            Assert.Equal((ushort)0x6000, row.Opcode);
            Assert.Equal("INVALID", row.Mnemonic);
            Assert.Equal("Truncated instruction: missing low byte.", row.Error);
        }

        [Fact]
        public void DisassemblyWindow_ShouldCenterOnCurrentProgramCounter()
        {
            var disassembler = new Chip8Disassembler();
            var listing = disassembler.Disassemble([
                0x60, 0x00,
                0x61, 0x01,
                0x62, 0x02,
                0x63, 0x03,
                0x64, 0x04
            ]);

            var window = disassembler.CreateWindow(listing, 0x204, radius: 1);

            Assert.Equal(3, window.Rows.Count);
            Assert.Equal((ushort)0x202, window.Rows[0].Address);
            Assert.Equal((ushort)0x204, window.Rows[1].Address);
            Assert.Equal((ushort)0x206, window.Rows[2].Address);
            Assert.Equal(1, window.SelectedIndex);
        }

        [Fact]
        public void DisassemblyWindow_ShouldClampNearRomBoundaries()
        {
            var disassembler = new Chip8Disassembler();
            var listing = disassembler.Disassemble([
                0x60, 0x00,
                0x61, 0x01,
                0x62, 0x02
            ]);

            var windowAtStart = disassembler.CreateWindow(listing, 0x200, radius: 2);
            Assert.Equal(3, windowAtStart.Rows.Count);
            Assert.Equal(0, windowAtStart.SelectedIndex);

            var windowPastEnd = disassembler.CreateWindow(listing, 0x240, radius: 2);
            Assert.Equal(3, windowPastEnd.Rows.Count);
            Assert.Equal(2, windowPastEnd.SelectedIndex);
        }

        [Fact]
        public void DisassembleLoadedRom_ShouldUseMachineRomDataAndAddress()
        {
            var disassembler = new Chip8Disassembler();
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0x0A,
                0x61, 0x0B
            ]);

            var listing = disassembler.DisassembleLoadedRom(machine);

            Assert.Equal(2, listing.Count);
            Assert.Equal((ushort)0x200, listing[0].Address);
            Assert.Equal((ushort)0x202, listing[1].Address);
            Assert.Equal("V0, 0x0A", listing[0].OperandsText);
            Assert.Equal("V1, 0x0B", listing[1].OperandsText);
        }

        [Fact]
        public void CreateWindowForCurrentProgramCounter_ShouldSelectCurrentInstruction()
        {
            var disassembler = new Chip8Disassembler();
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0x0A,
                0x61, 0x0B,
                0x62, 0x0C
            ]);
            machine.StepInstruction(); // PC=0x202

            var window = disassembler.CreateWindowForCurrentProgramCounter(machine, radius: 1);

            Assert.Equal((ushort)0x202, window.ProgramCounter);
            Assert.Equal((ushort)0x202, window.Rows[window.SelectedIndex].Address);
        }
    }
}
