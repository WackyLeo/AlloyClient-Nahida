using System;
using AlloyClient.Game;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class ThrowEffect : ParticleEffect {
    private const int ParticleQuality = 2;
    private readonly uint _color;
    private readonly double _duration;
    private readonly Vector2 _end;

    private readonly int _sparkLifetime;

    private readonly Vector2 _start;
    private double _spawnTimer;

    private double _timeLeft;

    public ThrowEffect(Vector2 start, Vector2 end, uint color, int duration = 1500) {
        _start = start;
        _end = end;
        _color = color;
        _duration = duration;
        _timeLeft = duration;
        _sparkLifetime = 400;
    }

    public override bool Update(double time, double dt) {
        _timeLeft -= dt;
        if (_timeLeft <= 0) return false;

        var t = (float)(1.0 - _timeLeft / _duration);
        _position = Vector2.Lerp(_start, _end, t);

        var z = MathF.Sin((float)(_timeLeft / _duration) * MathF.PI) * 2f;
        var size = (int)(75 * (z + 1));

        _spawnTimer -= dt;
        while (_spawnTimer <= 0) {
            var sparkEnd = _position + new Vector2(
                Random.Shared.NextSingle() * 2 - 1,
                Random.Shared.NextSingle() * 2 - 1
            );

            Map.AddParticleEffect(new SparkEffect(
                size, _color, _sparkLifetime, z, _position, sparkEnd
            ));

            _spawnTimer += 16.66;
        }

        return true;
    }
}