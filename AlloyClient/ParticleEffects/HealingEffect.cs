using AlloyClient.Game.Objects;

namespace AlloyClient.ParticleEffects;

public class HealingEffect : ParticleEffect {
    private readonly Entity _entity;
    private double _spawnRemainder;

    public HealingEffect(Entity entity) {
        _entity = entity;
    }

    public override bool Update(double time, double dt) {
        _ = _entity;
        _ = _spawnRemainder;
        return true;
    }
}