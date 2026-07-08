using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class RisingFuryEffect : ParticleEffect {
    private readonly Entity _entity;
    private readonly double _durationMs;
    private readonly Vector2 _startRounded;
    private double _elapsed;
    private double _spawnTimer;
    private bool _burstDone;

    public RisingFuryEffect(Entity entity, double durationMs) {
        _entity = entity;
        _durationMs = Math.Max(1.0, durationMs);
        _startRounded = entity == null
            ? Vector2.Zero
            : new Vector2(MathF.Floor(entity.Position.X * 10f) / 10f, MathF.Floor(entity.Position.Y * 10f) / 10f);
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        _position = _entity.Position;
        var nowRounded = new Vector2(MathF.Floor(_position.X * 10f) / 10f, MathF.Floor(_position.Y * 10f) / 10f);
        if (nowRounded != _startRounded)
            return false;

        _elapsed += dt;
        _spawnTimer -= dt;

        while (_spawnTimer <= 0 && _elapsed < _durationMs) {
            var a = Random.Shared.NextSingle() * MathF.Tau;
            var radius = 0.35f + Random.Shared.NextSingle() * 0.55f;
            var start = _position + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            var end = _position + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (radius + 0.15f);
            Map.AddParticleEffect(new SparkEffect(120, 0xFF5A00, 300, 0.5f, start, end));
            _spawnTimer += 33.0;
        }

        if (_elapsed >= _durationMs && !_burstDone) {
            _burstDone = true;
            Map.AddParticleEffect(new BurstEffect(_position, _position + new Vector2(1.4f, 0f), 0xFF5A00, 20));
        }

        return _elapsed < _durationMs + 400.0;
    }
}
