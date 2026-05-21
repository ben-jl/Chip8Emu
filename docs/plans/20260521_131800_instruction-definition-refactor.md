# Refactor CHIP-8 Instruction Model to InstructionDefinition for Assembler/Disassembler Support

> **Created**: 20260521_131800
> **Status**: Implemented

## Goal

Replace the current field-only decoded `Instruction` model with a definition-driven instruction table based on `InstructionDefinition` so encode/decode metadata is shared across CPU execution, future assembler, and future disassembler.

## Context

`Chip8Emu.Core` currently decodes opcodes by extracting fixed fields in [Chip8Emu.Core/Cpu/InstructionDecoder.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/InstructionDecoder.cs), returning a lightweight [Instruction.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Instruction.cs) record struct (`Opcode`, `X`, `Y`, `N`, `NN`, `NNN`). Execution then branches in [Chip8Cpu.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8Cpu.cs) with nested `switch` statements on opcode patterns.  
This split works for emulation, but assembler/disassembler features need a single source of truth for:
- matching patterns (`Mask`/`Pattern`)
- operand layout (`OperandPattern`)
- mnemonic text
- binary encode/decode rules

The SDL frontend in [Chip8Emu.SdlHost/Program.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.SdlHost/Program.cs) consumes `Chip8Machine` at a higher boundary and should remain unaffected by the instruction model refactor.  
Tests currently focus heavily on raw field extraction in [InstructionDecoderTests.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Test.Core/InstructionDecoderTests.cs) and CPU behavior in [CpuTests.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Test.Core/CpuTests.cs).

## Scope

**In scope**:
- Introduce `InstructionDefinition` and supporting operand types in `Chip8Emu.Core`.
- Build a canonical CHIP-8 instruction definition table (`Mask`, `Pattern`, mnemonic, encode/decode delegates).
- Refactor decode path to resolve opcode -> definition + decoded operands.
- Refactor CPU dispatch to use resolved instruction identity rather than large opcode switches.
- Update/add tests to validate definition matching and encode/decode round-trips.
- Preserve externally visible machine behavior (`Chip8Machine`, SDL host integration).

**Out of scope**:
- Full assembler parser/lexer implementation.
- Full disassembler text formatting output pipeline.
- SCHIP/XO-CHIP instruction expansion unless explicitly requested.
- Public CLI or file I/O tooling for assembly/disassembly.

## Architecture Decisions

- **Decision**: Add a dedicated instruction metadata layer (`InstructionDefinition` + operand models) in `Chip8Emu.Core.Cpu`.
  **Rationale**: Keeps binary semantics in one place and avoids duplicated opcode logic across emulator, assembler, and disassembler.

- **Decision**: Make `InstructionDefinition` `public`.
  **Rationale**: Assembler/disassembler components will need to consume the same canonical metadata from outside immediate CPU execution internals.

- **Decision**: Implement operand models (`ParsedOperands`, `DecodedOperands`, and supporting operand value types) as immutable records.
  **Rationale**: Immutable records make encode/decode data flow explicit, reduce mutation bugs, and align with value-semantic test assertions.

- **Decision**: Allow configurable mnemonic aliases per opcode variant.
  **Rationale**: Supports historical CHIP-8 naming differences and lets assembler/disassembler consumers choose canonical vs preferred textual forms without changing binary semantics.

- **Decision**: Use a hybrid decode API: non-throwing decode result for tooling plus `DecodeOrThrow` for strict execution paths.
  **Rationale**: Keeps CPU emulation fail-fast while enabling assembler/disassembler and analysis pipelines to continue processing and report invalid opcodes without exception-driven control flow.

- **Decision**: Resolve opcodes by linear pattern match over ordered definitions first; optimize later only if profiling requires.
  **Rationale**: CHIP-8 has a small instruction set, so clarity and maintainability are more valuable than premature indexing complexity.

- **Decision**: Separate parse-time operands (`ParsedOperands`) from decode-time operands (`DecodedOperands`).
  **Rationale**: Assembler inputs and decoded opcode outputs are related but not identical concerns; explicit types avoid accidental mixing.

- **Decision**: Keep CPU instruction handlers (`JP`, `CALL`, `DRW`, etc.) mostly unchanged in the first refactor pass.
  **Rationale**: Reduces risk by changing dispatch/data flow before touching arithmetic/control-flow behavior.

- **Decision**: Migrate tests from “bit-field extraction only” to “definition identity + operand semantics + round-trip” coverage.
  **Rationale**: Better aligns tests with long-term assembler/disassembler goals.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Cpu/InstructionDefinition.cs` | Create new `public sealed record InstructionDefinition(...)` and expose it as part of the reusable instruction metadata API. |
| `Chip8Emu.Core/Cpu/InstructionAliasOptions.cs` | Create alias configuration model that maps opcode variants/definitions to preferred mnemonic labels. |
| `Chip8Emu.Core/Cpu/OperandPattern.cs` | Create operand schema representation used by each instruction definition. |
| `Chip8Emu.Core/Cpu/ParsedOperands.cs` | Create assembler-oriented parsed operand model consumed by `Encode`. |
| `Chip8Emu.Core/Cpu/DecodedOperands.cs` | Create decode result model produced by `Decode`. |
| `Chip8Emu.Core/Cpu/DecodedInstruction.cs` | Create runtime instruction resolution type carrying opcode + `InstructionDefinition` + `DecodedOperands`. |
| `Chip8Emu.Core/Cpu/InstructionDecodeResult.cs` | Create discriminated decode result type (`Valid`/`Invalid`) for non-throwing decode APIs used by tooling. |
| `Chip8Emu.Core/Cpu/Chip8InstructionSet.cs` | Create canonical ordered list of CHIP-8 `InstructionDefinition` entries, alias metadata hooks, and helper lookup APIs. |
| `Chip8Emu.Core/Cpu/InstructionDecoder.cs` | Refactor from nibble extraction-only decoder to definition-based resolver. |
| `Chip8Emu.Core/Cpu/Instruction.cs` | Replace/remove old record struct or repurpose as compatibility wrapper during migration. |
| `Chip8Emu.Core/Cpu/Chip8Cpu.cs` | Refactor dispatch to execute based on matched definition identity/mnemonic, then pass decoded operands into existing handlers. |
| `Chip8Emu.Test.Core/InstructionDecoderTests.cs` | Rework tests to assert definition matching, operand decoding, unknown opcode handling, and round-trip encode/decode cases. |
| `Chip8Emu.Test.Core/CpuTests.cs` | Keep behavior tests; add targeted tests ensuring dispatch still maps to same handler outcomes after definition-based decode. |
| `Chip8Emu.Test.Core/Chip8Emu.Test.Core.csproj` | Only if needed to include new test files for instruction set/round-trip coverage. |

## Execution Steps

1. Create new instruction metadata types (`InstructionDefinition`, `OperandPattern`, `ParsedOperands`, `DecodedOperands`, `DecodedInstruction`) in `Chip8Emu.Core/Cpu` with clear XML docs and invariants.
2. Implement `Chip8InstructionSet` as the canonical ordered definitions list, covering currently supported CHIP-8 instructions already handled in `Chip8Cpu.Execute`.
3. For each definition, wire:
   - `Mnemonic` (for future assembly/disassembly text)
   - alias metadata (variant-friendly mnemonic alternatives)
   - `Mask` + `Pattern` matcher
   - `Operands` descriptor
   - `Encode(ParsedOperands)` and `Decode(ushort)` delegates
4. Refactor `InstructionDecoder` to:
   - iterate definitions in deterministic order
   - find first matching `Mask/Pattern`
   - expose a non-throwing decode API that returns `InstructionDecodeResult` (`Valid`/`Invalid`) for tooling
   - expose `DecodeOrThrow` (or equivalent) for strict CPU execution
5. Add focused tests for instruction-set matching priority edge cases (for example, `00E0`/`00EE` vs generic `0nnn`), and operand extraction correctness per operand pattern.
6. Refactor `Chip8Cpu.Step`/`Execute` to consume `DecodedInstruction` and dispatch by definition identity (or mnemonic) while reusing existing low-level handler methods (`JP`, `CALL`, `SEBYTE`, etc.).
7. Keep the old `Instruction` record temporarily behind an adapter if needed, then remove it once all call sites and tests compile on the new model.
8. Update `InstructionDecoderTests` from pure nibble assertions to semantic assertions:
   - opcode -> expected mnemonic
   - decoded operands correctness
   - encode(decode(opcode)) round-trip equals original opcode for valid patterns
9. Add assembler/disassembler-prep tests:
   - `Encode` validates missing/invalid operands
   - `Decode` returns normalized typed operand values for representative instructions
   - non-throwing decode returns `Invalid` for unknown opcodes with useful diagnostics
   - strict decode path throws for unknown opcodes
   - alias resolution can select configured mnemonic per opcode variant while preserving opcode identity
10. Run full test suite, fix regressions, and do a manual smoke run of `Chip8Emu.SdlHost` ROM loop behavior to confirm no frontend breakage.
11. Document the new source-of-truth instruction pipeline in a short `docs/` note (or project README section) so future assembler/disassembler work builds on the same API.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `InstructionSet_MatchesExpectedDefinition_ForRepresentativeOpcodes`
  - `InstructionSet_RespectsSpecificityOrdering_ForOverlappingPatterns`
  - `InstructionDefinition_DecodeExtractsExpectedOperands`
  - `InstructionDefinition_EncodeProducesExpectedOpcode`
  - `InstructionDefinition_EncodeDecodeRoundTrip_IsStable`
  - `InstructionDecoder_TryDecode_ReturnsInvalid_ForUnknownOpcode`
  - `InstructionDecoder_DecodeOrThrow_Throws_ForUnknownOpcode`
  - Existing `CpuTests` behavior checks continue to pass unchanged.
- Manual checks (for example: API calls, DB queries)
  - Run SDL host and confirm keyboard input, draw loop, and sound timer behavior still operate with no opcode dispatch regressions.

## Open Questions

- None currently.
