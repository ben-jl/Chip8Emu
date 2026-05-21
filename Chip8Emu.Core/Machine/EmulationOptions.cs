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

        /// <summary>
        /// Instruction budget for each <see cref="IEmulatorMachine.StepFrame"/> call.
        /// </summary>
        public int InstructionsPerFrame { get; init; } = 10;

        public bool ShiftUsesVy { get ; init; } = false;
        public bool JumpWithV0 { get; init; } = false;
        public bool ClipSprites { get; init; } = true;
        public bool ResetCarryFlagOnBitwiseOps { get; init; } = true;
        public bool IncrementIOnStoreLoadMemoryOps { get; init; } = false;
        /// <summary>
        /// When true, frame stepping allows at most one DRW per frame and blocks when a subsequent
        /// draw would occur in the same frame to model original display-wait behavior.
        /// This toggle affects <see cref="IEmulatorMachine.StepFrame"/> only.
        /// Timer cadence remains mode-dependent: frame stepping ticks timers once per frame while
        /// instruction stepping ticks based on instruction cadence.
        /// </summary>
        public bool DisplayWaitOnDraw { get; init; } = false;

        public int RandomSeed { get; init; } = Environment.TickCount;
    }
}
