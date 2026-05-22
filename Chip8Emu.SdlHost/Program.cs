using Chip8.SdlHost;
using Chip8Emu.Core.Debugging;
using Chip8Emu.Core.Debugging.Disassembly;
using Chip8Emu.Core.Debugging.Trace;
using Chip8Emu.Core.Machine;
using Chip8Emu.SdlHost.Debug;
using SDL3;

namespace Chip8Emu.SdlHost;

internal static class Program
{
    private const int Chip8Width = 64;
    private const int Chip8Height = 32;
    private const int Scale = 16;
    private const int SidePanelWidth = 340;

    private const int DisplayWidth = Chip8Width * Scale;
    private const int BaseWindowWidth = DisplayWidth;
    private const int ExpandedWindowWidth = DisplayWidth + (SidePanelWidth * 2);
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
            BaseWindowWidth,
            WindowHeight,
            0,
            out var window,
            out var renderer))
        {
            Console.WriteLine($"Failed to create window and renderer: {SDL.GetError()}");
            SDL.Quit();
            return;
        }

        var traceBuffer = new InMemoryTraceBuffer(4000);
        var machine = GetTestMachine(traceBuffer);
        var controller = new EmulatorDebugController(machine);
        var disassembler = new Chip8Disassembler();
        var debugViewState = new SdlDebugViewState();
        var overlayRenderer = new SdlDebugOverlayRenderer(
            renderer,
            ExpandedWindowWidth,
            WindowHeight,
            SidePanelWidth,
            DisplayWidth);
        var frameBufferRenderer = new SdlFramebufferRenderer(renderer, Scale);
        var previousDebugMode = debugViewState.IsDebugInputMode;

        using var audio = new SdlBeepAudio();
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

                var handledByDebug = SdlDebugInputHandler.HandleEvent(
                    sdlEvent,
                    debugViewState,
                    machine,
                    controller);
                if (handledByDebug)
                {
                    continue;
                }

                _ = SdlInput.HandleChip8KeyEvent(sdlEvent, machine);
            }

            if (debugViewState.IsDebugInputMode != previousDebugMode)
            {
                var targetWidth = debugViewState.IsDebugInputMode
                    ? ExpandedWindowWidth
                    : BaseWindowWidth;
                _ = SDL.SetWindowSize(window, targetWidth, WindowHeight);
                previousDebugMode = debugViewState.IsDebugInputMode;
            }

            controller.Update();

            audio.SetEnabled(machine.SoundEnabled);
            audio.Update();

            var displayOriginX = debugViewState.IsDebugInputMode ? SidePanelWidth : 0;
            frameBufferRenderer.Render(
                machine.Display.Buffer,
                machine.Display.Width,
                machine.Display.Height,
                displayOriginX,
                0);

            overlayRenderer.Render(
                debugViewState,
                controller,
                machine,
                disassembler,
                traceBuffer);

            SDL.RenderPresent(renderer);
            SDL.Delay(16); // Roughly 60 FPS
        }

        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }

    private static Chip8Machine GetTestMachine(ITraceSink traceSink)
    {
        var emuOptions = EmulationOptions.CosmacVipChip8;
        //var emuOptions = EmulationOptions.SuperChip8_Modern;
        //var emuOptions = EmulationOptions.SuperChip8_Legacy;
        //var emuOptions = EmulationOptions.XO_CHIP;
        var machine = new Chip8Machine(emuOptions, traceSink);

        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\1-chip8-logo (1).ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\2-ibm-logo.ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\3-corax+.ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\4-flags.ch8";
        //var testRomPath = "C:\\Users\\blevalley\\Downloads\\5-quirks.ch8";

        var testRomPath = "C:\\Users\\blevalley\\Downloads\\RPS.ch8";
        var bytes = File.ReadAllBytes(testRomPath);
        machine.LoadRom(bytes);
        return machine;
    }
}
