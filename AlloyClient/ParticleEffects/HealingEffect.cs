using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct HealingParticleData {
    public readonly float Angle;
    public readonly float Radius;
    public readonly float ZSpeed;
    public float Z;
    public float TimeLeft;

    public HealingParticleData(float angle, float radius, float zSpeed, float timeLeft) {
        Angle = angle;
        Radius = radius;
        ZSpeed = zSpeed;
        Z = 0f;
        TimeLeft = timeLeft;
    }
}

public class HealingEffect : ParticleEffect {
    private const int Buffer = 30;
    private const int ParticlesPerSecond = 10;

    private readonly HealingParticleData[] _data = new HealingParticleData[Buffer];
    private readonly Entity _entity;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private int _count;
    private double _spawnRemainder;

    public HealingEffect(Entity entity) {
        _entity = entity;
    }

    public override bool Update(double time, double dt) {
        _position = _entity.Position;

        var toSpawn = ParticlesPerSecond * (dt / 1000.0) + _spawnRemainder;
        var intSpawn = (int)toSpawn;
        _spawnRemainder = toSpawn - intSpawn;

        for (var s = 0; s < intSpawn; s++) {
            if (_count == Buffer) break;

            var angle = 2f * MathF.PI * Random.Shared.NextSingle();
            var distance = 0.3f + 0.4f * Random.Shared.NextSingle();
            var lifeMs = (3 + Random.Shared.Next(5)) * 20f;
            var zSpeed = 0.075f + 0.05f * Random.Shared.NextSingle();
            var sizeScale = (2 + Random.Shared.Next(2)) * 0.2f;

            _data[_count] = new HealingParticleData(angle, distance, zSpeed, lifeMs);
            _particles[_count] = new ParticleData(
                new Vector3(_position.X + distance * MathF.Cos(angle),
                    _position.Y + distance * MathF.Sin(angle),
                    0f),
                GetColor(0xFFFFFF)
            ) { Position = { W = sizeScale } };

            _count++;
        }

        for (var i = _count - 1; i >= 0; i--) {
            ref var data = ref _data[i];
            ref var particle = ref _particles[i];

            data.TimeLeft -= (float)dt;
            if (data.TimeLeft <= 0) {
                _particles[i] = _particles[_count - 1];
                _particles[i].Position.W = ParticleW((3 + Random.Shared.Next(5)) * 20);
                _data[i] = _data[_count - 1];
                _count--;
                continue;
            }

            data.Z += data.ZSpeed * (float)(dt * 0.008);
            particle.Position.X = _position.X + data.Radius * MathF.Cos(data.Angle);
            particle.Position.Y = _position.Y + data.Radius * MathF.Sin(data.Angle);
            particle.Position.Z = data.Z;
        }

        if (_count > 0) Map.AddParticles(_particles, _count);
        return true;
    }
}