using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Timing
{
    public sealed class Timers
    {
        private byte _delayTimer;
        private byte _soundTimer;
        private bool _delayWrittenSinceLastTick;
        private bool _soundWrittenSinceLastTick;

        public byte DelayTimer
        {
            get => _delayTimer;
            set
            {
                _delayTimer = value;
                _delayWrittenSinceLastTick = true;
            }
        }

        public byte SoundTimer
        {
            get => _soundTimer;
            set
            {
                _soundTimer = value;
                _soundWrittenSinceLastTick = true;
            }
        }

        public void Tick(bool skipFreshWrites = false)
        {
            if (_delayTimer > 0)
            {
                if (skipFreshWrites && _delayWrittenSinceLastTick)
                {
                    _delayWrittenSinceLastTick = false;
                }
                else
                {
                    _delayTimer--;
                    _delayWrittenSinceLastTick = false;
                }
            }
            else
            {
                _delayWrittenSinceLastTick = false;
            }

            if (_soundTimer > 0)
            {
                if (skipFreshWrites && _soundWrittenSinceLastTick)
                {
                    _soundWrittenSinceLastTick = false;
                }
                else
                {
                    _soundTimer--;
                    _soundWrittenSinceLastTick = false;
                }
            }
            else
            {
                _soundWrittenSinceLastTick = false;
            }
        }

        public void Reset()
        {
            _delayTimer = 0;
            _soundTimer = 0;
            _delayWrittenSinceLastTick = false;
            _soundWrittenSinceLastTick = false;
        }   
    }
}
