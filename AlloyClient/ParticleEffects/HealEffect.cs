using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct HealParticleData {
    public readonly float Angle;
    public readonly float Radius;
    public float Z;
    public readonly float ZSpeed;

    public HealParticleData(float angle, float radius, float z, float zSpeed) {
        Angle = angle;
        Radius = radius;
        Z = z;
        ZSpeed = zSpeed;
    }
}

/// <summary>
///     One-shot ring of heal particles orbiting the entity.
///     Each particle stays at a fixed angle + radius and rises over 1000ms.
/// </summary>
public class HealEffect : ParticleEffect {
    private const int Buffer = 20;

    private readonly uint _color;
    private readonly HealParticleData[] _data = new HealParticleData[Buffer];
    private readonly Entity _entity;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private int _count;
    private bool _initialized;
    private double _timeLeft = 1000.0;

    public HealEffect(Entity entity, uint color) {
        _entity = entity;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        _timeLeft -= dt;
        if (_timeLeft <= 0) return false;

        if (!_initialized) {
            var amount = 10;
            _count = Math.Min(amount, Buffer);

            for (var i = 0; i < _count; i++) {
                var angle = 2f * MathF.PI * (i / (float)_count);
                var radius = 0.3f + 0.4f * Random.Shared.NextSingle();
                var initialZ = 0.3f * Random.Shared.NextSingle();
                var zSpeed = 0.075f + 0.05f * Random.Shared.NextSingle();
                var sizeScale = (2 + Random.Shared.Next(2)) * 0.2f;

                _data[i] = new HealParticleData(angle, radius, initialZ, zSpeed);
                _particles[i] = new ParticleData(Vector3.Zero, GetColor(_color)) {
                    Position = { W = sizeScale }
                };
            }

            _initialized = true;
        }

        for (var i = 0; i < _count; i++) {
            ref var data = ref _data[i];
            ref var particle = ref _particles[i];

            // z_ += moveVec_.z * dt * 0.008  (AS3 SparkParticle scaling)
            data.Z += data.ZSpeed * (float)(dt * 0.008);

            particle.Position.X = _entity.Position.X + data.Radius * MathF.Cos(data.Angle);
            particle.Position.Y = _entity.Position.Y + data.Radius * MathF.Sin(data.Angle);
            particle.Position.Z = data.Z;
        }

        Map.AddParticles(_particles, _count);
        return true;
    }
}