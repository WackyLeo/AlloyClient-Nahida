using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class ConeBlastEffect : ParticleEffect {
    private readonly float _blastRadius;
    private readonly uint _color;
    private readonly int _particleCount;
    private readonly Vector2 _start;
    private readonly Vector2 _target;

    public ConeBlastEffect(Entity entity, Vector2 target, float blastRadius, uint color, int particleCount = 7) {
        _start = entity?.Position ?? target;
        _target = target;
        _blastRadius = blastRadius;
        _color = color;
        _particleCount = Math.Max(1, particleCount);
        _position = _start;
    }

    public override bool Update(double time, double dt) {
        const float arc = MathF.PI / 3f;
        var baseAngle = MathF.Atan2(_target.Y - _start.Y, _target.X - _start.X);

        for (var i = 0; i < _particleCount; i++) {
            var angle = baseAngle - arc * 0.5f + i * arc / _particleCount;
            var end = new Vector2(
                _start.X + _blastRadius * MathF.Cos(angle),
                _start.Y + _blastRadius * MathF.Sin(angle)
            );

            Map.AddParticleEffect(new SparkerEffect(200, _color, 100, 0.5f, _start, end));
        }

        return false;
    }
}
