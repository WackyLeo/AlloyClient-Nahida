using System;
using Alloy.Audio;
using AlloyClient.State;
using Microsoft.Extensions.Logging;

namespace AlloyClient;

public static class Audio {

    public static SingleTrackChannel MusicChannel { get; private set; }
    public static SfxChannel SfxChannel { get; private set; }

    private static AudioEngine _audioEngine;

    public static void Init(ILoggerFactory logFactory, string localPath) {
        _audioEngine = new AudioEngine(logFactory, localPath);
        MusicChannel = _audioEngine.CreateSingleTrackChannel();
        SfxChannel = _audioEngine.CreateSfxChannel();
    }

    public static void Start() => _audioEngine.Start();

    public static void Stop() => _audioEngine.StopAndDispose();

    public static void SetMasterVolume(float volume) => _audioEngine.SetMasterVolume(volume);
    
    public static void PlaySfx(string name) => SfxChannel.Play(EnsureOgg(name));

    private static string EnsureOgg(string name) =>
        name.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ? name : name + ".ogg";

}