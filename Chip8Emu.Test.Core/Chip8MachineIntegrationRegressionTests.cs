using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core
{
    public class Chip8MachineIntegrationRegressionTests
    {
        [Fact]
        public void NewMachine_ShouldStartAtRomStartWithClearStateAndLoadedFont()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            var snapshot = machine.CurrentSnapshot();

            Assert.Equal((ushort)0x200, snapshot.Cpu.PC);
            Assert.Equal((ushort)0x000, snapshot.Cpu.I);
            Assert.Equal((byte)0, snapshot.DelayTimer);
            Assert.Equal((byte)0, snapshot.SoundTimer);
            Assert.False(machine.SoundEnabled);
            Assert.All(snapshot.Cpu.V, value => Assert.Equal((byte)0, value));
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));

            machine.LoadRom([
                0x60, 0x0A, // LD V0, 0x0A
                0xF0, 0x29  // LD F, V0
            ]);
            machine.StepInstruction();
            machine.StepInstruction();

            Assert.Equal((ushort)0x32, machine.CurrentSnapshot().Cpu.I);
        }

        [Fact]
        public void LoadRom_ShouldResetCpuTimersDisplayAndKeypadBeforeLoadingProgram()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                RandomSeed = 1234
            });

            machine.SetKeyState(0x1, true);
            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0x61, 0x00, // LD V1, 0x00
                0xF0, 0x15, // LD DT, V0
                0xD0, 0x15  // DRW V0, V1, 5
            ]);
            for (var i = 0; i < 4; i++)
            {
                machine.StepInstruction();
            }

            Assert.Contains((byte)1, machine.Display.Buffer.ToArray());
            Assert.Equal((byte)1, machine.CurrentSnapshot().DelayTimer);

            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0xE0, 0x9E, // SKP V0
                0x61, 0x2A  // LD V1, 0x2A
            ]);
            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0, snapshot.DelayTimer);
            Assert.Equal((byte)0, snapshot.SoundTimer);
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));
            Assert.Equal((byte)0x2A, snapshot.Cpu.V[1]);
            Assert.Equal((ushort)0x206, snapshot.Cpu.PC);
        }

        [Fact]
        public void LoadRom_ShouldAcceptLargestRomThatFitsAndThrowForOversizedRom()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            var largestFittingRom = new byte[4096 - 0x200];
            largestFittingRom[0] = 0x60;
            largestFittingRom[1] = 0x2A;

            machine.LoadRom(largestFittingRom);
            machine.StepInstruction();

            Assert.Equal((byte)0x2A, machine.CurrentSnapshot().Cpu.V[0]);
            Assert.Throws<IndexOutOfRangeException>(() => machine.LoadRom(new byte[4096 - 0x200 + 1]));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void StepFrame_ShouldTreatNonPositiveInstructionBudgetsAsOneInstruction(int instructionsPerFrame)
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = instructionsPerFrame,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x01, // LD V0, 0x01
                0x61, 0x02  // LD V1, 0x02
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x01, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
            Assert.Equal((ushort)0x202, snapshot.Cpu.PC);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void StepInstruction_ShouldTreatNonPositiveTimerCadenceAsOneInstruction(int instructionsPerFrame)
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = instructionsPerFrame,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x02, // LD V0, 0x02
                0xF0, 0x15  // LD DT, V0
            ]);

            machine.StepInstruction();
            machine.StepInstruction();

            Assert.Equal((byte)0x01, machine.CurrentSnapshot().DelayTimer);
        }

        [Fact]
        public void StepFrame_ShouldDecrementTimersAfterExecutingTheFrameBudget()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 2,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x03, // LD V0, 0x03
                0xF0, 0x15, // LD DT, V0
                0x61, 0x01  // LD V1, 0x01
            ]);

            machine.StepFrame();
            Assert.Equal((byte)0x02, machine.CurrentSnapshot().DelayTimer);

            machine.StepFrame();
            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x01, snapshot.DelayTimer);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[1]);
        }

        [Fact]
        public void StepInstruction_ShouldContinueTickingTimersWhileWaitingForKey()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 1,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x03, // LD V0, 0x03
                0xF0, 0x15, // LD DT, V0
                0xF1, 0x0A  // LD V1, K
            ]);

            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x204, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.DelayTimer);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
        }

        [Fact]
        public void WaitForKey_ShouldStoreLowestPressedKeyAndAdvanceWhenKeyIsAvailable()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            machine.LoadRom([
                0xF2, 0x0A, // LD V2, K
                0x63, 0x99  // LD V3, 0x99
            ]);
            machine.SetKeyState(0xC, true);
            machine.SetKeyState(0x4, true);

            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x04, snapshot.Cpu.V[2]);
            Assert.Equal((byte)0x99, snapshot.Cpu.V[3]);
            Assert.Equal((ushort)0x204, snapshot.Cpu.PC);
        }

        [Fact]
        public void DisplayWaitOnDraw_ShouldOnlyAffectStepFrameAndAllowTheBlockedDrawNextFrame()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                DisplayWaitOnDraw = true,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0xD0, 0x05, // DRW V0, V0, 5
                0x61, 0x01, // LD V1, 0x01
                0xD0, 0x05, // DRW V0, V0, 5
                0x62, 0x02, // LD V2, 0x02
                0x12, 0x08  // JP 0x208
            ]);

            machine.StepFrame();
            var firstFrame = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x204, firstFrame.Cpu.PC);
            Assert.Equal((byte)0x01, firstFrame.Cpu.V[1]);
            Assert.Equal((byte)0x00, firstFrame.Cpu.V[2]);

            machine.StepFrame();
            var secondFrame = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x208, secondFrame.Cpu.PC);
            Assert.Equal((byte)0x02, secondFrame.Cpu.V[2]);
        }

        [Fact]
        public void DisplayWaitOnDraw_ShouldNotAffectStepInstruction()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                DisplayWaitOnDraw = true,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0xD0, 0x05, // DRW V0, V0, 5
                0xD0, 0x05, // DRW V0, V0, 5
                0x61, 0x01  // LD V1, 0x01
            ]);

            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x206, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[1]);
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));
        }

        [Fact]
        public void DisplayWaitOnDraw_ShouldStillBlockSecondDrawAfterClearInSameFrame()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                DisplayWaitOnDraw = true,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0xD0, 0x05, // DRW V0, V0, 5
                0x00, 0xE0, // CLS
                0xD0, 0x05, // DRW V0, V0, 5
                0x61, 0x01  // LD V1, 0x01
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x204, snapshot.Cpu.PC);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[1]);
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));
        }

        [Fact]
        public void LowAndHighInstructions_ShouldSwitchDisplayResolutionAndClearDisplay()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            machine.LoadRom([
                0xD0, 0x05, // DRW V0, V0, 5
                0x00, 0xFF, // HIGH
                0x00, 0xFE  // LOW
            ]);

            machine.StepInstruction();
            Assert.Contains((byte)1, machine.Display.Buffer.ToArray());

            machine.StepInstruction();
            Assert.Equal(128, machine.Display.Width);
            Assert.Equal(64, machine.Display.Height);
            Assert.Equal(128 * 64, machine.Display.Buffer.Length);
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));

            machine.StepInstruction();
            Assert.Equal(64, machine.Display.Width);
            Assert.Equal(32, machine.Display.Height);
            Assert.Equal(64 * 32, machine.Display.Buffer.Length);
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));
        }

        [Fact]
        public void HighResolutionDrawWithZeroHeight_ShouldDrawAndCollideAs16By16Sprite()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            machine.LoadRom([
                0x00, 0xFF, // HIGH
                0xA2, 0x08, // LD I, 0x208
                0xD0, 0x00, // DRW V0, V0, 0
                0xD0, 0x00, // DRW V0, V0, 0
                0x80, 0x01, // row 0: x 0 and x 15
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x40, 0x02  // row 15: x 1 and x 14
            ]);

            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var afterFirstDraw = machine.CurrentSnapshot();
            Assert.Equal((byte)0, afterFirstDraw.Cpu.V[0xF]);
            Assert.Equal((byte)1, machine.Display.Buffer[0]);
            Assert.Equal((byte)1, machine.Display.Buffer[15]);
            Assert.Equal((byte)1, machine.Display.Buffer[(15 * 128) + 1]);
            Assert.Equal((byte)1, machine.Display.Buffer[(15 * 128) + 14]);

            machine.StepInstruction();

            var afterSecondDraw = machine.CurrentSnapshot();
            Assert.Equal((byte)1, afterSecondDraw.Cpu.V[0xF]);
            Assert.Equal((byte)0, machine.Display.Buffer[0]);
            Assert.Equal((byte)0, machine.Display.Buffer[15]);
            Assert.Equal((byte)0, machine.Display.Buffer[(15 * 128) + 1]);
            Assert.Equal((byte)0, machine.Display.Buffer[(15 * 128) + 14]);
        }

        [Fact]
        public void DisplayWaitScope_ShouldBlockHighResolutionDraws_WhenAllDisplayModesAreSelected()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                DisplayWaitOnDraw = true,
                DisplayWaitScope = DisplayWaitScope.AllDisplayModes,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x00, 0xFF, // HIGH
                0xA2, 0x10, // LD I, 0x210
                0xD0, 0x00, // DRW V0, V0, 0
                0x61, 0x01, // LD V1, 0x01
                0xD0, 0x00, // DRW V0, V0, 0
                0x62, 0x02, // LD V2, 0x02
                0x12, 0x0C, // JP 0x20C
                0x00, 0x00,
                0x80, 0x00
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x208, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[1]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[2]);
            Assert.Equal(128, machine.Display.Width);
            Assert.Equal(64, machine.Display.Height);
        }

        [Fact]
        public void DisplayWaitScope_ShouldNotBlockHighResolutionDraws_WhenOnlyLowResolutionIsSelected()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 6,
                DisplayWaitOnDraw = true,
                DisplayWaitScope = DisplayWaitScope.LowResolutionOnly,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x00, 0xFF, // HIGH
                0xA2, 0x10, // LD I, 0x210
                0xD0, 0x00, // DRW V0, V0, 0
                0x61, 0x01, // LD V1, 0x01
                0xD0, 0x00, // DRW V0, V0, 0
                0x62, 0x02, // LD V2, 0x02
                0x12, 0x0C, // JP 0x20C
                0x00, 0x00,
                0x80, 0x00
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x20C, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[1]);
            Assert.Equal((byte)0x02, snapshot.Cpu.V[2]);
            Assert.Equal(128, machine.Display.Width);
            Assert.Equal(64, machine.Display.Height);
        }

        [Fact]
        public void DisplayWaitScope_ShouldNotBlockLowResolutionDraws_WhenOnlyHighResolutionIsSelected()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 6,
                DisplayWaitOnDraw = true,
                DisplayWaitScope = DisplayWaitScope.HighResolutionOnly,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0xD0, 0x05, // DRW V0, V0, 5
                0x61, 0x01, // LD V1, 0x01
                0xD0, 0x05, // DRW V0, V0, 5
                0x62, 0x02, // LD V2, 0x02
                0x12, 0x08  // JP 0x208
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x208, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[1]);
            Assert.Equal((byte)0x02, snapshot.Cpu.V[2]);
            Assert.Equal(64, machine.Display.Width);
            Assert.Equal(32, machine.Display.Height);
        }

        [Fact]
        public void DisplayWaitScope_ShouldBlockHighResolutionDraws_WhenOnlyHighResolutionIsSelected()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 10,
                DisplayWaitOnDraw = true,
                DisplayWaitScope = DisplayWaitScope.HighResolutionOnly,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x00, 0xFF, // HIGH
                0xA2, 0x10, // LD I, 0x210
                0xD0, 0x00, // DRW V0, V0, 0
                0x61, 0x01, // LD V1, 0x01
                0xD0, 0x00, // DRW V0, V0, 0
                0x62, 0x02, // LD V2, 0x02
                0x12, 0x0C, // JP 0x20C
                0x00, 0x00,
                0x80, 0x00
            ]);

            machine.StepFrame();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x208, snapshot.Cpu.PC);
            Assert.Equal((byte)0x01, snapshot.Cpu.V[1]);
            Assert.Equal((byte)0x00, snapshot.Cpu.V[2]);
            Assert.Equal(128, machine.Display.Width);
            Assert.Equal(64, machine.Display.Height);
        }

        [Fact]
        public void SoundEnabled_ShouldTrackCurrentSoundTimerValue()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 2,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x02, // LD V0, 0x02
                0xF0, 0x18  // LD ST, V0
            ]);

            machine.StepFrame();
            Assert.True(machine.SoundEnabled);
            Assert.Equal((byte)0x01, machine.CurrentSnapshot().SoundTimer);

            machine.StepFrame();
            Assert.False(machine.SoundEnabled);
            Assert.Equal((byte)0x00, machine.CurrentSnapshot().SoundTimer);
        }

        [Fact]
        public void DrawWithZeroHeight_ShouldClearVfAndLeaveDisplayUnchanged()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            machine.LoadRom([
                0x6F, 0x01, // LD VF, 0x01
                0xD0, 0x00  // DRW V0, V0, 0
            ]);

            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x00, snapshot.Cpu.V[0xF]);
            Assert.True(machine.Display.Buffer.ToArray().All(pixel => pixel == 0));
            Assert.Equal((ushort)0x204, snapshot.Cpu.PC);
        }

        [Fact]
        public void LoadFont_ShouldCalculateAddressForFullByteRegisterValueWithoutValidation()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            machine.LoadRom([
                0x60, 0x10, // LD V0, 0x10
                0xF0, 0x29  // LD F, V0
            ]);

            machine.StepInstruction();
            machine.StepInstruction();

            Assert.Equal((ushort)0x50, machine.CurrentSnapshot().Cpu.I);
        }

        [Fact]
        public void RandomSequenceAndCount_ShouldContinueAcrossResetAndLoadRom()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });

            machine.LoadRom([
                0xC0, 0xFF // RND V0, 0xFF
            ]);
            machine.StepInstruction();
            var firstSnapshot = machine.CurrentSnapshot();

            machine.LoadRom([
                0xC1, 0xFF // RND V1, 0xFF
            ]);
            machine.StepInstruction();
            var secondSnapshot = machine.CurrentSnapshot();

            var comparison = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            comparison.LoadRom([
                0xC0, 0xFF, // RND V0, 0xFF
                0xC1, 0xFF  // RND V1, 0xFF
            ]);
            comparison.StepInstruction();
            comparison.StepInstruction();
            var comparisonSnapshot = comparison.CurrentSnapshot();

            Assert.Equal(1, firstSnapshot.Cpu.RandomCount);
            Assert.Equal(2, secondSnapshot.Cpu.RandomCount);
            Assert.Equal(comparisonSnapshot.Cpu.V[1], secondSnapshot.Cpu.V[1]);
        }

        [Fact]
        public void CurrentSnapshot_ShouldReturnRegisterCopy()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0x60, 0x2A // LD V0, 0x2A
            ]);
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            snapshot.Cpu.V[0] = 0x00;

            Assert.Equal((byte)0x2A, machine.CurrentSnapshot().Cpu.V[0]);
        }

        [Fact]
        public void StepInstruction_ShouldThrowForUnknownOpcodeWithoutAdvancingProgramCounter()
        {
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom([
                0xF0, 0xF0
            ]);

            var exception = Assert.Throws<InvalidOperationException>(() => machine.StepInstruction());

            Assert.Contains("Unknown opcode", exception.Message);
            Assert.Equal((ushort)0x200, machine.CurrentSnapshot().Cpu.PC);
        }

        [Fact]
        public void JumpWithV0Option_ShouldUseV0ForBnnnWhenEnabled()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                JumpWithV0 = true,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x04, // LD V0, 0x04
                0xB2, 0x08, // JP V0, 0x208
                0x61, 0xEE, // skipped
                0x61, 0xEE, // skipped
                0x61, 0xEE, // skipped
                0x61, 0xEE, // skipped
                0x61, 0x42  // target at 0x20C
            ]);

            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x42, snapshot.Cpu.V[1]);
            Assert.Equal((ushort)0x20E, snapshot.Cpu.PC);
        }

        [Fact]
        public void JumpWithV0Option_ShouldUseVxFromAddressHighNibbleForBnnnWhenDisabled()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                JumpWithV0 = false,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x60, 0x04, // LD V0, 0x04
                0x62, 0x08, // LD V2, 0x08
                0xB2, 0x04, // JP 0x204 + V2
                0x61, 0xEE, // skipped
                0x61, 0xEE, // skipped
                0x61, 0xEE, // skipped
                0x61, 0x42  // target at 0x20C
            ]);

            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();
            machine.StepInstruction();

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x42, snapshot.Cpu.V[1]);
            Assert.Equal((ushort)0x20E, snapshot.Cpu.PC);
        }

        [Fact]
        public void MultiInstructionProgram_ShouldCoordinateCallReturnDisplayTimerAndArithmetic()
        {
            var machine = new Chip8Machine(new EmulationOptions
            {
                InstructionsPerFrame = 100,
                RandomSeed = 1234
            });

            machine.LoadRom([
                0x22, 0x0A, // 0x200: CALL 0x20A
                0x70, 0x01, // 0x202: ADD V0, 0x01
                0xF0, 0x15, // 0x204: LD DT, V0
                0xF0, 0x07, // 0x206: LD V0, DT
                0x12, 0x08, // 0x208: JP 0x208
                0x60, 0x03, // 0x20A: LD V0, 0x03
                0x61, 0x00, // 0x20C: LD V1, 0x00
                0xA0, 0x00, // 0x20E: LD I, 0x000
                0xD0, 0x15, // 0x210: DRW V0, V1, 5
                0x00, 0xEE  // 0x212: RET
            ]);

            for (var i = 0; i < 9; i++)
            {
                machine.StepInstruction();
            }

            var snapshot = machine.CurrentSnapshot();
            Assert.Equal((byte)0x04, snapshot.Cpu.V[0]);
            Assert.Equal((byte)0x04, snapshot.DelayTimer);
            Assert.Equal((ushort)0x208, snapshot.Cpu.PC);
            Assert.Equal((byte)1, machine.Display.Buffer[3]);
            Assert.Equal((byte)1, machine.Display.Buffer[4]);
            Assert.Equal((byte)1, machine.Display.Buffer[5]);
            Assert.Equal((byte)1, machine.Display.Buffer[6]);
        }
    }
}
