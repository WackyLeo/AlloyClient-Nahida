using System;
using AlloyClient.Game;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class CollapseEffect : ParticleEffect {
    private readonly Vector2 _center;
    private readonly uint _color;
    private readonly Vector2 _edge;
    private readonly int _particleCount;

    public CollapseEffect(Vector2 center, Vector2 edge, uint color, int particleCount = 24) {
        _center = center;
        _edge = edge;
        _position = center;
        _color = color;
        _particleCount = Math.Max(1, particleCount);
    }

    public override bool Update(double time, double dt) {
        var radius = Vector2.Distance(_center, _edge);

        for (var i = 0; i < _particleCount; i++) {
            var angle = i * MathF.Tau / _particleCount;
            var start = new Vector2(
                _center.X + radius * MathF.Cos(angle),
                _center.Y + radius * MathF.Sin(angle)
            );

            Map.AddParticleEffect(new SparkerEffect(300, _color, 200, 0.5f, start, _center));
        }

        return false;
    }
}
