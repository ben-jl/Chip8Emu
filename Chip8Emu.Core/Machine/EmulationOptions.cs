using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Machine
{
    public sealed class EmulationOptions
    {
        public static EmulationOptions Default { get; } = new EmulationOptions();

        public int InstructionsPerFrame { get; init; } = 10;

        public bool ShiftUsesVy { get ; init; } = false;
        public bool LoadStoreIncrementI { get; init; } = true;
        public bool JumpWithV0 { get; init; } = false;
        public bool ClipSprites { get; init; } = true;
        public bool ResetCarryFlagOnBitwiseOps { get; init; } = true;

        public int RandomSeed { get; init; } = Environment.TickCount;
    }
}
