using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;

namespace AlloyClient.ParticleEffects;

public class XMLEffect : ParticleEffect {
    private readonly float _baseCooldown;

    private readonly Entity _entity;
    private readonly EffectProperties _props;
    private float _cooldown;
    private float _cooldownLeft;

    public XMLEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _props = props;

        _baseCooldown = props.Cooldown;
        _cooldownLeft = 0;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null) return false;

        _position = _entity.Position;
        _cooldown = _baseCooldown;

        var dtSec = (float)(dt / 1000.0);
        _cooldownLeft -= dtSec;

        if (_cooldownLeft <= 0) {
            _cooldownLeft = _cooldown;
            Map.AddParticleEffect(new XMLParticle(_position, _props));
        }

        return true;
    }
}