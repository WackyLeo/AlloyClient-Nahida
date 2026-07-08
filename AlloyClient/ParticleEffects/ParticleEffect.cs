using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Objects;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public interface IDrawableParticleEffect {
    void Draw();
}

public abstract class ParticleEffect {
    protected Vector2 _position;
    public float Alpha = 1f;

    public void SetEntityPosition(Vector2 pos) {
        _position = pos;
    }

    public abstract bool Update(double time, double dt);

    protected Vector4 GetColor(uint rgb, float alpha = 1f) {
        var r = (byte)(rgb >> 16);
        var g = (byte)(rgb >> 8);
        var b = (byte)rgb;
        return new Vector4(r / 255f, g / 255f, b / 255f, -alpha);
    }

    protected static uint RandomColor(uint color1, uint color2) {
        if (color2 == 0 || color1 == color2)
            return color1;

        var r1 = (int)((color1 >> 16) & 0xFF);
        var g1 = (int)((color1 >> 8) & 0xFF);
        var b1 = (int)(color1 & 0xFF);

        var r2 = (int)((color2 >> 16) & 0xFF);
        var g2 = (int)((color2 >> 8) & 0xFF);
        var b2 = (int)(color2 & 0xFF);

        var t = Random.Shared.NextSingle();
        var r = (uint)(r1 + (r2 - r1) * t);
        var g = (uint)(g1 + (g2 - g1) * t);
        var b = (uint)(b1 + (b2 - b1) * t);

        return (r << 16) | (g << 8) | b;
    }

    public static ParticleEffect FromProperties(EffectProperties properties, Entity entity) {
        return properties.Id switch {
            "Healing" => new HealingEffect(entity),
            "Fountain" => new FountainEffect(properties.Color == 0 ? 0x4165D5u : properties.Color),
            "BlackFountain" => new BlackFountainEffect(),
            "FountainSnowy" => new FountainSnowyEffect(),
            "SkyBeam" => new SkyBeamEffect(entity, properties),
            "Circle" => new CircleEffect(entity, properties),
            "Diffuse" => new BurstEffect(entity.Position, entity.Position + new Vector2(properties.MaxRadius, properties.MaxRadius), properties.Color, Math.Max(1, (int)(properties.MaxRadius * 2f))),
            "Heart" => new HeartEffect(entity, properties),
            "Gas" => new GasEffect(entity, properties),
            "Vent" => new VentEffect(entity),
            "Bubbles" => new BubbleEffect(entity, properties),
            "Vortex" => new VortexEffect(entity, properties),
            "XMLEffect" => new XMLEffect(entity, properties),
            "CustomParticles" => new ParticleGenerator(entity, properties),
            _ => null
        };
    }

    protected static float ParticleW(int as3Size) {
        return as3Size / 100f;
    }
}
