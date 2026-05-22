# Add Host-Agnostic Debug Control Surface (Pause/Resume/Step)

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Introduce a host-agnostic debugger control API that can pause/resume execution and step exactly one CHIP-8 instruction.

## Context

Execution entry points are currently `StepInstruction()` and `StepFrame()` on `IEmulatorMachine` in [Chip8Emu.Core/Machine/IEmulatorMachine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/IEmulatorMachine.cs).  
`Chip8Machine` in [Chip8Emu.Core/Machine/Chip8Machine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/Chip8Machine.cs) owns timing and delegates instruction execution to `Chip8Cpu`.  
SDL host currently drives stepping directly in [Chip8Emu.SdlHost/Program.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/Program.cs), so there is no reusable debug orchestration layer yet.

## Scope

**In scope**:
- Add core debugging contracts for execution mode (`Running`, `Paused`) and debug commands.
- Add a host-agnostic coordinator that wraps `IEmulatorMachine` and determines whether to step frame, step instruction, or idle.
- Add metadata for stop reason (`UserPause`, `StepComplete`) and last transition time/cycle.
- Add unit tests for pause/resume and single-step semantics.

**Out of scope**:
- Breakpoint evaluation.
- Trace/event capture.
- Disassembly model and UI rendering.
- Step-over/step-out behaviors.

## Architecture Decisions

- **Decision**: Add a `Debugging` namespace under `Chip8Emu.Core` with public contracts.
  **Rationale**: Keeps debugger API reusable by SDL and any future host.

- **Decision**: Keep `IEmulatorMachine` as the execution primitive and layer control in a separate coordinator.
  **Rationale**: Avoids invasive changes to machine internals in the first milestone.

- **Decision**: Make pause/resume/step command handling explicit and deterministic (no hidden auto-step behavior while paused).
  **Rationale**: Debug workflows depend on strict command semantics.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Debugging/DebugExecutionMode.cs` | Add enum for `Running` / `Paused`. |
| `Chip8Emu.Core/Debugging/DebugStopReason.cs` | Add stop-reason model for debugger state transitions. |
| `Chip8Emu.Core/Debugging/DebugStateSnapshot.cs` | Add immutable state payload exposed to hosts. |
| `Chip8Emu.Core/Debugging/IEmulatorDebugController.cs` | Add host-agnostic control contract (`Pause`, `Resume`, `StepInstructionOnce`, `Update`). |
| `Chip8Emu.Core/Debugging/EmulatorDebugController.cs` | Implement orchestration over `IEmulatorMachine`. |
| `Chip8Emu.Core/Machine/Chip8Machine.cs` | Minor adjustments if needed to expose lightweight instruction counter/tick metadata used by debug state snapshots. |
| `Chip8Emu.Test.Core/DebugControllerTests.cs` | Add tests for pause/resume/step determinism and mode transitions. |
| `Chip8Emu.Test.Core/Chip8Emu.Test.Core.csproj` | Include new debugger test file(s) if needed. |

## Execution Steps

1. Create core debugger models (`DebugExecutionMode`, `DebugStopReason`, `DebugStateSnapshot`) with XML docs.
2. Define `IEmulatorDebugController` as the single host-facing control contract for run-state and stepping commands.
3. Implement `EmulatorDebugController` around `IEmulatorMachine`:
   1. `Update()` executes frame stepping only in `Running`.
   2. `StepInstructionOnce()` executes exactly one instruction only from `Paused`.
   3. `Pause()` and `Resume()` update mode and stop reason deterministically.
4. Add optional machine progress metadata (instruction count) if needed to validate stepping invariants cleanly in tests.
5. Wire minimal SDL integration path in plan scope only if needed for compile-time validation, but keep UI behavior deferred.
6. Add tests for:
   1. `Pause()` preventing frame execution.
   2. `Resume()` restoring frame execution.
   3. `StepInstructionOnce()` advancing PC exactly one instruction while remaining paused.
   4. Repeated `StepInstructionOnce()` calls producing deterministic state.
7. Run full core test suite and ensure no behavior regression in non-debug execution paths.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `DebugController_Pause_ShouldStopFrameExecution`
  - `DebugController_Resume_ShouldContinueFrameExecution`
  - `DebugController_StepInstructionOnce_ShouldAdvanceOneInstructionWhilePaused`
  - `DebugController_StepInstructionOnce_ShouldRemainPausedAfterStep`
- Manual checks (for example: API calls, DB queries)
  - In a temporary SDL host hook, bind a key to pause and another to step once; confirm visible ROM progression only occurs when expected.

## Open Questions

- None. Behavior requirements for this milestone are explicit.

