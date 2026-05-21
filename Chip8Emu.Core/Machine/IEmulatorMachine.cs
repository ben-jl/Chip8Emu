using Chip8Emu.Core.Display;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
