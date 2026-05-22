using Chip8Emu.Core.Debugging;
using Chip8Emu.Core.Debugging.Breakpoints;
using Chip8Emu.Core.Machine;
using SDL3;

namespace Chip8Emu.SdlHost.Debug
{
    internal static class SdlDebugInputHandler
    {
        public static bool HandleEvent(
            SDL.Event e,
            SdlDebugViewState viewState,
            IEmulatorMachine machine,
            IEmulatorDebugController controller)
        {
            var eventType = (SDL.EventType)e.Type;
            if (eventType != SDL.EventType.KeyDown)
            {
                return false;
            }

            if (!SdlDebugKeyBindings.TryMap(e.Key.Key, out var command))
            {
                return false;
            }

            if (!viewState.OverlayVisible && command != SdlDebugCommand.ToggleOverlay)
            {
                return false;
            }

            ExecuteCommand(command, viewState, machine, controller);
            return true;
        }

        private static void ExecuteCommand(
            SdlDebugCommand command,
            SdlDebugViewState viewState,
            IEmulatorMachine machine,
            IEmulatorDebugController controller)
        {
            switch (command)
            {
                case SdlDebugCommand.ToggleOverlay:
                    viewState.OverlayVisible = !viewState.OverlayVisible;
                    viewState.LastAction = viewState.OverlayVisible ? "DEBUG MODE ON" : "DEBUG MODE OFF";
                    break;
                case SdlDebugCommand.PauseResume:
                    if (controller.State.Mode == DebugExecutionMode.Running)
                    {
                        controller.Pause();
                        viewState.LastAction = "PAUSED";
                    }
                    else
                    {
                        controller.Resume();
                        viewState.LastAction = "RUNNING";
                    }
                    break;
                case SdlDebugCommand.StepInstruction:
                    if (controller.State.Mode != DebugExecutionMode.Paused)
                    {
                        controller.Pause();
                    }

                    controller.StepInstructionOnce();
                    viewState.LastAction = "STEP INSTRUCTION";
                    break;
                case SdlDebugCommand.NextPanel:
                    viewState.ActivePanel = (SdlDebugPanel)(((int)viewState.ActivePanel + 1) % 4);
                    viewState.LastAction = $"PANEL {viewState.ActivePanel}";
                    break;
                case SdlDebugCommand.PreviousPanel:
                    viewState.ActivePanel = (SdlDebugPanel)(((int)viewState.ActivePanel + 3) % 4);
                    viewState.LastAction = $"PANEL {viewState.ActivePanel}";
                    break;
                case SdlDebugCommand.MoveUp:
                    HandleMove(viewState, up: true, controller);
                    break;
                case SdlDebugCommand.MoveDown:
                    HandleMove(viewState, up: false, controller);
                    break;
                case SdlDebugCommand.ToggleAddressBreakpoint:
                    ToggleAddressBreakpoint(machine, controller, viewState);
                    break;
                case SdlDebugCommand.ToggleOpcodeBreakpoint:
                    ToggleOpcodeBreakpoint(machine, controller, viewState);
                    break;
                case SdlDebugCommand.RemoveSelectedBreakpoint:
                    RemoveSelectedBreakpoint(controller, viewState);
                    break;
                case SdlDebugCommand.None:
                default:
                    break;
            }
        }

        private static void HandleMove(SdlDebugViewState viewState, bool up, IEmulatorDebugController controller)
        {
            if (viewState.ActivePanel == SdlDebugPanel.Breakpoints)
            {
                var count = controller.Breakpoints.All.Count;
                if (count == 0)
                {
                    return;
                }

                viewState.BreakpointSelection = up
                    ? Math.Max(0, viewState.BreakpointSelection - 1)
                    : Math.Min(count - 1, viewState.BreakpointSelection + 1);
                viewState.LastAction = $"BP INDEX {viewState.BreakpointSelection}";
                return;
            }

            if (viewState.ActivePanel == SdlDebugPanel.Trace)
            {
                viewState.TraceOffset = up
                    ? viewState.TraceOffset + 1
                    : Math.Max(0, viewState.TraceOffset - 1);
                viewState.LastAction = $"TRACE OFFSET {viewState.TraceOffset}";
            }
        }

        private static void ToggleAddressBreakpoint(
            IEmulatorMachine machine,
            IEmulatorDebugController controller,
            SdlDebugViewState viewState)
        {
            var pc = machine.CurrentSnapshot().Cpu.PC;
            var existing = controller.Breakpoints.All.FirstOrDefault(
                bp => bp.Kind == BreakpointKind.Address && bp.Address == pc);
            if (existing is not null)
            {
                _ = controller.Breakpoints.Remove(existing.Id);
                viewState.LastAction = $"REMOVED ADDR BP 0X{pc:X3}";
                return;
            }

            _ = controller.Breakpoints.AddAddressBreakpoint(pc);
            viewState.LastAction = $"ADDED ADDR BP 0X{pc:X3}";
        }

        private static void ToggleOpcodeBreakpoint(
            IEmulatorMachine machine,
            IEmulatorDebugController controller,
            SdlDebugViewState viewState)
        {
            var opcode = machine.PeekOpcodeAtProgramCounter();
            var existing = controller.Breakpoints.All.FirstOrDefault(
                bp => bp.Kind == BreakpointKind.Opcode &&
                      bp.OpcodeMask == 0xFFFF &&
                      bp.OpcodeValue == opcode);
            if (existing is not null)
            {
                _ = controller.Breakpoints.Remove(existing.Id);
                viewState.LastAction = $"REMOVED OPCODE BP 0X{opcode:X4}";
                return;
            }

            _ = controller.Breakpoints.AddOpcodeBreakpoint(opcode, 0xFFFF);
            viewState.LastAction = $"ADDED OPCODE BP 0X{opcode:X4}";
        }

        private static void RemoveSelectedBreakpoint(IEmulatorDebugController controller, SdlDebugViewState viewState)
        {
            var breakpoints = controller.Breakpoints.All;
            if (breakpoints.Count == 0)
            {
                return;
            }

            var selected = Math.Clamp(viewState.BreakpointSelection, 0, breakpoints.Count - 1);
            var removed = controller.Breakpoints.Remove(breakpoints[selected].Id);
            if (removed)
            {
                viewState.BreakpointSelection = Math.Max(0, selected - 1);
                viewState.LastAction = "REMOVED BREAKPOINT";
            }
        }
    }
}
