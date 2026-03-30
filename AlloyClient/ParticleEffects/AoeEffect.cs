using System;
using AlloyClient.Game;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class AoeEffect : ParticleEffect {
    private readonly uint _color;
    private readonly float _novaRadius;

    public AoeEffect(Vector2 center, float novaRadius, uint color) {
        _position = center;
        _novaRadius = novaRadius;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        var size = 200;
        var lifetime = 200 * 6;
        var count = (int)(6 + _novaRadius * 2);

        for (var i = 0; i < count; i++) {
            var angle = i * 2 * MathF.PI / count;
            var end = new Vector2(
                _position.X + _novaRadius * MathF.Cos(angle),
                _position.Y + _novaRadius * MathF.Sin(angle)
            );

            Map.AddParticleEffect(new SparkerEffect(size, _color, lifetime, 0.5f, _position, end));
        }

        return false;
    }
}