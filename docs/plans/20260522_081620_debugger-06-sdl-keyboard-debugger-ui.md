# Add Keyboard-Driven SDL Debugger UI

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Expose debugger controls and live debug data in the SDL host through a keyboard-first overlay UI (pause/resume/step, CPU state, disassembly, breakpoints, and trace tail).

## Context

SDL host loop currently resides in [Chip8Emu.SdlHost/Program.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/Program.cs), and keyboard handling is in [Chip8Emu.SdlHost/SdlInput.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/SdlInput.cs).  
Core debugger capabilities from plans 1-5 are host-agnostic and should be consumed as APIs rather than duplicated in host logic.  
The host currently renders only framebuffer pixels via [Chip8Emu.SdlHost/SdlFramebufferRenderer.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/SdlFramebufferRenderer.cs), so textual/panel rendering must be introduced.

## Scope

**In scope**:
- Add SDL keyboard bindings for debugger actions (`pause/resume`, `step instruction`, panel focus/navigation).
- Add a lightweight overlay renderer for CPU state, disassembly window, breakpoints list, and recent trace lines.
- Add keyboard workflows for creating/toggling/removing breakpoints.
- Keep gameplay input and debugger input mode-separated to avoid key conflicts.
- Add tests for host-side input state transitions where feasible.

**Out of scope**:
- Mouse interaction workflows.
- Advanced window docking/layout customization.
- Persisting UI preferences.
- Non-SDL host implementations.

## Architecture Decisions

- **Decision**: Keep debugger state and logic in core; SDL layer only maps keys to controller commands and renders view models.
  **Rationale**: Maintains host-agnostic debugger architecture and avoids duplicate behavior logic.

- **Decision**: Use a simple bitmap text renderer (or equivalent immediate-mode glyph renderer) instead of pulling full UI frameworks.
  **Rationale**: Keeps dependency footprint low and preserves deterministic keyboard-first focus.

- **Decision**: Introduce explicit host mode switch (`EmulationInputMode` vs `DebugInputMode`).
  **Rationale**: Prevents CHIP-8 keypad events from colliding with debugger command keys.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.SdlHost/Program.cs` | Integrate debug controller update path and debugger overlay render/update calls. |
| `Chip8Emu.SdlHost/SdlInput.cs` | Split emulation keypad handling from debugger command handling and mode switching. |
| `Chip8Emu.SdlHost/Debug/SdlDebugKeyBindings.cs` | Define keyboard command map for debugger actions and navigation. |
| `Chip8Emu.SdlHost/Debug/SdlDebugViewState.cs` | Track panel focus, selection indices, and active input mode state. |
| `Chip8Emu.SdlHost/Debug/SdlDebugOverlayRenderer.cs` | Render debugger panels (CPU/disassembly/breakpoints/trace) over framebuffer output. |
| `Chip8Emu.SdlHost/Debug/SdlBitmapTextRenderer.cs` | Render fixed-width text for keyboard-first overlay panels. |
| `Chip8Emu.SdlHost/Chip8Emu.SdlHost.csproj` | Include any required assets or minor package additions if text rendering helper requires them. |
| `Chip8Emu.Test.Core/SdlDebugInputMappingTests.cs` (or host test project equivalent) | Add tests for keybinding-to-command mapping and mode-switch correctness. |
| `docs/` (new note) | Add quick reference of debugger hotkeys and panel navigation. |

## Execution Steps

1. Add SDL host debug bootstrap:
   1. instantiate core debug controller and trace/disassembly providers,
   2. route emulation stepping through controller update path.
2. Add input mode state and debugger key map:
   1. toggle debug overlay visibility,
   2. pause/resume,
   3. step instruction,
   4. panel navigation and selection movement.
3. Implement `SdlDebugOverlayRenderer` with four primary panels:
   1. CPU/timer snapshot,
   2. disassembly window centered on `PC`,
   3. breakpoint list/status,
   4. recent trace events.
4. Add keyboard workflows for breakpoint editing:
   1. add address/opcode breakpoint from current selection,
   2. toggle enable/disable,
   3. remove selected breakpoint.
5. Ensure debug input commands are consumed without forwarding to CHIP-8 keypad handling.
6. Add host-side tests for command mapping and mode transitions; add manual smoke path for overlay rendering.
7. Document hotkeys in a short `docs/` reference file for developer onboarding.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `SdlDebugInput_ShouldTogglePauseResume`
  - `SdlDebugInput_ShouldIssueSingleInstructionStepWhenPaused`
  - `SdlDebugInput_ShouldNotForwardDebuggerKeysToChip8Keypad`
  - `SdlDebugViewState_ShouldMoveSelectionAcrossPanelsWithKeyboard`
- Manual checks (for example: API calls, DB queries)
  - Run SDL host, open debugger overlay, pause ROM, step instructions, inspect CPU/disassembly/trace panels, and manipulate breakpoints entirely by keyboard.

## Open Questions

- None. Keyboard-first SDL host behavior is defined for this milestone.

