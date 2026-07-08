using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct HeartParticleData {
    public readonly double StartTime;
    public readonly float RadiusVariance;

    public HeartParticleData(double startTime, float radiusVariance) {
        StartTime = startTime;
        RadiusVariance = radiusVariance;
    }
}

public class HeartEffect : ParticleEffect {
    private const int Buffer = 120;
    private readonly uint _color;
    private readonly uint _color2;
    private readonly HeartParticleData[] _data = new HeartParticleData[Buffer];
    private readonly Entity _entity;
    private readonly int _maxLifeMs;
    private readonly float _maxRadius;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private readonly float _rise;
    private int _count;
    private bool _initialized;
    private double _lifeLeft;
    private float _radius;
    private double _startTime = -1;

    public HeartEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _color = props.Color == 0 ? 0xFF0000u : props.Color;
        _color2 = props.Color2;
        _rise = props.Rise;
        _radius = props.MinRadius;
        _maxRadius = props.MaxRadius;
        _maxLifeMs = Math.Max(1, (int)(props.Life * 1000f));
        _lifeLeft = _maxLifeMs;
        _count = Math.Clamp(props.Amount <= 0 ? 16 : props.Amount, 1, Buffer);
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        if (_startTime < 0)
            _startTime = time;

        _position = _entity.Position;

        if (!_initialized) {
            var step = MathF.Tau / _count;
            for (var i = 0; i < _count; i++) {
                var phaseStart = time + step * i * 1000.0;
                var color = GetColor(RandomColor(_color, _color2));
                var size = 140 + Random.Shared.NextSingle() * 40f;
                var z = 0.4f + MathF.Sin(step * i * 1.5f) * 0.05f;

                _data[i] = new HeartParticleData(phaseStart, 0.97f + Random.Shared.NextSingle() * 0.06f);
                _particles[i] = new ParticleData(new Vector3(_position.X, _position.Y, z), color) {
                    Position = { W = ParticleW((int)size) }
                };
            }

            _initialized = true;
        }

        for (var i = 0; i < _count; i++) {
            var t = (float)((time - _data[i].StartTime) / 1000.0);
            var s = MathF.Sin(t);
            var c = MathF.Cos(t);
            var radius = _radius * _data[i].RadiusVariance;

            var x = 16f * s * s * s / 16f;
            var y = (13f * c - 5f * MathF.Cos(2f * t) - 2f * MathF.Cos(3f * t) - MathF.Cos(4f * t)) / 16f;

            _particles[i].Position.X = _position.X + x * radius;
            _particles[i].Position.Y = _position.Y - y * radius;
        }

        if (_lifeLeft <= 500.0) {
            _radius = MathF.Max(_radius - 2f * _maxRadius * (float)(dt / 1000.0), 0f);
        }
        else {
            _radius = MathF.Min(_radius + _rise * (float)(dt / 1000.0), _maxRadius);
        }

        _lifeLeft -= dt;
        if (_lifeLeft <= 0)
            return false;

        Map.AddParticles(_particles, _count);
        return true;
    }
}
