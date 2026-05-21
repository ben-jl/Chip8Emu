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
        if (!SDL.Init(SDL.InitFlags.Video))
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
        var machine = GetTestMachine();

        var running = true;
        while (running)
        {
            while (SDL.PollEvent(out var sdlEvent))
            {
                if((SDL.EventType)sdlEvent.Type == SDL.EventType.Quit)
                {
                    running = false;
                }

                frameBufferRenderer.Render(
                    machine.Display.Buffer,
                    Chip8Width,
                    Chip8Height);

                SDL.Delay(16); // Roughly 60 FPS
                machine.StepFrame();
            }
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

        machine.LoadRom([
            0x60, 0x01, // LD V0, 1
            0xA0, 0x0A, // LD I, 0x00A (location of the two sprite)
            0xD0, 0x05, // DRW V0, V0, 5 ; Should be the two sprite
            0x12, 0x00  // JP 0x200 ; Loop indefinitely
            ]);

        return machine;
    }
}