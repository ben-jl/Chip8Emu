using Chip8Emu.Core.Debugging;
using Chip8Emu.Core.Debugging.Breakpoints;
using Chip8Emu.Core.Debugging.Disassembly;
using Chip8Emu.Core.Debugging.Trace;
using Chip8Emu.Core.Machine;
using SDL3;

namespace Chip8Emu.SdlHost.Debug
{
    internal sealed class SdlDebugOverlayRenderer
    {
        private readonly nint _renderer;
        private readonly int _windowWidth;
        private readonly int _windowHeight;
        private readonly SdlBitmapTextRenderer _text;

        public SdlDebugOverlayRenderer(nint renderer, int windowWidth, int windowHeight)
        {
            _renderer = renderer;
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            _text = new SdlBitmapTextRenderer(renderer);
        }

        public void Render(
            SdlDebugViewState viewState,
            IEmulatorDebugController controller,
            IEmulatorMachine machine,
            IChip8Disassembler disassembler,
            ITraceBuffer traceBuffer)
        {
            if (!viewState.OverlayVisible)
            {
                return;
            }

            var background = new SDL.FRect
            {
                X = 0,
                Y = 0,
                W = _windowWidth,
                H = 220
            };
            SDL.SetRenderDrawColor(_renderer, 12, 18, 26, 230);
            SDL.RenderFillRect(_renderer, ref background);

            _text.DrawText("DEBUG MODE", 8, 8, 3, 240, 240, 120);
            _text.DrawText($"PANEL: {viewState.ActivePanel}", 8, 30, 2, 220, 220, 220);
            _text.DrawText($"STATE: {controller.State.Mode} / {controller.State.StopReason}", 8, 44, 2, 200, 210, 255);
            _text.DrawText($"ACTION: {viewState.LastAction}", 8, 58, 2, 180, 230, 180);

            var lines = BuildPanelLines(viewState, controller, machine, disassembler, traceBuffer);
            var y = 78f;
            foreach (var line in lines.Take(14))
            {
                _text.DrawText(line, 8, y, 2, 230, 230, 230);
                y += 12;
            }
        }

        private static IReadOnlyList<string> BuildPanelLines(
            SdlDebugViewState viewState,
            IEmulatorDebugController controller,
            IEmulatorMachine machine,
            IChip8Disassembler disassembler,
            ITraceBuffer traceBuffer)
        {
            return viewState.ActivePanel switch
            {
                SdlDebugPanel.Cpu => BuildCpuLines(controller),
                SdlDebugPanel.Disassembly => BuildDisassemblyLines(controller, machine, disassembler),
                SdlDebugPanel.Breakpoints => BuildBreakpointLines(viewState, controller),
                SdlDebugPanel.Trace => BuildTraceLines(viewState, traceBuffer),
                _ => ["UNKNOWN PANEL"]
            };
        }

        private static IReadOnlyList<string> BuildCpuLines(IEmulatorDebugController controller)
        {
            var snapshot = controller.MachineSnapshot;
            var cpu = snapshot.Cpu;
            var lines = new List<string>
            {
                $"PC=0X{cpu.PC:X3} I=0X{cpu.I:X3} DT={snapshot.DelayTimer} ST={snapshot.SoundTimer}",
                $"RAND={cpu.RandomSeed} COUNT={cpu.RandomCount}"
            };

            for (var i = 0; i < 16; i += 4)
            {
                lines.Add(
                    $"V{i:X}=0X{cpu.V[i]:X2} V{i + 1:X}=0X{cpu.V[i + 1]:X2} V{i + 2:X}=0X{cpu.V[i + 2]:X2} V{i + 3:X}=0X{cpu.V[i + 3]:X2}");
            }

            lines.Add("F5 PAUSE/RESUME | F10 STEP | B ADDR BP | O OPCODE BP");
            return lines;
        }

        private static IReadOnlyList<string> BuildDisassemblyLines(
            IEmulatorDebugController controller,
            IEmulatorMachine machine,
            IChip8Disassembler disassembler)
        {
            var listing = disassembler.DisassembleLoadedRom(machine);
            var window = disassembler.CreateWindow(listing, controller.MachineSnapshot.Cpu.PC, radius: 6);
            if (window.Rows.Count == 0)
            {
                return ["NO ROM LOADED"];
            }

            var lines = new List<string> { "DISASSEMBLY" };
            for (var i = 0; i < window.Rows.Count; i++)
            {
                var row = window.Rows[i];
                var marker = i == window.SelectedIndex ? ">" : " ";
                var status = row.IsValid ? " " : "!";
                lines.Add($"{marker}{status} 0X{row.Address:X3} {row.Mnemonic} {row.OperandsText}".TrimEnd());
            }

            return lines;
        }

        private static IReadOnlyList<string> BuildBreakpointLines(
            SdlDebugViewState viewState,
            IEmulatorDebugController controller)
        {
            var lines = new List<string>
            {
                "BREAKPOINTS"
            };

            var breakpoints = controller.Breakpoints.All;
            if (breakpoints.Count == 0)
            {
                lines.Add("NONE");
                lines.Add("B: TOGGLE ADDR | O: TOGGLE OPCODE");
                return lines;
            }

            var selected = Math.Clamp(viewState.BreakpointSelection, 0, breakpoints.Count - 1);
            for (var i = 0; i < breakpoints.Count; i++)
            {
                var bp = breakpoints[i];
                var marker = i == selected ? ">" : " ";
                var enabled = bp.Enabled ? "ON " : "OFF";
                lines.Add($"{marker}{enabled} {DescribeBreakpoint(bp)}");
            }

            lines.Add("UP/DOWN SELECT | DELETE REMOVE");
            return lines;
        }

        private static string DescribeBreakpoint(BreakpointDefinition breakpoint)
        {
            return breakpoint.Kind switch
            {
                BreakpointKind.Address => $"ADDR 0X{breakpoint.Address ?? 0:X3}",
                BreakpointKind.Opcode => $"OP (MASK 0X{breakpoint.OpcodeMask ?? 0:X4}) == 0X{breakpoint.OpcodeValue ?? 0:X4}",
                BreakpointKind.Memory when breakpoint.Memory is not null
                    => $"MEM {breakpoint.Memory.StartAddress:X3}-{breakpoint.Memory.EndAddress:X3}",
                BreakpointKind.Register when breakpoint.Register is not null
                    => $"REG {breakpoint.Register.RegisterName} {breakpoint.Register.Comparison}",
                BreakpointKind.Conditional when breakpoint.Conditional is not null
                    => $"COND {breakpoint.Conditional.Expression}",
                _ => breakpoint.Kind.ToString()
            };
        }

        private static IReadOnlyList<string> BuildTraceLines(SdlDebugViewState viewState, ITraceBuffer traceBuffer)
        {
            var all = traceBuffer.Snapshot(80);
            if (all.Count == 0)
            {
                return ["TRACE EMPTY"];
            }

            var start = Math.Max(0, all.Count - 12 - viewState.TraceOffset);
            var window = all.Skip(start).Take(12).ToArray();
            var lines = new List<string> { "TRACE (LATEST)" };
            lines.AddRange(window.Select(FormatTraceLine));
            lines.Add("UP/DOWN SCROLL");
            return lines;
        }

        private static string FormatTraceLine(TraceEvent e)
        {
            return e switch
            {
                InstructionFetchedTraceEvent fetched => $"#{e.Sequence} FETCH PC=0X{fetched.ProgramCounter:X3} OP=0X{fetched.Opcode:X4}",
                InstructionDecodedTraceEvent decoded => $"#{e.Sequence} DEC {decoded.Mnemonic}",
                InstructionExecutedTraceEvent executed => $"#{e.Sequence} EXEC PC 0X{executed.ProgramCounterBefore:X3}->0X{executed.ProgramCounterAfter:X3}",
                InstructionFaultedTraceEvent faulted => $"#{e.Sequence} FAULT {faulted.ErrorMessage}",
                MemoryReadTraceEvent mr => $"#{e.Sequence} MR 0X{mr.Address:X3}=0X{mr.Value:X2}",
                MemoryWriteTraceEvent mw => $"#{e.Sequence} MW 0X{mw.Address:X3}=0X{mw.Value:X2}",
                FrameStartedTraceEvent fs => $"#{e.Sequence} FRAME START BUDGET={fs.InstructionBudget}",
                FrameCompletedTraceEvent fc => $"#{e.Sequence} FRAME END EXEC={fc.InstructionsExecuted}",
                TimerTickedTraceEvent tt => $"#{e.Sequence} TIMER {tt.Cadence} DT={tt.DelayTimer} ST={tt.SoundTimer}",
                KeyStateChangedTraceEvent ks => $"#{e.Sequence} KEY {ks.Key:X} {(ks.IsPressed ? "DOWN" : "UP")}",
                _ => $"#{e.Sequence} {e.Kind}"
            };
        }
    }
}
