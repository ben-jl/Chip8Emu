using Chip8Emu.Core.Debugging.Breakpoints;

namespace Chip8Emu.Test.Core
{
    public class BreakpointAddressOpcodeTests
    {
        [Fact]
        public void AddressBreakpoint_ShouldMatchProgramCounter()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddAddressBreakpoint(0x234);

            var matched = manager.TryMatch(0x234, 0x60AB, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(breakpoint.Id, match!.BreakpointId);
            Assert.Equal(BreakpointKind.Address, match.Kind);
        }

        [Fact]
        public void OpcodeBreakpoint_ShouldMatchExactOpcode()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddOpcodeBreakpoint(0xA2F0);

            var matched = manager.TryMatch(0x200, 0xA2F0, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(breakpoint.Id, match!.BreakpointId);
            Assert.Equal(BreakpointKind.Opcode, match.Kind);
        }

        [Fact]
        public void OpcodeBreakpoint_ShouldMatchMaskedPattern()
        {
            var manager = new BreakpointManager();
            manager.AddOpcodeBreakpoint(opcodeValue: 0x6000, opcodeMask: 0xF000);

            var matched = manager.TryMatch(0x200, 0x67FF, out var match);

            Assert.True(matched);
            Assert.NotNull(match);
            Assert.Equal(BreakpointKind.Opcode, match!.Kind);
        }

        [Fact]
        public void DisabledBreakpoint_ShouldNotMatch()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddAddressBreakpoint(0x200);

            var toggled = manager.SetEnabled(breakpoint.Id, enabled: false);
            var matched = manager.TryMatch(0x200, 0x6001, out var match);

            Assert.True(toggled);
            Assert.False(matched);
            Assert.Null(match);
        }

        [Fact]
        public void Remove_ShouldDeleteBreakpoint()
        {
            var manager = new BreakpointManager();
            var breakpoint = manager.AddOpcodeBreakpoint(0xA2F0);

            var removed = manager.Remove(breakpoint.Id);
            var matched = manager.TryMatch(0x200, 0xA2F0, out var match);

            Assert.True(removed);
            Assert.False(matched);
            Assert.Null(match);
        }
    }
}
