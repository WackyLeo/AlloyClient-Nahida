using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct VentParticleData {
    public readonly double StartTime;
    public readonly float Speed;

    public VentParticleData(double startTime, float speed) {
        StartTime = startTime;
        Speed = speed;
    }
}

public class VentEffect : ParticleEffect {
    private const int Buffer = 80;
    private const int PeriodMs = 50;
    private readonly Vector4 _color = new(0x26 / 255f, 0xCA / 255f, 0xFF / 255f, -1f);
    private readonly VentParticleData[] _data = new VentParticleData[Buffer];
    private readonly Entity _entity;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private int _count;
    private double _lastUpdate = -1;

    public VentEffect(Entity entity) {
        _entity = entity;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        if (_lastUpdate < 0)
            _lastUpdate = Math.Max(0, time - 400);

        _position = _entity.Position;
        var fromBucket = (int)(_lastUpdate / PeriodMs);
        var toBucket = (int)(time / PeriodMs);

        for (var bucket = fromBucket; bucket < toBucket && _count < Buffer; bucket++) {
            var spawnTime = bucket * PeriodMs;
            var r = Random.Shared.NextSingle();
            var angle = Random.Shared.NextSingle() * MathF.PI;
            var dist = Random.Shared.NextSingle() * 0.4f;
            var x = _position.X + dist * MathF.Cos(angle);
            var y = _position.Y + dist * MathF.Sin(angle);
            var speed = 2.5f - r * 1.5f;

            _data[_count] = new VentParticleData(spawnTime, speed);
            _particles[_count] = new ParticleData(new Vector3(x, y, 0f), _color) {
                Position = { W = ParticleW((int)(75 + r * 50)) }
            };
            _count++;
        }

        for (var i = _count - 1; i >= 0; i--) {
            var d = _data[i];
            var ageSec = (float)((time - d.StartTime) / 1000.0);
            var z = d.Speed * ageSec;
            if (z >= 1f) {
                _particles[i] = _particles[_count - 1];
                _data[i] = _data[_count - 1];
                _count--;
                continue;
            }

            _particles[i].Position.Z = z;
        }

        if (_count > 0)
            Map.AddParticles(_particles, _count);

        _lastUpdate = time;
        return true;
    }
}
