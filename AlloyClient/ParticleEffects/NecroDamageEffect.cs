using System;
using AlloyClient.Game;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class NecroDamageEffect : ParticleEffect {
    private readonly uint _color;
    private readonly float _radius;

    public NecroDamageEffect(Vector2 position, uint color, float radius) {
        _position = position;
        _color = color;
        _radius = Math.Max(0.5f, radius);
    }

    public override bool Update(double time, double dt) {
        var count = Math.Max(8, (int)(_radius * 8f));
        for (var i = 0; i < count; i++) {
            var a = i * MathF.Tau / count;
            var end = _position + new Vector2(MathF.Cos(a), MathF.Sin(a)) * _radius;
            Map.AddParticleEffect(new SparkEffect(130, _color, 450, 0.6f, _position, end));
        }

        return false;
    }
}
