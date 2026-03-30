using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class LineEffect : ParticleEffect {
    private const int ParticleQuality = 2;
    private readonly uint _color;
    private readonly Vector2 _end;

    private readonly Vector2 _start;

    public LineEffect(Entity entity, Vector2 endPos, uint color) {
        _start = entity?.Position ?? Vector2.Zero;
        _end = endPos;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        var count = ParticleQuality switch {
            0 => 5,
            1 => 15,
            _ => 30
        };

        for (var i = 0; i < count; i++) {
            var t = i / (float)count;
            var pos = Vector2.Lerp(_start, _end, t);

            var dx = Random.Shared.NextSingle() * 2f - 1f;
            var dy = Random.Shared.NextSingle() * 2f - 1f;

            var sparkEnd = pos + new Vector2(dx, dy) * 0.2f;
            Map.AddParticleEffect(new SparkEffect(
                100,
                _color,
                200,
                0.5f,
                pos,
                sparkEnd
            ));
        }

        return false;
    }
}