namespace Chip8Emu.Core.Cpu
{
    public abstract record InstructionDecodeResult
    {
        private InstructionDecodeResult()
        {
        }

        public sealed record Valid(DecodedInstruction Instruction) : InstructionDecodeResult;

        public sealed record Invalid(ushort Opcode, string Reason) : InstructionDecodeResult;
    }
}
