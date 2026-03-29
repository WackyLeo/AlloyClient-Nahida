using System;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class BurstEffect : ParticleEffect {
    private const int RingAmount = 30;
    private const int RingSize = 2;
    private const float RingLife = 2f;
    private const float RingSpeed = 1.5f;

    private readonly Vector2 _center;
    private readonly uint _color;
    private readonly Vector2 _edge;
    private readonly int _particleCount;

    public BurstEffect(Vector2 center, Vector2 edge, uint color, int particleCount = 30) {
        _center = center;
        _position = center;
        _edge = edge;
        _color = color;
        _particleCount = particleCount;
    }

    public override bool Update(double time, double dt) {
        var radius = Vector2.Distance(_center, _edge);

        for (var i = 0; i < _particleCount; i++) {
            var angle = i * 2 * MathF.PI / _particleCount;
            var end = new Vector2(
                _center.X + radius * MathF.Cos(angle),
                _center.Y + radius * MathF.Sin(angle)
            );
            var life = 50 + (int)(Random.Shared.NextSingle() * 20);
            Map.AddParticleEffect(new SparkerEffect(_particleCount, _color, life, 0f, _center, end));
        }

        var ringCount = (int)Math.Floor(RingAmount * (_particleCount / 30.0));
        if (ringCount > 0)
            Map.AddParticleEffect(new BurstOrbitEffect(_center, radius, ringCount, _color, RingSize, RingLife,
                RingSpeed));

        return false;
    }
}

internal class BurstOrbitEffect : ParticleEffect {
    private readonly float[] _angles;
    private readonly int _count;
    private readonly double[] _lifeTimes;

    private readonly ParticleData[] _particles;
    private readonly float _radius;
    private readonly float _speed;
    private readonly double[] _timesLeft;

    public BurstOrbitEffect(Vector2 center, float radius, int count, uint color,
        int sizeMultiplier, float lifeScale, float speed) {
        _position = center;
        _radius = radius;
        _speed = speed;
        _count = count;
        _particles = new ParticleData[count];
        _angles = new float[count];
        _lifeTimes = new double[count];
        _timesLeft = new double[count];

        var baseColor = GetColor(color);
        for (var i = 0; i < count; i++) {
            var lifeTime = (100.0 + Random.Shared.NextSingle() * 50.0) * lifeScale * sizeMultiplier;
            var startAngle = i * 2 * MathF.PI / count;

            _angles[i] = startAngle;
            _lifeTimes[i] = lifeTime;
            _timesLeft[i] = lifeTime;

            var px = center.X + radius * MathF.Cos(startAngle);
            var py = center.Y + radius * MathF.Sin(startAngle);
            var p = 1.0f;
            _particles[i] = new ParticleData(new Vector3(px, py, 0.4f), baseColor);
            _particles[i].Position.W = 200f * p * sizeMultiplier / 100f;
        }
    }

    public override bool Update(double time, double dt) {
        var dtSec = (float)(dt / 1000.0);
        var anyAlive = false;

        for (var i = 0; i < _count; i++) {
            if (_timesLeft[i] <= 0) continue;

            _timesLeft[i] -= dt;
            if (_timesLeft[i] <= 0) continue;

            anyAlive = true;
            _angles[i] += _speed * dtSec;

            var p = (float)(_timesLeft[i] / _lifeTimes[i]);
            _particles[i].Position.X = _position.X + _radius * MathF.Cos(_angles[i]);
            _particles[i].Position.Y = _position.Y + _radius * MathF.Sin(_angles[i]);
            _particles[i].Position.W = 200f * p / 100f;
        }

        if (!anyAlive) return false;
        var submitCount = 0;
        var submit = new ParticleData[_count];
        for (var i = 0; i < _count; i++)
            if (_timesLeft[i] > 0)
                submit[submitCount++] = _particles[i];

        Map.AddParticles(submit, submitCount);
        return true;
    }
}