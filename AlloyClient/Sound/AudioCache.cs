using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AlloyClient.State;
using Common;
using NAudio.Wave;

namespace AlloyClient.Sound;

public enum CacheTier {
    Permanent,
    Volatile
}

public static class AudioCache {
    private static readonly Logger Log = new(typeof(AudioCache));
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(Settings.AppEngineUrl) };

    private static readonly ConcurrentDictionary<string, float[]> PermanentSfx = new();
    private static readonly ConcurrentDictionary<string, float[]> VolatileSfx = new();
    private static readonly ConcurrentDictionary<string, byte[]> MusicCache = new();

    public static async Task PreloadSoundsAsync(IEnumerable<string> names, CacheTier tier) {
        var tasks = new List<Task>();
        foreach (var name in names) tasks.Add(LoadSfxAsync(name, tier));
        await Task.WhenAll(tasks);
    }

    public static async Task<float[]> LoadSfxAsync(string name, CacheTier tier) {
        var cache = tier == CacheTier.Permanent ? PermanentSfx : VolatileSfx;
        if (cache.TryGetValue(name, out var existing)) return existing;

        try {
            var url = $"/sfx/{name}.mp3";
            var bytes = await Http.GetByteArrayAsync(url);
            var samples = DecodeToFloatArray(bytes);

            cache[name] = samples;
            return samples;
        } catch (Exception e) {
            Log.Error($"Failed to download SFX {name}: {e.Message}");
            return null;
        }
    }

    public static async Task<byte[]> LoadMusicAsync(string name) {
        if (MusicCache.TryGetValue(name, out var existing)) return existing;

        try {
            var url = $"/music/{name}.mp3";
            var bytes = await Http.GetByteArrayAsync(url);
            MusicCache[name] = bytes;
            return bytes;
        } catch (Exception e) {
            Log.Error($"Failed to download Music {name}: {e.Message}");
            return null;
        }
    }

    public static float[] GetSfx(string name) {
        if (PermanentSfx.TryGetValue(name, out var data)) return data;
        if (VolatileSfx.TryGetValue(name, out data)) return data;
        return null;
    }

    public static void ClearVolatileCache() {
        VolatileSfx.Clear();
    }

    private static float[] DecodeToFloatArray(byte[] audioData) {
        using var ms = new MemoryStream(audioData);
        using var reader = new Mp3FileReader(ms);
        using var resampler = new MediaFoundationResampler(reader, SoundManager.MixerFormat);
        var sampleProvider = resampler.ToSampleProvider();

        var samples = new List<float>();
        var buffer = new float[SoundManager.MixerFormat.SampleRate * SoundManager.MixerFormat.Channels];
        int read;
        while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0) {
            var temp = new float[read];
            Array.Copy(buffer, temp, read);
            samples.AddRange(temp);
        }

        return samples.ToArray();
    }
}