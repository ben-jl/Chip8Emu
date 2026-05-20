using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Timing
{
    public sealed class Timers
    {
        public byte DelayTimer { get; set; }
        public byte SoundTimer { get; set; }

        public void Tick()
        {
            if (DelayTimer > 0)
            {
                DelayTimer--;
            }
            if (SoundTimer > 0)
            {
                SoundTimer--;
            }
        }

        public void Reset()
        {
            DelayTimer = 0;
            SoundTimer = 0;
        }   
    }
}
