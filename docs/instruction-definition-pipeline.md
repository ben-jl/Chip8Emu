# Instruction Definition Pipeline

The emulator now uses a definition-driven instruction pipeline so opcode semantics are centralized and reusable by CPU execution, assembler work, and disassembler work.

## Core Types

- `InstructionDefinition` (public): canonical instruction metadata (`Mnemonic`, `Mask`, `Pattern`, `Operands`, `Encode`, `Decode`).
- `ParsedOperands` (immutable record): assembler-facing operand input model.
- `DecodedOperands` (immutable record): decode output model.
- `DecodedInstruction` (immutable record): resolved instruction instance (`Opcode`, `InstructionDefinition`, `DecodedOperands`).
- `InstructionDecodeResult` (discriminated result): non-throwing decode contract (`Valid` / `Invalid`).
- `InstructionAliasOptions` + `InstructionVariantKey`: configurable mnemonic aliases by opcode variant (`Mask` + `Pattern`).

## Hybrid Decode API

`InstructionDecoder` exposes both decode modes:

- `Decode(ushort opcode)` returns `InstructionDecodeResult` for tooling that needs tolerant processing.
- `DecodeOrThrow(ushort opcode)` enforces strict fail-fast behavior for emulator execution paths.

`Chip8Cpu` uses `DecodeOrThrow` in `Step()`.

## Instruction Set Source of Truth

`Chip8InstructionSet` contains the ordered CHIP-8 definition table used for:

- pattern matching (`Mask`/`Pattern` specificity)
- decode delegates
- encode delegates

Specialized opcodes like `00E0` and `00EE` are ordered before generic `0nnn` (`SYS`) to preserve correct matching semantics.

## Execution Scheduling Note

Machine scheduling can consume execute metadata (for example, whether a `DRW` occurred in a step) to apply runtime quirks like display wait. Timer cadence is mode-dependent: `StepFrame()` is frame-cadenced while `StepInstruction()` is instruction-cadenced.
