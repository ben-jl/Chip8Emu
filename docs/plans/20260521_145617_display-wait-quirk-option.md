# Add Configurable Display Wait Quirk for Original CHIP-8 Behavior

> **Created**: 20260521_145617
> **Status**: Draft

## Goal

Add a configurable display-wait quirk so `DRW` effectively synchronizes with vblank semantics (max one sprite draw gate per frame) for original COSMAC-style behavior while preserving current default behavior for modern ROM compatibility.

## Context

`Chip8Machine` currently executes CPU work in [Chip8Machine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/Chip8Machine.cs) by running `InstructionsPerFrame` calls to `StepInstruction()` inside `StepFrame()`. `StepInstruction()` calls `Chip8Cpu.Step()` and advances timers on instruction cadence using `_instructionsSinceLastTimerTick`.

`Chip8Cpu` in [Chip8Cpu.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8Cpu.cs) currently returns `void` from `Step()`, so the machine cannot tell whether the executed opcode was `DRW` (`Chip8InstructionSet.PatternDRW`) and therefore cannot short-circuit frame execution when display wait is enabled.

Quirk toggles are centralized in [EmulationOptions.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/EmulationOptions.cs) (`ShiftUsesVy`, `ClipSprites`, `ResetCarryFlagOnBitwiseOps`, `IncrementIOnStoreLoadMemoryOps`), and current quirk/timing tests live in [CpuTests.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Test.Core/CpuTests.cs) and [Chip8MachineBasicTests.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Test.Core/Chip8MachineBasicTests.cs).

SDL host frame cadence in [Program.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/Program.cs) calls `machine.StepFrame()` then delays ~16ms, so this is the correct integration point for vblank-like frame gating.

## Scope

**In scope**:
- Add a new `EmulationOptions` boolean for display-wait behavior.
- Make CPU step expose enough execution metadata for machine-level frame gating.
- Update `Chip8Machine.StepFrame()` to stop executing further instructions after a draw when the quirk is enabled.
- Ensure timer ticking remains deterministic with early frame exits.
- Add unit tests validating display-wait-on and display-wait-off behavior and timer interaction.

**Out of scope**:
- Real wall-clock interrupt scheduling, threading, or actual GPU/scanline timing emulation.
- SCHIP/XO-CHIP extended display timing semantics.
- UI/runtime profile system for grouped quirk presets (can be layered later).

## Architecture Decisions

- **Decision**: Add `DisplayWaitOnDraw` as a new `EmulationOptions` flag (default `false`).
  **Rationale**: Keeps behavior opt-in for strict original compatibility without regressing existing ROM behavior that assumes faster draw throughput, and remains a standalone toggle for now (no preset/profile coupling).

- **Decision**: Return a lightweight step result from `Chip8Cpu.Step()` (for example `CpuStepResult` with `DrewSprite`).
  **Rationale**: Avoids re-decoding opcodes in `Chip8Machine` and keeps instruction identity knowledge in CPU execution flow.

- **Decision**: Implement display wait at `Chip8Machine.StepFrame()` (not inside `DRW` itself).
  **Rationale**: Vblank gating is a frame scheduler concern; `DRW` should remain a pure instruction effect.

- **Decision**: In `StepFrame()`, tick timers once per frame after execution loop, even when draw wait exits early.
  **Rationale**: Prevents timer starvation when early exit means fewer than `InstructionsPerFrame` instructions execute; preserves 60Hz conceptual cadence for frame-driven execution.

- **Decision**: Keep `StepInstruction()` instruction-cadence timer semantics intact for debugger/unit-test single-stepping.
  **Rationale**: Avoids breaking existing tests and preserves useful deterministic behavior for instruction-level stepping workflows.

- **Decision**: Explicitly document timer cadence as mode-dependent (`StepInstruction` cadence vs `StepFrame` cadence).
  **Rationale**: Prevents confusion when mixing stepping modes and makes timing behavior intentional for emulator users, tests, and future tooling.

- **Decision**: Use a hybrid scheduling API style: strict step metadata for machine orchestration plus tolerant external configuration via option toggle.
  **Rationale**: Mirrors existing hybrid decode philosophy (strict execution path, configurable tooling/runtime behavior) and minimizes invasive API churn.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Machine/EmulationOptions.cs` | Add `DisplayWaitOnDraw` option with XML summary and conservative default. |
| `Chip8Emu.Core/Cpu/Chip8Cpu.cs` | Change `Step()` to return step metadata; make `Execute(...)` communicate whether current opcode was `DRW`. |
| `Chip8Emu.Core/Cpu/CpuStepResult.cs` (new) | Add lightweight immutable result record/struct describing per-instruction execution traits (initially `DrewSprite`). |
| `Chip8Emu.Core/Machine/Chip8Machine.cs` | Update `StepFrame()` to break on draw when quirk enabled and revise frame timer tick placement to keep deterministic behavior. |
| `Chip8Emu.Core/Machine/IEmulatorMachine.cs` | Add XML docs clarifying timer cadence differences between `StepInstruction()` and `StepFrame()`. |
| `Chip8Emu.Core/Machine/EmulationOptions.cs` | Document how `DisplayWaitOnDraw` interacts with frame-based stepping and timer cadence expectations. |
| `Chip8Emu.Test.Core/Chip8MachineBasicTests.cs` | Extend timing/frame tests for early-exit draw scenarios. |
| `Chip8Emu.Test.Core/Chip8MachineDisplayWaitTests.cs` (new) | Add focused behavior tests for quirk enabled vs disabled around `DRW` throughput and PC/register progression. |
| `Chip8Emu.SdlHost/Program.cs` (optional dev-only) | Optionally set `DisplayWaitOnDraw = true` in local test machine config for Timendus quirks smoke testing. |
| `docs/instruction-definition-pipeline.md` | Add short note that execution scheduling can consume decode/execute metadata (draw detection) in machine layer and that timer cadence is stepping-mode dependent. |

## Execution Steps

1. Add `DisplayWaitOnDraw` to `EmulationOptions` with default `false` and concise docs describing original interpreter behavior.
2. Introduce `CpuStepResult` as an immutable record/readonly struct in `Chip8Emu.Core/Cpu`.
3. Refactor `Chip8Cpu.Step()` to return `CpuStepResult`; propagate result from `Execute(...)` by setting `DrewSprite = true` for `PatternDRW` and `false` otherwise.
4. Update all `Chip8Cpu.Step()` call sites (`Chip8Machine`, test harness helpers) to consume or ignore returned metadata as appropriate.
5. Refactor `Chip8Machine.StepFrame()` execution loop:
   1. Execute up to `InstructionsPerFrame` instructions.
   2. If `DisplayWaitOnDraw` is enabled and `CpuStepResult.DrewSprite` is true, stop the loop for the current frame.
   3. Tick timers once at frame end.
6. Keep `StepInstruction()` as single-instruction execution with existing instruction-cadence timer logic so current single-step tests remain valid.
7. Add explicit XML/documentation notes in `IEmulatorMachine`, `Chip8Machine`, and `EmulationOptions` stating timer cadence is mode-dependent:
   1. `StepInstruction()` advances timers by instruction cadence.
   2. `StepFrame()` advances timers by frame cadence.
   3. `DisplayWaitOnDraw` may reduce instructions executed in a frame while preserving frame-level timer cadence.
8. Add focused tests:
   1. `DisplayWaitOnDraw=true` stops frame execution immediately after first `DRW` (assert PC/register progress).
   2. `DisplayWaitOnDraw=false` continues executing additional instructions in same frame.
   3. Timers still tick once per frame when display wait short-circuits.
   4. Existing draw clipping/wrapping tests remain unchanged and passing.
9. Run unit tests and fix regressions around step signatures, timer assumptions, and frame-control logic.
10. Smoke-test Timendus quirks ROM in SDL host with `DisplayWaitOnDraw=true` and verify display-wait check no longer fails.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `Chip8Machine_StepFrame_ShouldStopAfterDraw_WhenDisplayWaitOnDrawEnabled`: validates vblank-style gating.
  - `Chip8Machine_StepFrame_ShouldContinuePastDraw_WhenDisplayWaitOnDrawDisabled`: validates current high-throughput behavior is preserved.
  - `Chip8Machine_StepFrame_ShouldTickTimersOnce_WhenDisplayWaitExitsEarly`: guards against timer starvation regression.
  - Existing `Chip8MachineBasicTests` and `CpuTests` continue to pass to confirm no CPU behavior regressions.
- Manual checks (for example: API calls, DB queries)
  - Run Timendus quirks ROM in SDL host with `DisplayWaitOnDraw=true`, confirm display-wait section passes and no regressions in prior quirk sections.
  - Review XML docs/intellisense text on `StepInstruction()` and `StepFrame()` to confirm mode-dependent timer cadence is explicitly stated.

## Open Questions

- None currently.
