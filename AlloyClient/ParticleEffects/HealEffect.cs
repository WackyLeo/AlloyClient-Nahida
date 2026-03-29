using AlloyClient.Game.Objects;

namespace AlloyClient.ParticleEffects;

public class HealEffect : ParticleEffect {
    private readonly uint _color;

    private readonly Entity _entity;

    public HealEffect(Entity entity, uint color) {
        _entity = entity;
        _color = color;
    }

    public override bool Update(double time, double dt) {
        _ = _entity;
        _ = _color;
        return false;
    }
}