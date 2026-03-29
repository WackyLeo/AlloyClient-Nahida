using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct CircleOrbitData {
    public float Angle;
    public readonly float Speed;
    public float Radius;

    public CircleOrbitData(float angle, float speed) {
        Angle = angle;
        Speed = speed;
        Radius = 0f;
    }
}

public class CircleEffect : ParticleEffect {
    private const int Buffer = 50;
    private readonly uint _color;

    private readonly CircleOrbitData[] _data = new CircleOrbitData[Buffer];
    private readonly Entity _entity;

    private readonly float _maxRadius;
    private readonly float _moveSpeed;
    private readonly double _offset;
    private readonly int _particleCount;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private readonly int _particleSize;
    private readonly float _rise;
    private readonly float _speed;
    private int _count;

    private bool _initialized;
    private double _maxLife;

    private float _radius;
    private double _startTime = -1;

    public CircleEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _radius = props.MinRadius;
        _maxRadius = props.MaxRadius;
        _rise = props.Rise;
        _speed = props.Speed;
        _moveSpeed = props.MoveSpeed;
        _maxLife = props.Life * 1000.0;
        _offset = props.Offset;
        _particleCount = props.Amount;
        _particleSize = props.Size;
        _color = props.Color;
    }

    public override bool Update(double time, double dt) {
        if (_startTime < 0) _startTime = time;
        if (time - _startTime < _offset) return true;

        _position = _entity.Position;

        if (!_initialized) {
            _count = Math.Min(_particleCount, Buffer);
            var stepAngle = 2f * MathF.PI / _count;

            for (var i = 0; i < _count; i++) {
                var angle = stepAngle * i;
                _data[i] = new CircleOrbitData(angle, _speed);
                _particles[i] = new ParticleData(
                    new Vector3(_position.X + _radius * MathF.Cos(angle),
                        _position.Y + _radius * MathF.Sin(angle),
                        0.3f),
                    GetColor(_color)
                );
                _particles[i].Position.W = _particleSize * 1.1f;
            }

            _initialized = true;
        }

        _radius = MathF.Min(_radius + _rise * (float)(dt / 1000.0) * _moveSpeed, _maxRadius);

        for (var i = 0; i < _count; i++) {
            ref var data = ref _data[i];
            ref var particle = ref _particles[i];

            data.Angle += data.Speed * (float)(dt / 1000.0);
            particle.Position.X = _position.X + _radius * MathF.Cos(data.Angle);
            particle.Position.Y = _position.Y + _radius * MathF.Sin(data.Angle);
        }

        Map.AddParticles(_particles, _count);

        _maxLife -= dt;
        return _maxLife > 0;
    }
}