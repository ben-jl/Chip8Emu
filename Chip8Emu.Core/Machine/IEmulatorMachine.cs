using Chip8Emu.Core.Diagnostics;
using Chip8Emu.Core.Display;

namespace Chip8Emu.Core.Machine
{
    public interface IEmulatorMachine
    {
        IFrameBuffer Display { get; }

        bool SoundEnabled { get; }

        void LoadRom(ReadOnlySpan<byte> romData);
        void Reset();

        void SetKeyState(byte key, bool isPressed);

        /// <summary>
        /// Executes exactly one instruction. Timer cadence is instruction-dependent in this mode.
        /// </summary>
        void StepInstruction();

        /// <summary>
        /// Executes up to <see cref="EmulationOptions.InstructionsPerFrame"/> instructions for one frame.
        /// Timer cadence is frame-dependent in this mode.
        /// </summary>
        void StepFrame();

        /// <summary>
        /// Captures current state for diagnostics and debugging workflows.
        /// </summary>
        MachineSnapshot CurrentSnapshot();

        /// <summary>
        /// Returns currently loaded ROM bytes when available.
        /// </summary>
        bool TryGetLoadedRom(out ReadOnlyMemory<byte> romData, out ushort romStartAddress);

        /// <summary>
        /// Reads the opcode at the current program counter without advancing execution.
        /// </summary>
        ushort PeekOpcodeAtProgramCounter();
    }
}
