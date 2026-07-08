using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class NecroLineEffect : ParticleEffect {
    private readonly uint _color;
    private readonly Vector2 _start;
    private readonly Entity _target;
    private readonly float _strength;

    public NecroLineEffect(Vector2 start, Entity target, uint color, float strength) {
        _start = start;
        _target = target;
        _color = color;
        _strength = strength;
        _position = start;
    }

    public override bool Update(double time, double dt) {
        if (_target == null || !Map.Entities.ContainsKey(_target.ObjectId))
            return false;

        var end = _target.Position;
        const int count = 20;
        var size = (int)(100 + _strength / 50f);
        for (var i = 0; i < count; i++) {
            var t = i / (float)count;
            var p = Vector2.Lerp(_start, end, t);
            var q = p + new Vector2(Random.Shared.NextSingle() * 2f - 1f, Random.Shared.NextSingle() * 2f - 1f) * 0.2f;
            Map.AddParticleEffect(new SparkEffect(size, _color, 500, 0.5f, p, q));
        }

        return false;
    }
}
