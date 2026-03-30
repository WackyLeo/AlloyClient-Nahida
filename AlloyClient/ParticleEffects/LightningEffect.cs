using System;
using AlloyClient.Game;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class LightningEffect : ParticleEffect {
    private readonly uint _color;
    private readonly Vector2 _end;
    private readonly float _lifetimeMultiplier;
    private readonly int _particleSize;
    private readonly Vector2 _start;

    public LightningEffect(Vector2 source, Vector2 target, uint color,
        int particleSize = 100, float lifetimeMultiplier = 1f) {
        _start = source;
        _position = source;
        _end = target;
        _color = color;
        _particleSize = particleSize;
        _lifetimeMultiplier = lifetimeMultiplier;
    }

    public override bool Update(double time, double dt) {
        const float qualityScale = 0.4f;
        const int rayCount = 3;

        var dist = Vector2.Distance(_start, _end);
        var totalSteps = (int)(dist * rayCount);
        var scaledSize = (int)(_particleSize * qualityScale);

        for (var step = 0; step < totalSteps; step++) {
            var frac = step / (float)totalSteps;
            var pos = Vector2.Lerp(_start, _end, frac);

            var lifeMs = (int)(1000f * qualityScale * _lifetimeMultiplier -
                               frac * 900f * qualityScale * _lifetimeMultiplier);

            var midDist = Math.Min(step, totalSteps - step);
            var maxJitter = dist / 200f * midDist;
            var jitterX = (Random.Shared.NextSingle() * 2f - 1f) * maxJitter;
            var jitterY = (Random.Shared.NextSingle() * 2f - 1f) * maxJitter;

            var jitteredPos = new Vector2(pos.X + jitterX, pos.Y + jitterY);
            Map.AddParticleEffect(new SparkEffect(
                scaledSize, _color, lifeMs, 0.5f,
                jitteredPos, jitteredPos // start == end → dx=dy=0, pure fade
            ));
        }

        return false;
    }
}