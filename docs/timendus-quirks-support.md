# Timendus quirks ROM support notes

This note captures the behavior modeled from `C:\Users\blevalley\Downloads\5-quirks.ch8`
and the Timendus `5-quirks.8o` source so later changes can reason from the same map.

## Result bytes

The ROM stores selected platform and early quirk probe results in its scratchpad:

- `scratchpad + 0`: selected platform (`1` CHIP-8, `2` Super-CHIP modern, `3` XO-CHIP, `4` Super-CHIP legacy).
- `scratchpad + 1`: display-wait result.
- `scratchpad + 2`: clipping/wrapping result.

The result values used by the ROM are:

- `0`: OFF
- `1`: ON
- `2`: BOTH
- `3`: NONE
- `4`: LORES
- `5`: HIRES
- `6`: ERR1/SLOW depending on row
- `7`: ERR2
- `8`: ERR3

## Emulator option mapping

The quirk rows in the Timendus ROM map to API options as follows:

| Timendus row | Emulation option |
| --- | --- |
| `VF RESET` | `ResetCarryFlagOnBitwiseOps` |
| `MEMORY` | `IncrementIOnStoreLoadMemoryOps` |
| `DISP.WAIT` | `DisplayWaitOnDraw` plus `DisplayWaitScope` |
| `CLIPPING` | `ClipSprites` |
| `SHIFTING` | `ShiftUsesVy` |
| `JUMPING` | `JumpWithV0` |

The display mode opcodes used by the Super-CHIP and XO-CHIP paths are supported directly:

- `00FE` (`LOW`) switches to 64x32.
- `00FF` (`HIGH`) switches to 128x64.
- `DXY0` draws a 16x16 sprite only while the display is 128x64; in 64x32 mode it keeps the current zero-height no-op behavior.

## Timendus-compatible profiles

These are not hard-coded presets. They are option combinations that match the ROM's expected rows:

| Target | Options |
| --- | --- |
| CHIP-8 | `ResetCarryFlagOnBitwiseOps=true`, `IncrementIOnStoreLoadMemoryOps=true`, `DisplayWaitOnDraw=true`, `DisplayWaitScope=AllDisplayModes`, `ClipSprites=true`, `ShiftUsesVy=true`, `JumpWithV0=true` |
| Super-CHIP modern | `ResetCarryFlagOnBitwiseOps=false`, `IncrementIOnStoreLoadMemoryOps=false`, `DisplayWaitOnDraw=false`, `ClipSprites=true`, `ShiftUsesVy=false`, `JumpWithV0=false` |
| Super-CHIP legacy | `ResetCarryFlagOnBitwiseOps=false`, `IncrementIOnStoreLoadMemoryOps=false`, `DisplayWaitOnDraw=true`, `DisplayWaitScope=LowResolutionOnly`, `ClipSprites=true`, `ShiftUsesVy=false`, `JumpWithV0=false` |
| XO-CHIP | `ResetCarryFlagOnBitwiseOps=false`, `IncrementIOnStoreLoadMemoryOps=true`, `DisplayWaitOnDraw=false`, `ClipSprites=false`, `ShiftUsesVy=true`, `JumpWithV0=true` |

## Regression anchors

The behavior above is covered by the core test suite. The most relevant tests are:

- `CpuTests` for `BNNN` base-register behavior.
- `Chip8MachineIntegrationRegressionTests` for `LOW`, `HIGH`, high-resolution `DXY0`, display-wait scoping, and API-level behavior.
- `InstructionCatalogRegressionTests` and `InstructionDecoderTests` for opcode catalog and decode behavior.
