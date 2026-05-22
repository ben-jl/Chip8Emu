namespace Chip8Emu.Core.Debugging.Breakpoints
{
    public enum BreakpointKind
    {
        Address = 0,
        Opcode = 1,
        Memory = 2,
        Register = 3,
        Conditional = 4
    }
}
