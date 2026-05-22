using Chip8Emu.Core.Machine;

namespace Chip8Emu.Core.Debugging.Disassembly
{
    public static class Chip8DisassemblerExtensions
    {
        public static IReadOnlyList<DisassembledInstruction> DisassembleLoadedRom(
            this IChip8Disassembler disassembler,
            IEmulatorMachine machine)
        {
            ArgumentNullException.ThrowIfNull(disassembler);
            ArgumentNullException.ThrowIfNull(machine);

            if (!machine.TryGetLoadedRom(out var romData, out var romStartAddress))
            {
                return Array.Empty<DisassembledInstruction>();
            }

            return disassembler.Disassemble(romData.Span, romStartAddress);
        }

        public static DisassemblyWindow CreateWindowForCurrentProgramCounter(
            this IChip8Disassembler disassembler,
            IEmulatorMachine machine,
            int radius = 8)
        {
            ArgumentNullException.ThrowIfNull(disassembler);
            ArgumentNullException.ThrowIfNull(machine);

            var listing = disassembler.DisassembleLoadedRom(machine);
            var snapshot = machine.CurrentSnapshot();
            return disassembler.CreateWindow(listing, snapshot.Cpu.PC, radius);
        }
    }
}
