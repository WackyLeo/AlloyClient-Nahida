using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class FlowEffect : ParticleEffect {
    private const int ParticleCount = 5;
    private readonly uint _color;
    private readonly Entity _target;
    private readonly Vector2 _start;

    public FlowEffect(Vector2 start, Entity target, uint color) {
        _start = start;
        _position = start;
        _target = target;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        if (_target == null || !Map.Entities.ContainsKey(_target.ObjectId))
            return false;

        for (var i = 0; i < ParticleCount; i++) {
            var size = (3 + Random.Shared.Next(5)) * 20;
            Map.AddParticleEffect(new FlowParticleEffect(_start, _target, _color, size));
        }

        return false;
    }
}

internal class FlowParticleEffect : ParticleEffect {
    private readonly uint _color;
    private readonly Entity _target;
    private readonly int _size;
    private float _flowSpeed;
    private float _maxDist;
    private ParticleData _particle;

    public FlowParticleEffect(Vector2 start, Entity target, uint color, int size) {
        _position = start;
        _target = target;
        _color = color;
        _size = size;
        _flowSpeed = Random.Shared.NextSingle() * 5f;
        _maxDist = target != null ? Vector2.Distance(start, target.Position) : 0f;
        _particle = new ParticleData(new Vector3(_position, 0.5f), GetColor(color)) {
            Position = { W = ParticleW(size) }
        };
    }

    public override bool Update(double time, double dt) {
        if (_target == null || !Map.Entities.ContainsKey(_target.ObjectId))
            return false;

        var target = _target.Position;
        var toTarget = target - _position;
        var dist = toTarget.Length;
        if (dist < 0.5f)
            return false;

        var dtSec = (float)(dt / 1000.0);
        _flowSpeed += 8f * dtSec;
        _maxDist -= _flowSpeed * dtSec;

        var nextDist = dist - _flowSpeed * dtSec;
        if (nextDist > _maxDist)
            nextDist = _maxDist;

        if (dist > 0.0001f)
            _position = target - toTarget * (nextDist / dist);

        _particle.Position.X = _position.X;
        _particle.Position.Y = _position.Y;
        _particle.Position.W = ParticleW(_size);
        Map.AddParticle(_particle);
        return true;
    }
}
