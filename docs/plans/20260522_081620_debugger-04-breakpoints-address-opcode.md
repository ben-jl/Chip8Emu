# Add Address and Opcode Breakpoints

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Implement first-class breakpoint support for program-counter address matches and opcode matches that halts execution in paused debugger state.

## Context

Plan 1 introduces debugger run-state orchestration, and plan 2 adds instruction-level trace events that include fetched opcode and current `PC`.  
`Chip8Cpu.Step()` in [Chip8Emu.Core/Cpu/Chip8Cpu.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8Cpu.cs) already fetches `opcode` at `PC`, so breakpoint checks can run before execution without extra memory reads.  
Instruction metadata in [Chip8Emu.Core/Cpu/InstructionDecoder.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/InstructionDecoder.cs) enables exact opcode or masked opcode matching strategies.

## Scope

**In scope**:
- Add breakpoint model and manager for address and opcode breakpoints.
- Evaluate breakpoints before executing an instruction.
- Halt debug controller execution on match with explicit stop reason and match details.
- Support enable/disable and remove operations for breakpoints.
- Add unit tests for both breakpoint classes and matching edge cases.

**Out of scope**:
- Memory watchpoints.
- Register value breakpoints.
- Conditional expression breakpoints.
- UI editing workflows.

## Architecture Decisions

- **Decision**: Evaluate breakpoints in the debug control layer before invoking machine step.
  **Rationale**: Keeps CPU instruction semantics unchanged and centralizes stop policy.

- **Decision**: Model opcode breakpoints with value + mask to allow both exact and pattern matches.
  **Rationale**: Supports both “break on exact opcode” and useful grouped opcode detection.

- **Decision**: Keep breakpoint storage in host-agnostic core manager with stable IDs.
  **Rationale**: UI and future hosts can mutate breakpoints through one shared contract.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Debugging/Breakpoints/BreakpointKind.cs` | Add enum for `Address` and `Opcode`. |
| `Chip8Emu.Core/Debugging/Breakpoints/BreakpointDefinition.cs` | Add immutable model with ID, enabled flag, and type payload. |
| `Chip8Emu.Core/Debugging/Breakpoints/BreakpointMatch.cs` | Add match payload used for stop reason details. |
| `Chip8Emu.Core/Debugging/Breakpoints/IBreakpointManager.cs` | Add CRUD/query contract for breakpoint lifecycle. |
| `Chip8Emu.Core/Debugging/Breakpoints/BreakpointManager.cs` | Implement in-memory breakpoint registry and match evaluator. |
| `Chip8Emu.Core/Debugging/IEmulatorDebugController.cs` | Extend API to include breakpoint management and current halt reason payload. |
| `Chip8Emu.Core/Debugging/EmulatorDebugController.cs` | Invoke breakpoint evaluation before frame/instruction execution and pause on match. |
| `Chip8Emu.Core/Machine/Chip8Machine.cs` | Add minimal non-invasive access path for current PC/opcode probe if required by control layer. |
| `Chip8Emu.Test.Core/BreakpointAddressOpcodeTests.cs` | Add core tests for add/remove/enable/disable and halt-on-match semantics. |

## Execution Steps

1. Create breakpoint domain models for `Address` and `Opcode` types with stable identifiers.
2. Implement `BreakpointManager` with:
   1. add/update/remove operations,
   2. enable/disable toggles,
   3. deterministic evaluation order.
3. Define opcode match semantics (`(opcode & mask) == value`) and address match semantics (`PC == address`).
4. Integrate manager into `EmulatorDebugController` update path:
   1. read candidate `PC` and opcode before executing,
   2. evaluate active breakpoints,
   3. if matched, set mode to `Paused` and capture `BreakpointMatch`.
5. Ensure explicit user step command can still execute one instruction when paused at a breakpoint.
6. Add tests for:
   1. exact address hits,
   2. exact opcode hits,
   3. masked opcode hits,
   4. disabled breakpoints not triggering.
7. Validate no regression when no breakpoints exist.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `Breakpoints_Address_ShouldPauseBeforeInstructionExecutes`
  - `Breakpoints_OpcodeExact_ShouldPauseOnMatchingOpcode`
  - `Breakpoints_OpcodeMasked_ShouldPauseOnPatternMatch`
  - `Breakpoints_DisabledEntries_ShouldNotTriggerPause`
- Manual checks (for example: API calls, DB queries)
  - In SDL host, set address and opcode breakpoints, resume execution, and confirm emulator pauses immediately on matching instruction with correct reason details.

## Open Questions

- None. Address/opcode behavior is fully scoped.

