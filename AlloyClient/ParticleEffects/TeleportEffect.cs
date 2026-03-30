using System;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct TeleportParticleData {
    public double TimeLeft;
    public float Z;
}

public class TeleportEffect : ParticleEffect {
    private const int ParticleQuality = 2;
    private const int Buffer = 20;
    private readonly TeleportParticleData[] _data = new TeleportParticleData[Buffer];
    private readonly ParticleData[] _particles = new ParticleData[Buffer];

    private int _count;

    public TeleportEffect(Vector2 position) {
        _position = position;

        var amount = ParticleQuality switch {
            0 => 10,
            1 => 15,
            _ => 20
        };

        _count = Math.Min(amount, Buffer);
        var color = GetColor(0x0000FF);

        for (var i = 0; i < _count; i++) {
            var angle = 2f * MathF.PI * Random.Shared.NextSingle();
            var radius = 0.7f * Random.Shared.NextSingle();

            _data[i] = new TeleportParticleData {
                TimeLeft = 5 + 2000 * Random.Shared.NextDouble(),
                Z = 0f
            };

            _particles[i] = new ParticleData(new Vector3(
                _position.X + radius * MathF.Cos(angle),
                _position.Y + radius * MathF.Sin(angle),
                0f
            ), color) {
                Position = { W = 0.5f }
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

            data.Z += 0.1f * (float)(dt * 0.008);
            _particles[i].Position.Z = data.Z;
        }

        if (_count > 0) {
            Map.AddParticles(_particles, _count);
            return true;
        }

        return false;
    }
}