using Chip8Emu.Core.Machine;

namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public sealed record MemoryBreakpointDefinition(
        ushort StartAddress,
        ushort EndAddress,
        bool BreakOnRead,
        bool BreakOnWrite)
    {
        public bool Matches(MemoryAccessSnapshot access)
        {
            if (access.Address < StartAddress || access.Address > EndAddress)
            {
                return false;
            }

            return access.Kind switch
            {
                MemoryAccessKind.Read => BreakOnRead,
                MemoryAccessKind.Write => BreakOnWrite,
                _ => false
            };
        }
    }
}
