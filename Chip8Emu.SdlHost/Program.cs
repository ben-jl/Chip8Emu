using SDL3;

namespace Chip8Emu.SdlHost;

internal static class Program
{
    private const int WindowWidth = 1024;
    private const int WindowHeight = 512;

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

        var running = true;
        while (running)
        {
            while (SDL.PollEvent(out var sdlEvent))
            {
                if((SDL.EventType)sdlEvent.Type == SDL.EventType.Quit)
                {
                    running = false;
                }

                SDL.SetRenderDrawColor(renderer, 24, 32, 48, 255);
                SDL.RenderClear(renderer);
                SDL.RenderPresent(renderer);

                SDL.Delay(16); // Roughly 60 FPS
            }
        }

        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }
}