# Add ROM Disassembler and Runtime Instruction View Model

> **Created**: 20260522_081620
> **Status**: Draft

## Goal

Expose a host-agnostic disassembly model for the loaded CHIP-8 ROM and a current-PC-centered instruction window for live debugging.

## Context

The instruction catalog and decode semantics already live in [Chip8Emu.Core/Cpu/Chip8InstructionSet.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/Chip8InstructionSet.cs) and [Chip8Emu.Core/Cpu/InstructionDecoder.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Cpu/InstructionDecoder.cs).  
`Chip8Machine.LoadRom(...)` currently writes ROM bytes to memory in [Chip8Emu.Core/Machine/Chip8Machine.cs](/C:/Users/blevalley/source/repos/Chip8Emu/Chip8Emu.Core/Machine/Chip8Machine.cs), but there is no public disassembly projection or PC window model for hosts.

## Scope

**In scope**:
- Add a disassembler service for CHIP-8 ROM bytes using existing decode metadata.
- Track loaded ROM byte span and entry address for runtime disassembly boundaries.
- Add query APIs for:
  - full disassembly listing,
  - centered instruction window around current `PC`,
  - unknown-opcode annotations.
- Add tests for disassembly formatting and PC window selection.

**Out of scope**:
- Source-map support, labels, or symbolic references.
- Assembler text parser.
- Multi-variant (SCHIP/XO-CHIP) decode expansion.
- UI panel rendering.

## Architecture Decisions

- **Decision**: Reuse `InstructionDecoder.Decode(...)` (non-throwing) for disassembly.
  **Rationale**: Avoids duplicate decode logic and preserves single opcode truth source.

- **Decision**: Represent disassembled output as structured rows, not plain strings only.
  **Rationale**: Hosts can choose their own rendering style (SDL overlay, terminal, future GUI).

- **Decision**: Keep ROM boundary metadata in machine runtime state (`romStart`, `romLength`).
  **Rationale**: Disassembly should respect loaded content boundaries rather than full memory range.

## Files to Create / Modify

| File | Change |
|------|--------|
| `Chip8Emu.Core/Debugging/Disassembly/DisassembledInstruction.cs` | Add immutable row model (`Address`, `Opcode`, `Mnemonic`, `OperandsText`, `IsValid`). |
| `Chip8Emu.Core/Debugging/Disassembly/DisassemblyWindow.cs` | Add current-PC-centered view model with selected row index. |
| `Chip8Emu.Core/Debugging/Disassembly/IChip8Disassembler.cs` | Add disassembler contract for full listing and window queries. |
| `Chip8Emu.Core/Debugging/Disassembly/Chip8Disassembler.cs` | Implement decode-based ROM disassembly service. |
| `Chip8Emu.Core/Machine/Chip8Machine.cs` | Store last loaded ROM metadata and expose read-only view needed by disassembler integration. |
| `Chip8Emu.Core/Machine/IEmulatorMachine.cs` | Add minimal API surface for debugger consumers to retrieve current ROM/disassembly inputs if needed. |
| `Chip8Emu.Test.Core/DisassemblerTests.cs` | Add tests for valid decode rows, unknown opcode rows, and PC-window centering rules. |

## Execution Steps

1. Create disassembly models (`DisassembledInstruction`, `DisassemblyWindow`) and interface (`IChip8Disassembler`).
2. Add ROM metadata tracking in `Chip8Machine` when `LoadRom` is called (start address and loaded length).
3. Implement `Chip8Disassembler`:
   1. iterate ROM bytes in 2-byte instruction increments,
   2. decode with `InstructionDecoder.Decode`,
   3. produce structured rows with validity/error metadata.
4. Add API for generating a centered window around current `PC` with configurable row radius.
5. Add readable operand formatting from `DecodedOperands` and `OperandPattern` for debugger display.
6. Add tests covering:
   1. canonical instruction rows,
   2. unknown opcode handling,
   3. correct window clamping at ROM start/end.
7. Ensure disassembler output remains deterministic across repeated calls.

## Verification

How to confirm the implementation is correct:
- Build command(s) that must pass with 0 errors/warnings
  - `dotnet build Chip8Emu.slnx`
- Tests to write (with a brief description of what each covers)
  - `Disassembler_ShouldDecodeLoadedRomIntoInstructionRows`
  - `Disassembler_ShouldMarkUnknownOpcodesWithoutThrowing`
  - `DisassemblerWindow_ShouldCenterOnCurrentProgramCounter`
  - `DisassemblerWindow_ShouldClampNearRomBoundaries`
- Manual checks (for example: API calls, DB queries)
  - Load a known ROM in SDL host, pause execution, and print disassembly rows around current `PC` to verify mnemonics/addresses match expected opcodes.

## Open Questions

- None. CHIP-8-only decode scope is fixed for this milestone.

