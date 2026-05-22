# Add Memory, Register, and Conditional Breakpoints

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Extend the breakpoint engine with memory watchpoints, register breakpoints, and conditional breakpoints evaluated against live CPU/machine state.

## Context

Address/opcode breakpoint flow is established by plan 4 in the debug controller and breakpoint manager.  
`Chip8Machine.CurrentSnapshot()` in [Chip8Emu.Core/Machine/Chip8Machine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/Chip8Machine.cs) returns `MachineSnapshot`, which already contains register/timer values through [Chip8Emu.Core/Diagnostics/MachineSnapshot.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Diagnostics/MachineSnapshot.cs).  
Memory read/write operations are centralized in [Chip8Emu.Core/Memory/MemoryBus.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Memory/MemoryBus.cs), making watchpoint hooks practical.

## Scope

**In scope**:
- Add memory watchpoints (read/write, address or range).
- Add register breakpoints (value compare and change-detected modes).
- Add conditional breakpoints with a constrained expression grammar.
- Integrate all new breakpoint types into debugger halt flow with clear match payloads.
- Add tests for evaluation correctness and deterministic behavior.

**Out of scope**:
- Arbitrary user scripting languages.
- Time-travel debugging.
- Persisted breakpoint profiles.
- UI editing/authoring workflows (handled in plan 6).

## Architecture Decisions

- **Decision**: Represent conditional breakpoints with a constrained expression grammar over known symbols (`PC`, `I`, `V0`-`VF`, timers, opcode).
  **Rationale**: Enables useful power while avoiding runtime code execution risk.

- **Decision**: Evaluate register/condition breakpoints at instruction boundary, and memory watchpoints on read/write events.
  **Rationale**: Aligns trigger timing with semantic event type and yields predictable stops.

- **Decision**: Cache compiled conditional expressions in breakpoint definitions.
  **Rationale**: Avoids reparsing every instruction and keeps runtime cost manageable.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Debugging/Breakpoints/BreakpointKind.cs` | Extend enum with `Memory`, `Register`, `Conditional`. |
| `Chip8Emu.Core/Debugging/Breakpoints/MemoryBreakpointDefinition.cs` | Add read/write watchpoint model with address/range settings. |
| `Chip8Emu.Core/Debugging/Breakpoints/RegisterBreakpointDefinition.cs` | Add register target and comparison mode/value model. |
| `Chip8Emu.Core/Debugging/Breakpoints/ConditionalBreakpointDefinition.cs` | Add condition string + compiled predicate fields. |
| `Chip8Emu.Core/Debugging/Breakpoints/Condition/BreakpointConditionLexer.cs` | Add tokenizer for constrained breakpoint conditions. |
| `Chip8Emu.Core/Debugging/Breakpoints/Condition/BreakpointConditionParser.cs` | Add parser and AST for expression grammar. |
| `Chip8Emu.Core/Debugging/Breakpoints/Condition/BreakpointConditionCompiler.cs` | Compile AST to predicate delegate over machine debug context. |
| `Chip8Emu.Core/Debugging/Breakpoints/BreakpointManager.cs` | Extend match evaluation to memory/register/conditional types. |
| `Chip8Emu.Core/Debugging/EmulatorDebugController.cs` | Feed evaluation context each step and pause on advanced match results. |
| `Chip8Emu.Core/Memory/MemoryBus.cs` | Surface read/write events to breakpoint evaluator path. |
| `Chip8Emu.Test.Core/BreakpointMemoryRegisterConditionalTests.cs` | Add tests for all new breakpoint classes and expression behavior. |

## Execution Steps

1. Extend breakpoint models and manager contracts to include `Memory`, `Register`, and `Conditional` definitions.
2. Add memory watchpoint matching hooks at `MemoryBus.Read` and `MemoryBus.Write` boundaries.
3. Add register breakpoint evaluation against snapshot deltas across instruction boundaries.
4. Implement a constrained condition grammar supporting:
   1. identifiers (`PC`, `I`, `DT`, `ST`, `V0`-`VF`, `OPCODE`),
   2. integer literals (hex + decimal),
   3. comparison operators (`==`, `!=`, `<`, `<=`, `>`, `>=`),
   4. boolean composition (`&&`, `||`, parentheses).
5. Compile parsed conditions to cached predicates and fail-fast on invalid expressions.
6. Integrate match evaluation into debug update flow with detailed `BreakpointMatch` reason payloads.
7. Add tests for:
   1. memory read and write triggers,
   2. register value and change-detected triggers,
   3. conditional expression truth table correctness,
   4. invalid condition rejection with actionable errors.
8. Validate existing address/opcode breakpoint behavior remains unchanged.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `Breakpoints_MemoryWrite_ShouldPauseOnConfiguredAddress`
  - `Breakpoints_MemoryRead_ShouldPauseWhenReadWatchpointEnabled`
  - `Breakpoints_RegisterValue_ShouldPauseWhenComparisonMatches`
  - `Breakpoints_Conditional_ShouldPauseWhenExpressionEvaluatesTrue`
  - `Breakpoints_Conditional_InvalidExpression_ShouldReturnValidationError`
- Manual checks (for example: API calls, DB queries)
  - Configure one breakpoint of each type, run ROM, and verify pause occurs only on expected transitions with correct match details.

## Open Questions

- None. Requested breakpoint classes are fully defined.

