using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public abstract class ParticleEffect {
    protected Vector2 _position;

    public void SetEntityPosition(Vector2 pos) {
        _position = pos;
    }

    public abstract bool Update(double time, double dt);

    protected Vector4 GetColor(uint rgb) {
        var r = (byte)(rgb >> 16);
        var g = (byte)(rgb >> 8);
        var b = (byte)rgb;
        return new Vector4(r / 255f, g / 255f, b / 255f, -1);
    }

    public static ParticleEffect FromProperties(EffectProperties properties, Entity entity) {
        return properties.Id switch {
            "Healing" => new HealingEffect(entity),
            "Fountain" => new FountainEffect(),
            "BlackFountain" => new BlackFountainEffect(),
            "FountainSnowy" => new FountainSnowyEffect(),
            "SkyBeam" => null,
            "Circle" => new CircleEffect(entity, properties),
            "Heart" => null,
            "Gas" => new GasEffect(entity, properties),
            "Vent" => null,
            "Bubbles" => new BubbleEffect(entity, properties),
            "Vortex" => null,
            //"XMLEffect" => new XMLEffect(entity, properties),
            "CustomParticles" => new ParticleGenerator(entity, properties),
            _ => null
        };
    }

    protected static float ParticleW(int as3Size) {
        return as3Size / 100f;
    }
}