using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class ShockerEffect : ParticleEffect {
    private readonly Entity _entity;
    private readonly float _innerRadius;
    private readonly float _outerRadius;
    private readonly int _particleSize;
    private double _elapsed;
    private double _timer;

    public ShockerEffect(Entity entity) {
        _entity = entity;
        var big = entity != null && entity.Texture.RawH() > 8;
        _innerRadius = big ? 0.7f : 0.4f;
        _outerRadius = big ? 2f : 1.3f;
        _particleSize = big ? 40 : 30;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        _elapsed += dt;
        _timer -= dt;
        _position = _entity.Position;

        while (_timer <= 0 && _elapsed <= 2000.0) {
            var radians = Random.Shared.NextSingle() * MathF.Tau;
            var start = _position + new Vector2(MathF.Sin(radians), MathF.Cos(radians)) * _innerRadius;
            var end = _position + new Vector2(MathF.Sin(radians), MathF.Cos(radians)) * _outerRadius;
            Map.AddParticleEffect(new LightningEffect(start, end, 0xFFFFFF, _particleSize, 0.22f));
            _timer += 200.0;
        }

        return _elapsed <= 2000.0;
    }
}
