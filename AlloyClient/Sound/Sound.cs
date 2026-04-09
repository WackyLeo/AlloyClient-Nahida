using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AlloyClient.State;
using Common;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AlloyClient.Sound;

public enum SoundType {
    Sfx,
    Weapon,
    Loot
}

public static class SoundManager {
    private static readonly Logger Log = new(typeof(SoundManager));
    private static readonly ConcurrentDictionary<string, SoundEffect> CachedSoundEffects = new();

    internal static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private static readonly MixingSampleProvider Mixer;
    private static readonly WaveOutEvent OutputDevice;

    static SoundManager() {
        Mixer = new MixingSampleProvider(MixerFormat) { ReadFully = true };
        OutputDevice = new WaveOutEvent { DesiredLatency = 50 };
        OutputDevice.Init(Mixer);
        OutputDevice.Play();
    }

    public static void PreLoadSounds() {
        var names = new[] {
            "button_click",
            "death_screen",
            "enter_realm",
            "error",
            "inventory_move_item",
            "level_up",
            "loot_appears",
            "no_mana",
            "use_key",
            "use_potion",
            "loot/legendary_loot",
            "loot/demonic_loot",
            "loot/cosmic_loot",
            "loot/angelic_loot"
        };

        Parallel.ForEach(names, name => LoadSoundEffect(name));
    }

    public static void PlaySoundEffect(string name, SoundType soundType = SoundType.Sfx) {
        float volume;
        switch (soundType) {
            case SoundType.Sfx:
                if (!Settings.PlaySfx) return;
                volume = Settings.SfxVolume;
                break;
            case SoundType.Weapon:
                if (!Settings.PlayWeaponSfx) return;
                volume = Settings.WeaponSfxVolume;
                break;
            case SoundType.Loot:
                if (!Settings.PlayLootSfx) return;
                volume = Settings.LootSfxVolume;
                break;
            default:
                return;
        }

        if (CachedSoundEffects.TryGetValue(name, out var effect)) {
            effect?.Play(volume);
            return;
        }

        Task.Run(() => {
            try {
                LoadSoundEffect(name)?.Play(volume);
            } catch (Exception e) {
                Log.Error($"Failed to lazy-load sound {name}: {e}");
            }
        });
    }

    internal static void AddToMixer(ISampleProvider provider) {
        try {
            Mixer.AddMixerInput(provider);
        } catch (Exception e) {
            Log.Error($"Mixer add failed: {e.Message}");
        }
    }

    public static void Dispose() {
        OutputDevice.Stop();
        OutputDevice.Dispose();
    }

    private static SoundEffect LoadSoundEffect(string name) {
        return CachedSoundEffects.GetOrAdd(name, _ => {
            try {
                return new SoundEffect($"{Settings.AppEngineUrl}//sfx/{name}.mp3");
            } catch (Exception e) {
                Log.Error($"Failed to load sound {name}: {e}");
                return null;
            }
        });
    }
}

public class SoundEffect {
    private static readonly Logger Log = new(typeof(SoundEffect));

    private readonly float[] _samples;

    public SoundEffect(string url) {
        using var reader = new MediaFoundationReader(url);
        using var resampler = new MediaFoundationResampler(reader, SoundManager.MixerFormat);

        var byteBuffer = new byte[resampler.WaveFormat.AverageBytesPerSecond * 30];
        var bytesRead = resampler.Read(byteBuffer, 0, byteBuffer.Length);

        _samples = new float[bytesRead / 4];
        Buffer.BlockCopy(byteBuffer, 0, _samples, 0, bytesRead);
    }

    public void Play(float volume = 1f) {
        try {
            var provider = new FloatArraySampleProvider(_samples, SoundManager.MixerFormat, volume);
            SoundManager.AddToMixer(provider);
        } catch (Exception e) {
            Log.Error($"SoundEffect.Play failed: {e}");
        }
    }
}

internal class FloatArraySampleProvider : ISampleProvider {
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

        for (var i = 0; i < toRead; i++)
            buffer[offset + i] = _samples[_position + i] * _volume;

        _position += toRead;
        return toRead;
    }
}