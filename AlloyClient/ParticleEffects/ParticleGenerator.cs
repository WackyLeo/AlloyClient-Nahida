using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class ParticleGenerator : ParticleEffect {
    private const int Buffer = 2000;

    private readonly Vector4 _color;
    private readonly BaseParticle[] _data = new BaseParticle[Buffer];
    private readonly Entity _parent;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];

    private readonly EffectProperties _properties;
    private readonly AtlasData _texture;
    private double spawnRemainder;

    public ParticleGenerator(Entity parent, EffectProperties props) {
        _properties = props;
        _parent = parent;
    }

    public override bool Update(double time, double dt) {
        if (!Map.Entities.ContainsValue(_parent)) return false;

        _position.X = _parent.Position.X;
        _position.Y = _parent.Position.Y;

        var adjustedRate = _properties.Rate;
        var adjustedLifeBase = _properties.Life;
        var adjustedLifeVar = _properties.LifeVariance;

        var desiredParticles = adjustedRate;
        spawnRemainder = 0;

        for (var i = 0; i < desiredParticles; i++) {
            var data = _data[i];
            ref var part = ref _particles[i];
        }

        Map.AddParticles(_particles, (int)desiredParticles);
        return true;
    }
}