using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class PoisonEffect : ParticleEffect {
    private const int ParticleQuality = 2;
    private readonly uint _color;

    private readonly Entity _entity;

    public PoisonEffect(Entity entity, uint color) {
        _entity = entity;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null) return false;

        _position = _entity.Position;

        var count = ParticleQuality switch {
            0 => 5,
            1 => 7,
            _ => 10
        };

        for (var i = 0; i < count; i++) {
            var dx = Random.Shared.NextSingle() * 8f - 4f;
            var dy = Random.Shared.NextSingle() * 8f - 4f;
            var end = _position + new Vector2(dx, dy) * 0.4f;
            Map.AddParticleEffect(new SparkEffect(
                50,
                _color,
                400,
                0.75f,
                _position,
                end
            ));
        }

        return false;
    }
}