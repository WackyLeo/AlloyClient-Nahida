using System;
using System.Threading.Tasks;
using AlloyClient.State;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AlloyClient.Sound;

public enum SoundType {
    Sfx,
    Weapon,
    Loot
}

public static class SoundManager {
    internal static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private static readonly MixingSampleProvider Mixer;
    private static readonly WaveOutEvent OutputDevice;

    static SoundManager() {
        Mixer = new MixingSampleProvider(MixerFormat) { ReadFully = true };
        OutputDevice = new WaveOutEvent { DesiredLatency = 100 };
        OutputDevice.Init(Mixer);
        OutputDevice.Play();
    }

    public static void PlaySoundEffect(string name, SoundType soundType = SoundType.Sfx) {
        var volume = 0.0f;
        switch (soundType) {
            case SoundType.Sfx: volume = Settings.SfxVolume; break;
            case SoundType.Weapon: volume = Settings.WeaponSfxVolume; break;
            case SoundType.Loot: volume = Settings.LootSfxVolume; break;
            default: volume = 0.0f; break;
        }

        if (volume <= 0f) return;

        var samples = AudioCache.GetSfx(name);

        if (samples != null)
            PlayFromMemory(samples, volume);
        else
            Task.Run(async () => {
                var newSamples = await AudioCache.LoadSfxAsync(name, CacheTier.Volatile);
                if (newSamples != null) PlayFromMemory(newSamples, volume);
            });
    }

    private static void PlayFromMemory(float[] samples, float volume) {
        var provider = new FloatArraySampleProvider(samples, MixerFormat, volume);


        //Mixer.AddMixerInput(provider);
    }

    internal static void AddToMixer(ISampleProvider provider) {
        Mixer.AddMixerInput(provider);
    }
}

public class FloatArraySampleProvider : ISampleProvider {
    private readonly float[] _samples;
    private readonly float _volume;
    private int _position;

    public FloatArraySampleProvider(float[] samples, WaveFormat format, float volume) {
        _samples = samples;
        _volume = volume;
        WaveFormat = format;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count) {
        var available = _samples.Length - _position;
        var toRead = Math.Min(available, count);
        if (toRead <= 0) return 0;

        for (var i = 0; i < toRead; i++) buffer[offset + i] = _samples[_position + i] * _volume;

        _position += toRead;
        return toRead;
    }
}