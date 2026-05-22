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
        private readonly int _displayX;
        private readonly int _displayWidth;
        private readonly SdlBitmapTextRenderer _text;

        public SdlDebugOverlayRenderer(
            nint renderer,
            int windowWidth,
            int windowHeight,
            int displayX,
            int displayWidth)
        {
            _renderer = renderer;
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            _displayX = displayX;
            _displayWidth = displayWidth;
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

            var leftWidth = _displayX;
            var rightX = _displayX + _displayWidth;
            var rightWidth = _windowWidth - rightX;
            var halfHeight = _windowHeight / 2f;

            DrawPanel(0, 0, leftWidth, halfHeight, 12, 18, 26, "CPU", BuildCpuLines(controller));
            DrawPanel(0, halfHeight, leftWidth, _windowHeight - halfHeight, 18, 16, 30, "DEBUG MODE", BuildDebugModeLines(viewState, controller, machine, disassembler));
            DrawPanel(rightX, 0, rightWidth, halfHeight, 20, 18, 20, "BREAKPOINTS", BuildBreakpointLines(viewState, controller));
            DrawPanel(rightX, halfHeight, rightWidth, _windowHeight - halfHeight, 16, 20, 20, "TRACE", BuildTraceLines(viewState, traceBuffer));
        }

        private void DrawPanel(
            float x,
            float y,
            float width,
            float height,
            byte r,
            byte g,
            byte b,
            string title,
            IReadOnlyList<string> lines)
        {
            var panel = new SDL.FRect
            {
                X = x,
                Y = y,
                W = width,
                H = height
            };
            SDL.SetRenderDrawColor(_renderer, r, g, b, 255);
            SDL.RenderFillRect(_renderer, ref panel);

            var border = new SDL.FRect
            {
                X = x + 1,
                Y = y + 1,
                W = width - 2,
                H = height - 2
            };
            SDL.SetRenderDrawColor(_renderer, 42, 46, 58, 255);
            SDL.RenderRect(_renderer, ref border);

            _text.DrawText(title, x + 8, y + 8, 3, 240, 240, 120);

            var lineY = y + 28;
            foreach (var line in lines.Take(17))
            {
                _text.DrawText(line, x + 8, lineY, 2, 225, 225, 225);
                lineY += 12;
            }
        }

        private static IReadOnlyList<string> BuildCpuLines(IEmulatorDebugController controller)
        {
            var snapshot = controller.MachineSnapshot;
            var cpu = snapshot.Cpu;
            var lines = new List<string>
            {
                $"PC=0X{cpu.PC:X3} I=0X{cpu.I:X3}",
                $"DT={snapshot.DelayTimer} ST={snapshot.SoundTimer}",
                $"RAND={cpu.RandomSeed}",
                $"RCOUNT={cpu.RandomCount}"
            };

            for (var i = 0; i < 16; i += 4)
            {
                lines.Add($"V{i:X}=0X{cpu.V[i]:X2} V{i + 1:X}=0X{cpu.V[i + 1]:X2}");
                lines.Add($"V{i + 2:X}=0X{cpu.V[i + 2]:X2} V{i + 3:X}=0X{cpu.V[i + 3]:X2}");
            }

            return lines;
        }

        private static IReadOnlyList<string> BuildDebugModeLines(
            SdlDebugViewState viewState,
            IEmulatorDebugController controller,
            IEmulatorMachine machine,
            IChip8Disassembler disassembler)
        {
            var lines = new List<string>
            {
                $"STATE {controller.State.Mode}",
                $"STOP {controller.State.StopReason}",
                $"ACTIVE {viewState.ActivePanel}",
                $"LAST {viewState.LastAction}",
                "F1 TOGGLE  F5 RUN/PAUSE",
                "F10 STEP   TAB NEXT",
                "BACKSPACE PREV PANEL",
                "UP/DOWN MOVE",
                "B ADDR BP  O OPCODE BP",
                "DEL REMOVE BP"
            };

            var listing = disassembler.DisassembleLoadedRom(machine);
            var window = disassembler.CreateWindow(listing, controller.MachineSnapshot.Cpu.PC, radius: 1);
            if (window.Rows.Count > 0)
            {
                lines.Add("NOW:");
                var current = window.Rows[window.SelectedIndex];
                lines.Add($"0X{current.Address:X3} {current.Mnemonic} {current.OperandsText}".TrimEnd());
            }

            return lines;
        }

        private static IReadOnlyList<string> BuildBreakpointLines(
            SdlDebugViewState viewState,
            IEmulatorDebugController controller)
        {
            var lines = new List<string>();
            var breakpoints = controller.Breakpoints.All;
            if (breakpoints.Count == 0)
            {
                lines.Add("NONE");
                lines.Add("B TOGGLE ADDR");
                lines.Add("O TOGGLE OPCODE");
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

            return lines;
        }

        private static string DescribeBreakpoint(BreakpointDefinition breakpoint)
        {
            return breakpoint.Kind switch
            {
                BreakpointKind.Address => $"ADDR 0X{breakpoint.Address ?? 0:X3}",
                BreakpointKind.Opcode => $"OP 0X{breakpoint.OpcodeValue ?? 0:X4}",
                BreakpointKind.Memory when breakpoint.Memory is not null => $"MEM {breakpoint.Memory.StartAddress:X3}-{breakpoint.Memory.EndAddress:X3}",
                BreakpointKind.Register when breakpoint.Register is not null => $"REG {breakpoint.Register.RegisterName} {breakpoint.Register.Comparison}",
                BreakpointKind.Conditional when breakpoint.Conditional is not null => $"COND {breakpoint.Conditional.Expression}",
                _ => breakpoint.Kind.ToString()
            };
        }

        private static IReadOnlyList<string> BuildTraceLines(SdlDebugViewState viewState, ITraceBuffer traceBuffer)
        {
            var all = traceBuffer.Snapshot(120);
            if (all.Count == 0)
            {
                return ["TRACE EMPTY"];
            }

            var start = Math.Max(0, all.Count - 18 - viewState.TraceOffset);
            var window = all.Skip(start).Take(18).ToArray();
            return window.Select(FormatTraceLine).ToArray();
        }

        private static string FormatTraceLine(TraceEvent e)
        {
            return e switch
            {
                InstructionFetchedTraceEvent fetched => $"#{e.Sequence} F 0X{fetched.ProgramCounter:X3} 0X{fetched.Opcode:X4}",
                InstructionDecodedTraceEvent decoded => $"#{e.Sequence} D {decoded.Mnemonic}",
                InstructionExecutedTraceEvent executed => $"#{e.Sequence} X 0X{executed.ProgramCounterBefore:X3}->0X{executed.ProgramCounterAfter:X3}",
                InstructionFaultedTraceEvent faulted => $"#{e.Sequence} ! {faulted.ErrorMessage}",
                MemoryReadTraceEvent mr => $"#{e.Sequence} MR 0X{mr.Address:X3}=0X{mr.Value:X2}",
                MemoryWriteTraceEvent mw => $"#{e.Sequence} MW 0X{mw.Address:X3}=0X{mw.Value:X2}",
                FrameStartedTraceEvent fs => $"#{e.Sequence} FS B={fs.InstructionBudget}",
                FrameCompletedTraceEvent fc => $"#{e.Sequence} FE N={fc.InstructionsExecuted}",
                TimerTickedTraceEvent tt => $"#{e.Sequence} T {tt.Cadence} D{tt.DelayTimer} S{tt.SoundTimer}",
                KeyStateChangedTraceEvent ks => $"#{e.Sequence} K {ks.Key:X} {(ks.IsPressed ? "D" : "U")}",
                _ => $"#{e.Sequence} {e.Kind}"
            };
        }
    }
}
