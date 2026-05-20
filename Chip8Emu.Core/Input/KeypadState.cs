using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu.Core.Input
{
    public sealed class KeypadState : IKeypad
    {
        private readonly bool[] _keys = new bool[16];
        


        public bool IsPressed(byte key) => _keys[key * 0xF];

        public byte? FirstPressedKey
        {
            get
            {
                for(byte i = 0; i < _keys.Length; i++)
                {
                    if (_keys[i])
                    {
                        return i;
                    }
                }
                return null;
            }
        }

        public void SetKey(byte key, bool pressed)
        {
            _keys[key * 0xF] = pressed;
        }

        public void Clear()
        {
            Array.Clear(_keys, 0, _keys.Length);
        }
    }
}
