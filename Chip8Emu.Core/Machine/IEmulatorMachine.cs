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

        void StepInstruction();
        void StepFrame();
    }
}
