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
        var displayBuffer = GetTestDisplayBuffer();

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
                    displayBuffer,
                    Chip8Width,
                    Chip8Height);

                SDL.Delay(16); // Roughly 60 FPS
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
                0x60, 0x01, // LD V0, 1
                0xD0, 0x05 // DRW V0, V0, 5 ; Should be the zero sprite
                ]);

        machine.StepInstruction(); // LD V0, 1
        machine.StepInstruction(); // DRW V0, V0, 5

        return machine.Display.Buffer;
    }
}