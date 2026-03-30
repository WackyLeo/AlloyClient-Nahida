using System;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct StreamParticleData {
    public double TimeLeft;
    public readonly double Duration;
    public readonly float Period;

    public StreamParticleData(double duration, float period) {
        TimeLeft = duration;
        Duration = duration;
        Period = period;
    }
}

public class StreamEffect : ParticleEffect {
    private const int Buffer = 20;

    private readonly StreamParticleData[] _data = new StreamParticleData[Buffer];
    private readonly Vector2 _deflect;
    private readonly Vector2 _end;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private readonly Vector2 _start;
    private int _count;

    public StreamEffect(Vector2 start, Vector2 end, uint color) {
        _start = start;
        _end = end;

        const int amount = 10;
        _count = Math.Min(amount, Buffer);

        var dir = end - start;
        var dist = dir.Length;
        if (dist > 0.0001f) {
            var norm = dir / dist;
            _deflect = new Vector2(norm.Y, -norm.X) * 0.25f;
        } else {
            _deflect = Vector2.Zero;
        }

        var parsedColor = GetColor(color);

        for (var i = 0; i < _count; i++) {
            var duration = 1500.0 + Random.Shared.NextDouble() * 3000.0;
            var period = 0.25f + Random.Shared.NextSingle() * 0.5f;
            var sizeScale = (3 + Random.Shared.Next(5)) * 0.2f;

            _data[i] = new StreamParticleData(duration, period);
            _particles[i] = new ParticleData(new Vector3(start.X, start.Y, 1.85f), parsedColor) {
                Position = { W = sizeScale }
            };
        }
    }

    public override bool Update(double time, double dt) {
        for (var i = _count - 1; i >= 0; i--) {
            ref var data = ref _data[i];
            data.TimeLeft -= dt;

            if (data.TimeLeft <= 0) {
                _particles[i] = _particles[_count - 1];
                _data[i] = _data[_count - 1];
                _count--;
                continue;
            }

            var t = (float)(1.0 - data.TimeLeft / data.Duration);
            var path = Vector2.Lerp(_start, _end, t);
            var sine = MathF.Sin((float)(data.TimeLeft / 1000.0) / data.Period);

            ref var particle = ref _particles[i];
            particle.Position.X = path.X + _deflect.X * sine;
            particle.Position.Y = path.Y + _deflect.Y * sine;
        }

        if (_count > 0) {
            Map.AddParticles(_particles, _count);
            return true;
        }

        return false;
    }
}