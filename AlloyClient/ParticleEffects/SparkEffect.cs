using System;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class SparkEffect : ParticleEffect {
    private readonly float _dx;
    private readonly float _dy;
    private readonly double _lifeTime;

    private readonly int _startingSize;
    private ParticleData _particle;
    private double _timeLeft;

    public SparkEffect(int size, uint color, int lifetime, float z, Vector2 start, Vector2 end) {
        _startingSize = size;
        _lifeTime = _timeLeft = lifetime;
        _dx = (end.X - start.X) / (lifetime / 1000f);
        _dy = (end.Y - start.Y) / (lifetime / 1000f);
        _particle = new ParticleData(new Vector3(start, z), GetColor(color));
    }

    public SparkEffect(int size, uint color, int lifetime, float z, float dx, float dy, float x, float y) {
        _startingSize = size;
        _lifeTime = _timeLeft = lifetime;
        _dx = dx;
        _dy = dy;
        _particle = new ParticleData(new Vector3(x, y, z), GetColor(color));
    }

    public override bool Update(double time, double dt) {
        _timeLeft -= dt;
        if (_timeLeft <= 0) return false;

        var delta = (float)(dt / 1000.0);
        _particle.Position.X += _dx * delta;
        _particle.Position.Y += _dy * delta;
        _particle.Position.W = ParticleW(_startingSize) * (float)(_timeLeft / _lifeTime);
        
        var c = _particle.Color;
        c.W = Math.Abs(Alpha - 1f) < 0.001f ? -1f : Alpha;
        _particle.Color = c;

        Map.AddParticle(_particle);
        return true;
    }
}