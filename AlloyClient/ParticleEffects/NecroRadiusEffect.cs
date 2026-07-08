using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class NecroRadiusEffect : ParticleEffect {
    private const int Count = 16;
    private readonly uint _color;
    private readonly Entity _entity;
    private readonly ParticleData[] _particles = new ParticleData[Count];
    private readonly float _radius;
    private double _lifeLeft = 300.0;
    private bool _initialized;
    private float _angle;

    public NecroRadiusEffect(Entity entity, uint color, float radius) {
        _entity = entity;
        _color = color;
        _radius = radius;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        _position = _entity.Position;
        if (!_initialized) {
            var color = GetColor(_color);
            for (var i = 0; i < Count; i++) {
                var a = i * MathF.Tau / Count;
                _particles[i] = new ParticleData(new Vector3(
                    _position.X + _radius * MathF.Cos(a),
                    _position.Y + _radius * MathF.Sin(a),
                    0.4f), color) {
                    Position = { W = ParticleW(240) }
                };
            }

            _initialized = true;
        }

        _angle += 0.025f;
        for (var i = 0; i < Count; i++) {
            var a = _angle + i * MathF.Tau / Count;
            _particles[i].Position.X = _position.X + _radius * MathF.Cos(a);
            _particles[i].Position.Y = _position.Y + _radius * MathF.Sin(a);
        }

        _lifeLeft -= dt;
        if (_lifeLeft <= 0)
            return false;

        Map.AddParticles(_particles, Count);
        return true;
    }
}
