using Chip8Emu.Core.Assembly;
using Chip8Emu.Core.Assembly.Codegen;
using Chip8Emu.Core.Machine;

namespace Chip8Emu.Test.Core.Assembly
{
    public class ChipAssemblerIntegrationTests
    {
        private static AssemblyResult Assemble(string source, ushort start = 0x200)
        {
            return new ChipAssembler().Assemble(source, start);
        }

        private static ushort OpAt(byte[] bytecode, int byteOffset) =>
            (ushort)((bytecode[byteOffset] << 8) | bytecode[byteOffset + 1]);

        // ---- No-operand instructions ----

        [Fact]
        public void Assemble_CLS_Produces_00E0()
        {
            var result = Assemble("CLS");

            Assert.True(result.Success);
            Assert.Equal(0x00E0, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_RET_Produces_00EE()
        {
            var result = Assemble("RET");

            Assert.True(result.Success);
            Assert.Equal(0x00EE, OpAt(result.Bytecode!, 0));
        }

        // ---- Address instructions ----

        [Fact]
        public void Assemble_JP_Address_Produces_1NNN()
        {
            var result = Assemble("JP 0x300");

            Assert.True(result.Success);
            Assert.Equal(0x1300, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_CALL_Address_Produces_2NNN()
        {
            var result = Assemble("CALL 0x234");

            Assert.True(result.Success);
            Assert.Equal(0x2234, OpAt(result.Bytecode!, 0));
        }

        // ---- Register + byte instructions ----

        [Fact]
        public void Assemble_LD_Vx_Byte_Produces_6XNN()
        {
            var result = Assemble("LD V5, 0x42");

            Assert.True(result.Success);
            Assert.Equal(0x6542, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_ADD_Vx_Byte_Produces_7XNN()
        {
            var result = Assemble("ADD VB, 0x05");

            Assert.True(result.Success);
            Assert.Equal(0x7B05, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_SE_Vx_Byte_Produces_3XNN()
        {
            var result = Assemble("SE V3, 0x10");

            Assert.True(result.Success);
            Assert.Equal(0x3310, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_SNE_Vx_Byte_Produces_4XNN()
        {
            var result = Assemble("SNE V0, 0xFF");

            Assert.True(result.Success);
            Assert.Equal(0x40FF, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_RND_Vx_Byte_Produces_CXNN()
        {
            var result = Assemble("RND V2, 0x0F");

            Assert.True(result.Success);
            Assert.Equal(0xC20F, OpAt(result.Bytecode!, 0));
        }

        // ---- Register + register instructions ----

        [Fact]
        public void Assemble_OR_Vx_Vy_Produces_8XY1()
        {
            var result = Assemble("OR V1, V2");

            Assert.True(result.Success);
            Assert.Equal(0x8121, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_AND_Vx_Vy_Produces_8XY2()
        {
            var result = Assemble("AND V3, V4");

            Assert.True(result.Success);
            Assert.Equal(0x8342, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_XOR_Vx_Vy_Produces_8XY3()
        {
            var result = Assemble("XOR V0, VF");

            Assert.True(result.Success);
            Assert.Equal(0x80F3, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_ADD_Vx_Vy_Produces_8XY4()
        {
            var result = Assemble("ADD V1, V2");

            Assert.True(result.Success);
            Assert.Equal(0x8124, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_SUB_Vx_Vy_Produces_8XY5()
        {
            var result = Assemble("SUB V5, V6");

            Assert.True(result.Success);
            Assert.Equal(0x8565, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_SE_Vx_Vy_Produces_5XY0()
        {
            var result = Assemble("SE V7, V8");

            Assert.True(result.Success);
            Assert.Equal(0x5780, OpAt(result.Bytecode!, 0));
        }

        // ---- Three-operand instruction ----

        [Fact]
        public void Assemble_DRW_Vx_Vy_N_Produces_DXYN()
        {
            var result = Assemble("DRW V0, V1, 5");

            Assert.True(result.Success);
            Assert.Equal(0xD015, OpAt(result.Bytecode!, 0));
        }

        // ---- Register-only instructions ----

        [Fact]
        public void Assemble_SKP_Vx_Produces_EX9E()
        {
            var result = Assemble("SKP VA");

            Assert.True(result.Success);
            Assert.Equal(0xEA9E, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_SKNP_Vx_Produces_EXA1()
        {
            var result = Assemble("SKNP V4");

            Assert.True(result.Success);
            Assert.Equal(0xE4A1, OpAt(result.Bytecode!, 0));
        }

        // ---- Special LD variants ----

        [Fact]
        public void Assemble_LD_I_Address_Produces_ANNN()
        {
            var result = Assemble("LD I, 0x300");

            Assert.True(result.Success);
            Assert.Equal(0xA300, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LD_Vx_DT_Produces_FX07()
        {
            var result = Assemble("LD V2, DT");

            Assert.True(result.Success);
            Assert.Equal(0xF207, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LD_Vx_K_Produces_FX0A()
        {
            var result = Assemble("LD V3, K");

            Assert.True(result.Success);
            Assert.Equal(0xF30A, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LD_DT_Vx_Produces_FX15()
        {
            var result = Assemble("LD DT, V1");

            Assert.True(result.Success);
            Assert.Equal(0xF115, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LD_ST_Vx_Produces_FX18()
        {
            var result = Assemble("LD ST, V5");

            Assert.True(result.Success);
            Assert.Equal(0xF518, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_ADD_I_Vx_Produces_FX1E()
        {
            var result = Assemble("ADD I, V4");

            Assert.True(result.Success);
            Assert.Equal(0xF41E, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LD_F_Vx_Produces_FX29()
        {
            var result = Assemble("LD F, V0");

            Assert.True(result.Success);
            Assert.Equal(0xF029, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LD_B_Vx_Produces_FX33()
        {
            var result = Assemble("LD B, V9");

            Assert.True(result.Success);
            Assert.Equal(0xF933, OpAt(result.Bytecode!, 0));
        }

        // ---- Labels & symbol resolution ----

        [Fact]
        public void Assemble_BackwardJumpToLabel_ResolvesCorrectAddress()
        {
            var source = """
                ORG 0x200
                LOOP:
                    CLS
                    JP LOOP
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            // LOOP is at 0x200, JP LOOP should produce 0x1200
            Assert.Equal(0x1200, OpAt(result.Bytecode!, 2));
        }

        [Fact]
        public void Assemble_ForwardJumpToLabel_ResolvesCorrectAddress()
        {
            var source = """
                ORG 0x200
                    JP END
                    CLS
                END:
                    RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            // JP END: END is at 0x204 (after JP + CLS), so opcode = 0x1204
            Assert.Equal(0x1204, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_LabelUsedInLD_ResolvesAddress()
        {
            var source = """
                ORG 0x200
                SPRITE:
                    LD I, SPRITE
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            // SPRITE is at 0x200, LD I, 0x200 = 0xA200
            Assert.Equal(0xA200, OpAt(result.Bytecode!, 0));
        }

        // ---- DEFINE directive ----

        [Fact]
        public void Assemble_DefineConstant_UsedInInstruction()
        {
            var source = """
                DEFINE SPEED 0x05
                ADD V0, SPEED
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            // ADD V0, 5 → 0x7005
            Assert.Equal(0x7005, OpAt(result.Bytecode!, 0));
        }

        [Fact]
        public void Assemble_DefineConstant_DecimalValue()
        {
            var source = """
                DEFINE COUNT 10
                LD V1, COUNT
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            // LD V1, 10 → 0x610A
            Assert.Equal(0x610A, OpAt(result.Bytecode!, 0));
        }

        // ---- ORG directive ----

        [Fact]
        public void Assemble_Org_SetsStartAddressInResult()
        {
            var result = Assemble("ORG 0x300\nCLS");

            Assert.True(result.Success);
            Assert.Equal(0x300, result.StartAddress);
        }

        [Fact]
        public void Assemble_OrgAffectsLabelAddresses()
        {
            var source = """
                ORG 0x400
                TARGET:
                    JP TARGET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            Assert.Equal(0x1400, OpAt(result.Bytecode!, 0));
        }

        // ---- Multi-instruction programs ----

        [Fact]
        public void Assemble_MultipleInstructions_ProducesCorrectByteLength()
        {
            var source = """
                CLS
                LD V0, 0x01
                LD V1, 0x02
                ADD V0, V1
                RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            Assert.Equal(10, result.Bytecode!.Length);
        }

        [Fact]
        public void Assemble_EmptyProgram_ProducesEmptyBytecode()
        {
            var result = Assemble("");

            Assert.True(result.Success);
            Assert.NotNull(result.Bytecode);
            Assert.Empty(result.Bytecode);
        }

        // ---- Error cases ----

        [Fact]
        public void Assemble_UnknownMnemonic_ReportsError()
        {
            var result = Assemble("BOGUS V0, 0x10");

            Assert.False(result.Success);
            Assert.True(result.Diagnostics.HasErrors);
            Assert.Contains(result.Diagnostics.Errors, e => e.Code == "UNKNOWN_MNEMONIC");
        }

        [Fact]
        public void Assemble_UndefinedLabel_ReportsError()
        {
            var result = Assemble("JP NOWHERE");

            Assert.False(result.Success);
            Assert.True(result.Diagnostics.HasErrors);
            Assert.Contains(result.Diagnostics.Errors, e => e.Code == "UNDEFINED_SYMBOL");
        }

        [Fact]
        public void Assemble_ErrorsContainLineNumber()
        {
            var result = Assemble("CLS\nBOGUS\nRET");

            var error = Assert.Single(result.Diagnostics.Errors);
            Assert.Equal(2, error.LineNumber);
        }

        // ---- End-to-end: Assembly → Machine Execution ----

        [Fact]
        public void EndToEnd_SimpleLoadAndAdd_ExecutesCorrectly()
        {
            var source = """
                LD V0, 0x05
                LD V1, 0x03
                ADD V0, V1
                RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom(result.Bytecode!);

            var snapshot1 = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x200, snapshot1.Cpu.PC);

            machine.StepInstruction(); // LD V0, 0x05
            var snapshot2 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x05, snapshot2.Cpu.V[0]);
            Assert.Equal((ushort)0x202, snapshot2.Cpu.PC);

            machine.StepInstruction(); // LD V1, 0x03
            var snapshot3 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x03, snapshot3.Cpu.V[1]);
            Assert.Equal((ushort)0x204, snapshot3.Cpu.PC);

            machine.StepInstruction(); // ADD V0, V1
            var snapshot4 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x08, snapshot4.Cpu.V[0]); // 5 + 3 = 8
            Assert.Equal((ushort)0x206, snapshot4.Cpu.PC);
        }

        [Fact]
        public void EndToEnd_LabelJumps_ExecutesCorrectly()
        {
            var source = """
                ORG 0x200
                LD V0, 0x00
                JP SKIP
                LD V0, 0xFF
                SKIP:
                LD V1, 0x42
                RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom(result.Bytecode!);

            machine.StepInstruction(); // LD V0, 0x00
            var snapshot1 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x00, snapshot1.Cpu.V[0]);

            machine.StepInstruction(); // JP SKIP (jumps to 0x206)
            var snapshot2 = machine.CurrentSnapshot();
            Assert.Equal((ushort)0x206, snapshot2.Cpu.PC); // Should skip the LD V0, 0xFF

            machine.StepInstruction(); // LD V1, 0x42
            var snapshot3 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x42, snapshot3.Cpu.V[1]);
            Assert.Equal((byte)0x00, snapshot3.Cpu.V[0]); // V0 should still be 0x00 (not 0xFF)
        }

        [Fact]
        public void EndToEnd_DefinesAndRegisters_ExecutesCorrectly()
        {
            var source = """
                DEFINE MAGIC_NUMBER 0x7F
                LD V5, MAGIC_NUMBER
                LD V6, MAGIC_NUMBER
                ADD V5, V6
                RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom(result.Bytecode!);

            machine.StepInstruction(); // LD V5, 0x7F
            var snapshot1 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x7F, snapshot1.Cpu.V[5]);

            machine.StepInstruction(); // LD V6, 0x7F
            var snapshot2 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x7F, snapshot2.Cpu.V[6]);

            machine.StepInstruction(); // ADD V5, V6
            var snapshot3 = machine.CurrentSnapshot();
            Assert.Equal((byte)0xFE, snapshot3.Cpu.V[5]); // 0x7F + 0x7F = 0xFE
        }

        [Fact]
        public void EndToEnd_DrawOperations_UpdatesDisplay()
        {
            var source = """
                ORG 0x200
                LD V0, 0x00
                LD V1, 0x00
                LD I, 0x200
                DRW V0, V1, 5
                RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom(result.Bytecode!);

            // Display should be blank initially
            var initialBuffer = machine.Display.Buffer.ToArray();
            Assert.True(initialBuffer.All(pixel => pixel == 0));

            machine.StepInstruction(); // LD V0, 0x00
            machine.StepInstruction(); // LD V1, 0x00
            machine.StepInstruction(); // LD I, 0x200
            machine.StepInstruction(); // DRW V0, V1, 5

            var newBuffer = machine.Display.Buffer.ToArray();
            // Display should have been modified (sprite drawn at position 0,0)
            // At least some pixels should be set
            Assert.Contains((byte)1, newBuffer);
        }

        [Fact]
        public void EndToEnd_RegisterOperations_ExecuteCorrectly()
        {
            var source = """
                LD V0, 0x0F
                LD V1, 0x01
                OR V0, V1
                AND V0, V1
                XOR V0, V1
                RET
                """;

            var result = Assemble(source);

            Assert.True(result.Success);
            var machine = new Chip8Machine(new EmulationOptions { RandomSeed = 1234 });
            machine.LoadRom(result.Bytecode!);

            machine.StepInstruction(); // LD V0, 0x0F
            var snap1 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x0F, snap1.Cpu.V[0]);

            machine.StepInstruction(); // LD V1, 0x01
            var snap2 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x01, snap2.Cpu.V[1]);

            machine.StepInstruction(); // OR V0, V1 -> 0x0F | 0x01 = 0x0F
            var snap3 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x0F, snap3.Cpu.V[0]);

            machine.StepInstruction(); // AND V0, V1 -> 0x0F & 0x01 = 0x01
            var snap4 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x01, snap4.Cpu.V[0]);

            machine.StepInstruction(); // XOR V0, V1 -> 0x01 ^ 0x01 = 0x00
            var snap5 = machine.CurrentSnapshot();
            Assert.Equal((byte)0x00, snap5.Cpu.V[0]);
        }
    }
}
