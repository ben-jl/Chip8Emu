using Chip8Emu.Core.Cpu;

namespace Chip8Emu.Core.Debugging.Trace
{
    public sealed record InstructionFetchedTraceEvent(
        ushort ProgramCounter,
        ushort Opcode) : TraceEvent(TraceEventKind.InstructionFetched);

    public sealed record InstructionDecodedTraceEvent(
        ushort ProgramCounter,
        ushort Opcode,
        string Mnemonic,
        DecodedOperands Operands) : TraceEvent(TraceEventKind.InstructionDecoded);

    public sealed record InstructionExecutedTraceEvent(
        ushort ProgramCounterBefore,
        ushort ProgramCounterAfter,
        ushort Opcode,
        bool DrewSprite,
        bool WaitingForDrawVBlank) : TraceEvent(TraceEventKind.InstructionExecuted);

    public sealed record InstructionFaultedTraceEvent(
        ushort ProgramCounter,
        ushort Opcode,
        string ErrorMessage) : TraceEvent(TraceEventKind.InstructionFaulted);
}
