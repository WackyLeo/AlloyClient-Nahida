using System;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class FountainSnowyEffect : ParticleEffect {
    private const float G = -0.8f;
    private const float VI = 2.0f;
    private const int Buffer = 50;

    private readonly Vector4 _color;
    private readonly FountainParticle[] _data = new FountainParticle[Buffer];
    private readonly ParticleData[] _particles = new ParticleData[Buffer];

    private int _count;
    private double _lastUpdate = -1;

    public FountainSnowyEffect(uint color = 4285909) {
        _color = GetColor(color);
    }

    public override bool Update(double time, double dt) {
        if (_lastUpdate < 0) _lastUpdate = Math.Max(0, time - 400);

        var count = (int)(time / 50);
        var start = (int)(_lastUpdate / 50);

        for (var i = start; i < count; i++) {
            if (_count == Buffer) break;

            _data[_count] = new FountainParticle(time, MathHelper.TwoPi * Random.Shared.NextSingle());
            _particles[_count] = new ParticleData(new Vector3(_position, 0.75f), _color);
            _count++;
        }

        for (var i = _count - 1; i >= 0; i--) {
            var data = _data[i];
            var t = (float)((time - data.StartTime) / 1000.0);
            var z = 0.75f + VI * t + G * (t * t);

            if (z <= 0) {
                _particles[i] = _particles[_count - 1];
                _data[i] = _data[_count - 1];
                _count--;
                continue;
            }

            ref var part = ref _particles[i];
            part.Position.X += data.Velocity.X * (float)(dt * 0.0008);
            part.Position.Y += data.Velocity.Y * (float)(dt * 0.0008);
            part.Position.X += Random.Shared.NextSingle() * 0.1f - 0.05f;
            part.Position.Y += Random.Shared.NextSingle() * 0.1f - 0.05f;
            part.Position.Z = z;
        }

        Map.AddParticles(_particles, _count);
        _lastUpdate = time;
        return true;
    }
}