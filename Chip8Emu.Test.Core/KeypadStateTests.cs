using Chip8Emu.Core.Input;

namespace Chip8Emu.Test.Core
{
    public class KeypadStateTests
    {
        [Theory]
        [InlineData(0x00, 0x10)]
        [InlineData(0x0F, 0x1F)]
        [InlineData(0x05, 0xF5)]
        public void SetKeyAndIsPressed_ShouldMaskKeysToLowNibble(byte canonicalKey, byte aliasKey)
        {
            var keypad = new KeypadState();

            keypad.SetKey(aliasKey, true);

            Assert.True(keypad.IsPressed(canonicalKey));
            Assert.True(keypad.IsPressed(aliasKey));
        }

        [Fact]
        public void FirstPressedKey_ShouldReturnLowestPressedKey()
        {
            var keypad = new KeypadState();

            keypad.SetKey(0xC, true);
            keypad.SetKey(0x3, true);
            keypad.SetKey(0x7, true);

            Assert.Equal((byte)0x3, keypad.FirstPressedKey);
        }

        [Fact]
        public void FirstPressedKey_ShouldReturnNull_WhenNoKeysArePressed()
        {
            var keypad = new KeypadState();

            Assert.Null(keypad.FirstPressedKey);
        }

        [Fact]
        public void Clear_ShouldReleaseEveryKey()
        {
            var keypad = new KeypadState();
            for (byte key = 0; key <= 0xF; key++)
            {
                keypad.SetKey(key, true);
            }

            keypad.Clear();

            for (byte key = 0; key <= 0xF; key++)
            {
                Assert.False(keypad.IsPressed(key));
            }

            Assert.Null(keypad.FirstPressedKey);
        }

        [Fact]
        public void SetKey_ShouldReleaseCanonicalKey_WhenAliasKeyIsReleased()
        {
            var keypad = new KeypadState();

            keypad.SetKey(0x0F, true);
            keypad.SetKey(0x1F, false);

            Assert.False(keypad.IsPressed(0x0F));
            Assert.False(keypad.IsPressed(0x1F));
            Assert.Null(keypad.FirstPressedKey);
        }
    }
}
