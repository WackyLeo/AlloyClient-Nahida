using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct SkyBeamParticleData {
    public readonly double StartTime;
    public readonly float Speed;
    public readonly float Height;

    public SkyBeamParticleData(double startTime, float speed, float height) {
        StartTime = startTime;
        Speed = speed;
        Height = height;
    }
}

public class SkyBeamEffect : ParticleEffect {
    private const int Buffer = 120;
    private readonly Vector4 _color;
    private readonly SkyBeamParticleData[] _data = new SkyBeamParticleData[Buffer];
    private readonly Entity _entity;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private readonly EffectProperties _props;
    private int _count;
    private double _lastUpdate = -1;
    private float _radius;

    public SkyBeamEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _props = props;
        _radius = props.MinRadius;
        _color = GetColor(props.Color == 0 ? 0x26CAFFu : props.Color);
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        if (_lastUpdate < 0)
            _lastUpdate = Math.Max(0, time - 400);

        _position = _entity.Position;

        const int periodMs = 20;
        var fromBucket = (int)(_lastUpdate / periodMs);
        var toBucket = (int)(time / periodMs);

        for (var bucket = fromBucket; bucket < toBucket && _count < Buffer; bucket++) {
            var spawnTime = bucket * periodMs;
            var angle = Random.Shared.NextSingle() * MathF.Tau;
            var dist = Random.Shared.NextSingle() * _radius;
            var x = _position.X + dist * MathF.Cos(angle);
            var y = _position.Y + dist * MathF.Sin(angle);
            var speed = _props.Speed + (_props.MaxRadius - dist);
            var height = _props.ZOffset;

            _data[_count] = new SkyBeamParticleData(spawnTime, speed, height);
            _particles[_count] = new ParticleData(new Vector3(x, y, height), _color) {
                Position = { W = ParticleW(100) }
            };
            _count++;
        }

        _radius = MathF.Min(_radius + _props.Rise * (float)(dt / 1000.0), _props.MaxRadius);

        for (var i = _count - 1; i >= 0; i--) {
            var d = _data[i];
            var ageSec = (float)((time - d.StartTime) / 1000.0);
            var z = d.Height - d.Speed * ageSec;
            if (z <= 0f) {
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
