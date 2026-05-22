# Add In-Memory Cross-Layer Trace Pipeline

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Capture CHIP-8 execution across machine, CPU, memory, timers, input, and display layers in a bounded in-memory trace stream for live debugger inspection.

## Context

`Chip8Machine` in [Chip8Emu.Core/Machine/Chip8Machine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/Chip8Machine.cs) coordinates frame/instruction stepping and timer ticking.  
`Chip8Cpu` in [Chip8Emu.Core/Cpu/Chip8Cpu.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8Cpu.cs) reads opcodes, decodes, and executes handlers, but it currently emits no execution events.  
`MemoryBus` in [Chip8Emu.Core/Memory/MemoryBus.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Memory/MemoryBus.cs), timers, keypad, and display operations are currently opaque to external observers.

## Scope

**In scope**:
- Add a core trace event model and sink interface.
- Instrument key runtime touchpoints (instruction fetch/decode/execute boundaries, memory reads/writes, timer ticks, key state changes, draw events).
- Add bounded in-memory ring buffer storage for recent events.
- Expose read APIs for hosts to query recent trace entries.
- Add focused tests for event ordering and payload correctness.

**Out of scope**:
- Breakpoint-trigger behavior.
- Persisting traces to disk.
- Filtering/search UI in SDL.
- SCHIP/XO-CHIP-specific trace metadata.

## Architecture Decisions

- **Decision**: Represent trace entries as strongly typed immutable records (base `TraceEvent` + derived event types).
  **Rationale**: Preserves type safety and simplifies debugger consumers versus string logs.

- **Decision**: Use optional instrumentation hooks so non-debug runs can avoid event allocation when no sink is attached.
  **Rationale**: Keeps normal runtime path lightweight and avoids mandatory tracing overhead.

- **Decision**: Keep trace storage bounded via ring buffer with configurable capacity.
  **Rationale**: Satisfies in-memory-only requirement without unbounded growth.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Debugging/Trace/TraceEvent.cs` | Add base trace event model with common metadata (sequence, timestamp, event kind). |
| `Chip8Emu.Core/Debugging/Trace/InstructionTraceEvents.cs` | Add CPU instruction lifecycle events (fetch/decode/execute-complete/fault). |
| `Chip8Emu.Core/Debugging/Trace/MemoryTraceEvents.cs` | Add memory read/write event models. |
| `Chip8Emu.Core/Debugging/Trace/MachineTraceEvents.cs` | Add frame/timer/input/display event models. |
| `Chip8Emu.Core/Debugging/Trace/ITraceSink.cs` | Add sink contract for trace emission. |
| `Chip8Emu.Core/Debugging/Trace/InMemoryTraceBuffer.cs` | Add bounded ring-buffer sink and query API. |
| `Chip8Emu.Core/Memory/IMemoryBus.cs` | Add optional trace-aware methods or callback integration points. |
| `Chip8Emu.Core/Memory/MemoryBus.cs` | Emit memory read/write events when trace sink is present. |
| `Chip8Emu.Core/Cpu/Chip8Cpu.cs` | Emit instruction and execution events, including decoded metadata and execution result. |
| `Chip8Emu.Core/Machine/Chip8Machine.cs` | Emit frame begin/end, timer tick, and key-state mutation events. |
| `Chip8Emu.Test.Core/TracePipelineTests.cs` | Add ordering/payload tests across layer boundaries. |

## Execution Steps

1. Create trace domain models and `ITraceSink` contract under `Chip8Emu.Core.Debugging.Trace`.
2. Implement `InMemoryTraceBuffer` with fixed capacity and monotonic sequence numbers for stable ordering.
3. Add optional trace sink plumbing in constructors where runtime ownership exists (`Chip8Machine` -> `Chip8Cpu` -> `MemoryBus` path).
4. Instrument `Chip8Cpu.Step()` to emit:
   1. instruction fetch (`PC`, raw opcode),
   2. decode result (`Mnemonic`, operands),
   3. completion (`PC before/after`, draw flag),
   4. execution faults for unknown opcodes.
5. Instrument `MemoryBus` reads/writes and `Chip8Machine` timer/input/frame boundaries.
6. Ensure trace emission is null-safe and bypassed when sink is absent.
7. Add tests asserting:
   1. deterministic sequence ordering,
   2. correct payload fields for representative opcodes,
   3. ring-buffer eviction behavior at capacity boundaries.
8. Validate existing emulator tests remain green with tracing disabled by default.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `TracePipeline_ShouldCaptureInstructionLifecycleEventsInOrder`
  - `TracePipeline_ShouldCaptureMemoryReadsAndWritesWithAddresses`
  - `TracePipeline_ShouldCaptureFrameAndTimerEventsFromMachineLayer`
  - `InMemoryTraceBuffer_ShouldEvictOldestEventsAtCapacity`
- Manual checks (for example: API calls, DB queries)
  - Run a short ROM loop and inspect trace buffer snapshots to confirm mixed event kinds (CPU/memory/timer/display/input) appear in expected order.

## Open Questions

- None. Trace scope and in-memory constraint are explicit.

