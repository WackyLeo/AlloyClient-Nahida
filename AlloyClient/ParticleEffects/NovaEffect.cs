using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class NovaEffect : ParticleEffect {
    private const int ParticleQuality = 0;
    private readonly uint _color;

    private readonly float _novaRadius;

    public NovaEffect(Entity entity, float novaRadius, uint color) {
        _position = entity?.Position ?? Vector2.Zero;
        _novaRadius = novaRadius;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        const float qualityScale = 0.75f;
        const int baseSize = 100;
        const int baseLife = 200;

        var particleCount = (int)((4 + _novaRadius * 2) * qualityScale);

        for (var i = 0; i < particleCount; i++) {
            var angle = i * 2f * MathF.PI / particleCount;
            var end = new Vector2(
                _position.X + _novaRadius * MathF.Cos(angle),
                _position.Y + _novaRadius * MathF.Sin(angle)
            );

            Map.AddParticleEffect(new SparkerEffect(baseSize, _color, baseLife, 0.5f, _position, end));
        }

        return false;
    }
}