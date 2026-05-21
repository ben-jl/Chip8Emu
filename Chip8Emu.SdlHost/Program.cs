using Chip8.SdlHost;
using Chip8Emu.Core.Machine;
using SDL3;
using System.Reflection.PortableExecutable;

namespace Chip8Emu.SdlHost;

internal static class Program
{
    private const int Chip8Width = 64;
    private const int Chip8Height = 32;
    private const int Scale = 16;

    private const int WindowWidth = Chip8Width * Scale;
    private const int WindowHeight = Chip8Height * Scale;

    [STAThread]
    private static void Main()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio))
        {
            Console.WriteLine($"Failed to initialize SDL: {SDL.GetError()}");
            return;
        }

        if (!SDL.CreateWindowAndRenderer(
            "Chip-8 Emulator",
            WindowWidth,
            WindowHeight,
            0,
            out var window,
            out var renderer))
        {
            Console.WriteLine($"Failed to create window and renderer: {SDL.GetError()}");
            SDL.Quit();
            return;
        }

        var frameBufferRenderer = new SdlFramebufferRenderer(renderer, Scale);
        using var audio = new SdlBeepAudio();
        var machine = GetTestMachine();

        var running = true;
        while (running)
        {
            while (SDL.PollEvent(out var sdlEvent))
            {
                var eventType = (SDL.EventType)sdlEvent.Type;

                if (eventType == SDL.EventType.Quit)
                {
                    running = false;
                    continue;
                }

                SdlInput.HandleEvent(sdlEvent, machine);
            }

            machine.StepFrame();

            audio.SetEnabled(machine.SoundEnabled);
            audio.Update();

            frameBufferRenderer.Render(
                machine.Display.Buffer,
                Chip8Width,
                Chip8Height);

            SDL.Delay(16); // Roughly 60 FPS
        }

        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }

    private static ReadOnlySpan<byte> GetTestDisplayBuffer()
    {
        var machine = new Chip8Machine();
        machine.LoadRom([
                
                ]);

        machine.StepInstruction(); // LD V0, 1
        machine.StepInstruction(); // DRW V0, V0, 5

        return machine.Display.Buffer;
    }

    private static Chip8Machine GetTestMachine()
    {
        var machine = new Chip8Machine();

        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\1-chip8-logo (1).ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\2-ibm-logo.ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\3-corax+.ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\4-flags.ch8";
        var testRomPath = "C:\\Users\\blevalley\\Downloads\\5-quirks.ch8";
        var bytes = File.ReadAllBytes(testRomPath);
        machine.LoadRom(bytes);
//        machine.LoadRom([
//            0xF0, 0x0A, // wait for keypress and store in V0
////            0x60, 0x01, // LD V0, 1
//            0xA0, 0x0A, // LD I, 0x00A (location of the two sprite)
//            0xD0, 0x05, // DRW V0, V0, 5 ; Should be the two sprite
//            0x12, 0x04  // JP 0x204 ; Loop indefinitely without redrawing
//            ]);

        return machine;
    }
}