using System.IO;
using System.Threading.Tasks;
using AlloyClient.State;
using Common;
using NAudio.Wave;

namespace AlloyClient.Sound;

public static class Music {
    private static readonly Logger Log = new(typeof(Music));

    private static string _currentSongName;
    private static GameMusicProvider _currentMusicProvider;

    public static void PlayMusic(string name, float fadeDurationSecs = 2f) {
        if (!Settings.PlayMusic || _currentSongName == name) return;

        Log.Info($"Playing song {name}");
        _currentSongName = name;

        if (_currentMusicProvider != null) _currentMusicProvider.BeginFadeOut(fadeDurationSecs);

        Task.Run(async () => {
            var mp3Bytes = await AudioCache.LoadMusicAsync(name);
            if (mp3Bytes == null) return;

            if (_currentSongName != name) return;
            _currentMusicProvider = new GameMusicProvider(mp3Bytes, Settings.MusicVolume);
            _currentMusicProvider.BeginFadeIn(fadeDurationSecs);

            SoundManager.AddToMixer(_currentMusicProvider);
        });
    }

    public static void StopMusic(float fadeDurationSecs = 2f) {
        _currentSongName = null;
        _currentMusicProvider?.BeginFadeOut(fadeDurationSecs);
        _currentMusicProvider = null;
    }

    public static void SetVolume(float volume) {
        if (_currentMusicProvider != null) _currentMusicProvider.TargetVolume = volume;
    }
}

public class GameMusicProvider : ISampleProvider {
    private readonly MemoryStream _memoryStream;
    private readonly Mp3FileReader _reader;
    private readonly ISampleProvider _resampler;
    private float _fadeDelta;

    private float _fadeMultiplier;
    private bool _isFadingOut;

    public GameMusicProvider(byte[] mp3Bytes, float volume) {
        TargetVolume = volume;
        _memoryStream = new MemoryStream(mp3Bytes);
        _reader = new Mp3FileReader(_memoryStream);

        var resampler = new MediaFoundationResampler(_reader, SoundManager.MixerFormat);
        _resampler = resampler.ToSampleProvider();
    }

    public float TargetVolume { get; set; }
    public WaveFormat WaveFormat => SoundManager.MixerFormat;

    public int Read(float[] buffer, int offset, int count) {
        var read = _resampler.Read(buffer, offset, count);

        if (read < count && !_isFadingOut) {
            _reader.Position = 0;
            read += _resampler.Read(buffer, offset + read, count - read);
        }

        for (var i = 0; i < read; i++) {
            if (i % WaveFormat.Channels == 0 && _fadeDelta != 0) {
                _fadeMultiplier += _fadeDelta;
                if (_fadeMultiplier >= 1f) {
                    _fadeMultiplier = 1f;
                    _fadeDelta = 0f;
                } else if (_fadeMultiplier <= 0f) {
                    _fadeMultiplier = 0f;
                    Dispose();
                    return 0;
                }
            }

            buffer[offset + i] *= _fadeMultiplier * TargetVolume;
        }

        return read;
    }

    public void BeginFadeIn(float durationSecs) {
        _fadeMultiplier = 0f;
        _fadeDelta = 1f / (durationSecs * WaveFormat.SampleRate);
        _isFadingOut = false;
    }

    public void BeginFadeOut(float durationSecs) {
        _fadeDelta = -(1f / (durationSecs * WaveFormat.SampleRate));
        _isFadingOut = true;
    }

    private void Dispose() {
        _reader?.Dispose();
        _memoryStream?.Dispose();
    }
}