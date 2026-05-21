namespace Chip8Emu.Core.Cpu
{
    public enum OperandPattern
    {
        None,
        Address,
        RegisterByte,
        RegisterRegister,
        RegisterNibble,
        Register,
        V0Address,
        IAddress,
        RegisterDelayTimer,
        RegisterKey,
        DelayTimerRegister,
        SoundTimerRegister,
        IRegister,
        FontRegister,
        BcdRegister,
        IndirectIRegister,
        RegisterIndirectI
    }
}
