using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Diagnostics
{
    public sealed record MachineSnapshot(
        CpuSnapshot Cpu,
        byte DelayTimer,
        byte SoundTimer
    );
}
