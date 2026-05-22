# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **CHIP-8 emulator** in C# targeting .NET 8.0. CHIP-8 is a simple virtual machine designed for 1970s games. The project is structured around:

- **Chip8Emu.Core**: The emulator core engine (CPU, memory, display, input, timing, debugging)
- **Chip8Emu.SdlHost**: An SDL3-based graphical host with an integrated debugger overlay
- **Chip8Emu.Test.Core**: xUnit test suite covering emulator behavior, instruction execution, and edge cases

## Build & Test Commands

```bash
# Build all projects
dotnet build

# Run all tests (xUnit, located in Chip8Emu.Test.Core)
dotnet test

# Run a specific test class (e.g., CpuTests)
dotnet test --filter "ClassName=Chip8Emu.Test.Core.CpuTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName=Chip8Emu.Test.Core.CpuTests.OpCode8xy4_AddVxVy_WithCarry"

# Run the SDL host (interactive emulator)
dotnet run --project Chip8Emu.SdlHost

# Build for Release
dotnet build -c Release

# Run tests with code coverage
dotnet test /p:CollectCoverage=true
```

To load a ROM in the SDL host, modify the `GetTestMachine()` method in `Chip8Emu.SdlHost/Program.cs` to point to a `.ch8` ROM file.

## Architecture & Design

### Modular Layer Design

The Core is divided into functional subsystems:

- **Machine** (`Chip8Emu.Core/Machine`): Top-level orchestrator. `Chip8Machine` coordinates CPU, memory, display, input, and timers. Exposes `IEmulatorMachine` interface for execution and state access.
- **CPU** (`Chip8Emu.Core/Cpu`): Instruction execution engine. `Chip8Cpu` decodes and executes 16-bit opcodes; `Chip8InstructionSet` is the canonical instruction metadata table.
- **Memory** (`Chip8Emu.Core/Memory`): Address space management via `MemoryBus`; maps RAM, font, and ROM regions.
- **Display** (`Chip8Emu.Core/Display`): Framebuffer (`MonochromeFrameBuffer`) holds 64×32 pixel state; implements `IFrameBuffer`.
- **Input** (`Chip8Emu.Core/Input`): 16-key CHIP-8 keypad state tracking via `KeypadState`.
- **Timing** (`Chip8Emu.Core/Timing`): Delay and sound timer logic; handles quirk-dependent timer cadence.
- **Debugging** (`Chip8Emu.Core/Debugging`): Breakpoints, instruction tracing, disassembly, and step-by-step execution control via `EmulatorDebugController`.

### Instruction Definition Pipeline

The core uses a **definition-driven instruction system** for code reuse across execution, assembly, and disassembly:

- `InstructionDefinition`: Canonical metadata (mnemonic, bitwise mask, pattern, operands).
- `InstructionDecoder`: Decodes 16-bit opcodes into `DecodedInstruction` objects; offers both tolerant (`Decode()`) and strict (`DecodeOrThrow()`) modes.
- `Chip8InstructionSet`: Ordered table of instruction definitions used for pattern matching and encoding/decoding.

Pattern matching is order-dependent: specialized opcodes like `00E0` (clear display) are listed before generic `0nnn` (SYS).

### Quirk & Variant Support

`EmulationOptions` encapsulates variant-specific behavior (e.g., timer cadence, DRW display-wait). Preset configurations exist for CosmacVIP, SuperCHIP-8 (legacy & modern), and XO-CHIP.

### Debugging Architecture

- **DebugController**: Manages breakpoints, execution modes (run/step), and pause state.
- **Breakpoints**: Address, opcode, memory, register, and conditional breakpoints.
- **Disassembler**: Runtime disassembly from bytecode (Mnemonic strings for each instruction).
- **Trace Pipeline**: Optional in-memory instruction and memory-access tracing for performance analysis.
- **SDL Debug UI**: Keyboard-driven overlay showing registers, stack, memory, breakpoints, and execution trace.

## Key Design Patterns

- **Immutable Records**: `DecodedOperands`, `DecodedInstruction`, `ParsedOperands` ensure thread-safe instruction state.
- **Result Types**: `InstructionDecodeResult` uses discriminated unions for robust error handling without exceptions.
- **Snapshot Pattern**: `MachineSnapshot` and `DebugStateSnapshot` capture state for diagnostics.
- **Quirks via Enum**: Runtime behavior configured at initialization; variants apply different instruction semantics (e.g., VF register clear on DRAW).
- **Interface-Driven Host**: `IEmulatorMachine` and `IEmulatorDebugController` allow headless testing and alternative UI hosts.

## Running the Emulator

The SDL host provides an interactive CHIP-8 environment:

- **Normal Mode**: CHIP-8 keypad mapped to keyboard (1-4, Q-R, A-F, Z-V).
- **Debug Mode**: Press `D` to toggle the debug overlay. Includes:
  - CPU registers and program counter
  - Stack contents
  - Memory view
  - Instruction trace
  - Breakpoint management
  - Step/Continue/Step-Over controls

Step through code with:
- Space: Single-step instruction
- Enter: Step frame (execute up to `InstructionsPerFrame`)
- C: Continue to next breakpoint (or full speed)

## Test Organization

Tests are organized by subsystem:

- **CpuTests.cs**: Comprehensive instruction execution coverage (all opcodes).
- **Chip8MachineIntegrationRegressionTests.cs**: Cross-system integration tests (ROM execution paths).
- **DebugControllerTests.cs**: Breakpoint and stepping behavior.
- **TracePipelineTests.cs**: Instruction and memory tracing.
- **Breakpoint*Tests.cs**: Conditional and memory breakpoint logic.

## Common Development Tasks

**Adding a new quirk variant:**
1. Define the quirk boolean in `EmulationOptions` and add a configuration preset (e.g., `EmulationOptions.NewVariant`).
2. Apply the quirk logic at the appropriate instruction (usually in `Chip8Cpu.Execute()`).
3. Add integration tests in `Chip8MachineIntegrationRegressionTests.cs` to verify variant behavior.

**Adding a new instruction or opcode handler:**
1. Add `InstructionDefinition` entry to `Chip8InstructionSet`.
2. Implement encode/decode delegates in the definition.
3. Add handler logic in `Chip8Cpu.Execute()`.
4. Add unit tests in `CpuTests.cs`.

**Debugging a failing test or ROM:**
1. Use the SDL host with debug mode enabled (`D` key).
2. Set breakpoints on addresses or opcodes in the debug UI.
3. Use the trace view to inspect instruction execution history.
4. Check `MemoryAccessesSince()` to audit memory reads/writes.

**Adding a new debug feature:**
1. Extend `IEmulatorDebugController` interface if needed.
2. Implement feature in `EmulatorDebugController` (e.g., new breakpoint type).
3. Add tests in `DebugControllerTests.cs`.
4. If it requires UI, update `SdlDebugOverlayRenderer` in `Chip8Emu.SdlHost/DebugUi/`.

## Project Conventions

- **Nullable enabled**: All projects use `<Nullable>enable</Nullable>`.
- **Implicit usings**: `using` statements are implicit; namespace prefixes are kept short.
- **Internal visibility**: Core classes are `internal` where not part of the public host interface (e.g., `Chip8Cpu`, CPU execution logic).
- **InternalsVisibleTo**: `Chip8Emu.Core` exposes internals to `Chip8Emu.Test.Core` for white-box testing.
- **Test naming**: Test methods follow `OpCode[Hex]_[Description]_[Condition]` pattern (e.g., `OpCode8xy4_AddVxVy_WithCarry`).

## Documentation

Key design decisions are documented in `docs/`:
- **instruction-definition-pipeline.md**: Decoding and instruction definition architecture.
- **timendus-quirks-support.md**: CHIP-8 variant behaviors and quirk matrix.
- **plans/**: Feature roadmaps (debugger implementation phases, instruction refactoring, quirk additions).
