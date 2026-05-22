# CHIP-8 Debugger Roadmap (Atomic Plan Series)

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Define an execution-ready sequence of atomic implementation plans for CHIP-8 v1 debugging in a host-agnostic core with SDL-first keyboard UX.

## Context

The emulator core currently exposes instruction and frame stepping through `IEmulatorMachine` in [Chip8Emu.Core/Machine/IEmulatorMachine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/IEmulatorMachine.cs), with runtime behavior implemented by `Chip8Machine` in [Chip8Emu.Core/Machine/Chip8Machine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/Chip8Machine.cs) and opcode execution in `Chip8Cpu` in [Chip8Emu.Core/Cpu/Chip8Cpu.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8Cpu.cs).  
SDL host execution is currently a single game loop in [Chip8Emu.SdlHost/Program.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/Program.cs) with keyboard mapping in [Chip8Emu.SdlHost/SdlInput.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/SdlInput.cs).  
Instruction metadata already exists in [Chip8Emu.Core/Cpu/Chip8InstructionSet.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8InstructionSet.cs), which can power disassembly and opcode breakpoints.

## Scope

**In scope**:
- Sequence six atomic plans that deliver: pause/resume/step, live trace buffer, ROM disassembly, and all requested breakpoint classes (address, opcode, memory, register, conditional).
- Keep debugger core host-agnostic and reusable beyond SDL.
- Keep debug data in-memory only.
- Deliver SDL keyboard-first debugger interaction and panels.

**Out of scope**:
- SCHIP/XO-CHIP debugger semantics in this v1 sequence.
- Step-over/step-out semantics.
- Persisted trace logs, external database storage, or file export.

## Architecture Decisions

- **Decision**: Use a layered roadmap where each plan is independently shippable and testable.
  **Rationale**: Reduces risk and gives clean checkpoints for incremental integration.

- **Decision**: Keep debugger domain types in `Chip8Emu.Core` under a dedicated `Debugging` namespace.
  **Rationale**: Preserves host-agnostic ownership and lets SDL act as a consumer only.

- **Decision**: Sequence breakpoints in two phases (`address/opcode` before `memory/register/conditional`).
  **Rationale**: Establishes halt pipeline first, then extends with richer predicates.

## Files to Create / Modify

| File | Change |
|------|--------|
| `docs/plans/20260522_081620_debugger-01-core-debug-control.md` | Atomic plan 1: debug control API and pause/resume/step orchestration. |
| `docs/plans/20260522_081620_debugger-02-trace-pipeline.md` | Atomic plan 2: layered trace event instrumentation and in-memory buffer. |
| `docs/plans/20260522_081620_debugger-03-disassembler-runtime-view.md` | Atomic plan 3: ROM disassembly and runtime instruction-window model. |
| `docs/plans/20260522_081620_debugger-04-breakpoints-address-opcode.md` | Atomic plan 4: address and opcode breakpoints. |
| `docs/plans/20260522_081620_debugger-05-breakpoints-memory-register-conditional.md` | Atomic plan 5: memory/register/conditional breakpoints. |
| `docs/plans/20260522_081620_debugger-06-sdl-keyboard-debugger-ui.md` | Atomic plan 6: SDL keyboard-driven debugger UI and workflows. |

## Execution Steps

1. Implement plan 1 to establish host-agnostic control points (`pause/resume/step`) and debugger state semantics.
2. Implement plan 2 to capture cross-layer runtime events in a bounded in-memory buffer.
3. Implement plan 3 to expose disassembly listings and focused current-PC windows for debugger consumers.
4. Implement plan 4 to add halt-on-match breakpoints for program counter and opcode patterns.
5. Implement plan 5 to expand breakpoint engine with memory/register watches and conditional expressions.
6. Implement plan 6 to expose all accumulated capabilities in the SDL host via keyboard-first workflows.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - Add focused test classes per milestone in `Chip8Emu.Test.Core` for control flow, traces, disassembly, and breakpoint behavior.
  - Add SDL-host-facing integration tests only where pure unit tests are not sufficient (for input mapping and overlay state transitions).
- Manual checks (for example: API calls, DB queries)
  - Run `Chip8Emu.SdlHost` with a test ROM, toggle pause/resume, single-step, inspect CPU state/disassembly, and confirm each breakpoint type halts with clear reason metadata.

## Open Questions

- None. User constraints are sufficient for this roadmap.

