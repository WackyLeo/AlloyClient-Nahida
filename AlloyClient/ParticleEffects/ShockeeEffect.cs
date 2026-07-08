using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class ShockeeEffect : ParticleEffect {
    private readonly Entity _entity;
    private double _elapsed;
    private double _timer;

    public ShockeeEffect(Entity entity) {
        _entity = entity;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        _elapsed += dt;
        _timer -= dt;
        _position = _entity.Position;

        while (_timer <= 0 && _elapsed <= 600.0) {
            var a = Random.Shared.NextSingle() * MathF.Tau;
            var b = a + MathF.PI + (Random.Shared.NextSingle() - 0.5f) * 0.6f;
            var start = _position + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 0.55f;
            var end = _position + new Vector2(MathF.Cos(b), MathF.Sin(b)) * 0.55f;
            Map.AddParticleEffect(new LightningEffect(start, end, 0xFFFFFF, 45, 0.12f));
            _timer += 50.0;
        }

        return _elapsed <= 600.0;
    }
}
