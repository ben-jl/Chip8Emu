using SDL3;

namespace Chip8.SdlHost;

public sealed class SdlBeepAudio : IDisposable
{
    private const int SampleRate = 48_000;
    private const int Channels = 1;
    private const float Frequency = 440.0f;
    private const float Volume = 0.20f;

    private readonly nint _stream;
    private readonly float[] _buffer = new float[1024];

    private double _phase;
    private bool _enabled;
    private bool _disposed;

    public SdlBeepAudio()
    {
        var spec = new SDL.AudioSpec
        {
            Freq = SampleRate,
            Channels = Channels,
            Format = SDL.AudioFormat.AudioF32LE
        };

        _stream = SDL.OpenAudioDeviceStream(
            SDL.AudioDeviceDefaultPlayback,
            ref spec,
            null,
            nint.Zero);

        if (_stream == nint.Zero)
            throw new InvalidOperationException($"Could not open audio stream: {SDL.GetError()}");

        SDL.ResumeAudioStreamDevice(_stream);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public void Update()
    {
        if (!_enabled)
            return;

        // Avoid queueing unlimited audio.
        if (SDL.GetAudioStreamQueued(_stream) > SampleRate / 10)
            return;

        FillSineWave(_buffer);

        unsafe
        {
            fixed (float* ptr = _buffer)
            {
                int byteCount = _buffer.Length * sizeof(float);

                if (!SDL.PutAudioStreamData(_stream, (nint)ptr, byteCount))
                    throw new InvalidOperationException($"Could not queue audio: {SDL.GetError()}");
            }
        }
    }

    private void FillSineWave(float[] samples)
    {
        double phaseStep = Frequency / SampleRate;

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = MathF.Sin((float)(_phase * Math.Tau)) * Volume;

            _phase += phaseStep;

            if (_phase >= 1.0)
                _phase -= 1.0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        SDL.DestroyAudioStream(_stream);
        _disposed = true;
    }
}