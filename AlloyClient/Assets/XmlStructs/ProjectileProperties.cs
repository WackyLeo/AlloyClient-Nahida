using System;
using System.Linq;
using System.Xml.Linq;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public sealed class ProjectileProperties {
    public readonly float Acceleration;
    public readonly float AccelerationDelay;
    public readonly int AltAnimation;

    public readonly bool HasAcceleration;
    public readonly float Amplitude;
    public readonly bool Animated;
    public readonly int AnimationTime;
    public readonly int SplitAmount;
    public readonly bool Split;
    public readonly bool ArmorPiercing;
    public readonly bool Asyncrone;
    public readonly float BonusSize;
    public readonly bool Boomerang;
    public readonly int BulletType;
    public readonly bool CheckRayCast;
    public readonly bool CircularAngle;
    public readonly bool OneWayFrequency;
    public readonly int HitAmount;

    public readonly bool DynamicAngle;
    public readonly bool DynamicRotationAngle;
    public readonly ConditionEffectDesc[] Effects;
    public readonly int Fade;
    public readonly bool Falldown;
    public readonly float FixedRotationAngle;
    public readonly bool ForceHit;
    public readonly bool ForceOrbiting;
    public readonly float Frequency;
    public readonly float LifetimeMs;
    public readonly float Magnitude;
    public readonly float MaxAcceleration;
    public readonly int MaxDamage;
    public readonly float MaxRotationRadius;
    public readonly float MinAcceleration;
    public readonly int MinDamage;
    public readonly int Penetration;
    public readonly bool Missile;
    public readonly bool MultiHit;
    public readonly bool NoAlt;
    public readonly bool NoRotation;
    public readonly string ObjectId;
    public readonly bool OnlyClockwise;
    public readonly bool OnlyCounterClockwise;
    public readonly bool Orbiting;
    public readonly bool Parametric;
    public readonly bool ParticleTrail;
    public readonly int NumProjectiles;

    public readonly uint ParticleTrailColor;
    public readonly float ParticleTrailIntensity;
    public readonly int ParticleTrailLifetime;
    public readonly bool PassAll;
    public readonly bool PassesCover;
    public readonly int ProjDelayMs;
    public readonly float RadiusIncreaseSpeed;
    public readonly int OrbitDelay;
    
    public readonly bool RayCast;
    public readonly int AlphaDecay;

    public readonly int RCLifetimeMs;
    public readonly int RCSpeed;
    public readonly float RealSpeed;
    public readonly bool ReturnRayCast;
    public readonly int Ricochet;
    public readonly float RotationSpeed;
    

    public readonly string SecondId;
    public readonly int ShadowSize;
    public readonly int Size;
    public readonly float Speed;
    public readonly float StartFrequency;

    public readonly float StartRotationRadius;

    public readonly float StaticFallAngle;
    public readonly bool VanillaAngle;
    public readonly bool Wavy;

    public ProjectileProperties(XElement e) {
        BulletType = e.GetAttribute<int>("id");
        ObjectId = e.GetValue<string>("ObjectId");
        LifetimeMs = e.GetValue<float>("LifetimeMS");
        RealSpeed = e.GetValue<float>("Speed");
        Speed = RealSpeed / 10000f;
        Size = e.GetValue("Size", -1);
        ShadowSize = e.GetValue("ShadowSize", -1);
        MinDamage = e.HasElement("Damage") ? e.GetValue<int>("Damage") : e.GetValue<int>("MinDamage");
        MaxDamage = e.HasElement("Damage") ? e.GetValue<int>("Damage") : e.GetValue<int>("MaxDamage");
        Penetration = e.HasElement("Penetration") ? e.GetValue<int>("Penetration") : 0;
        MultiHit = e.HasElement("MultiHit");
        PassesCover = e.HasElement("PassesCover");
        ArmorPiercing = e.HasElement("ArmorPiercing");
        ParticleTrail = e.HasElement("ParticleTrail");
        HasAcceleration = e.HasElement("Acceleration");
        Wavy = e.HasElement("Wavy");
        Parametric = e.HasElement("Parametric");
        Boomerang = e.HasElement("Boomerang");
        NoRotation = e.HasElement("NoRotation");
        PassAll = e.HasElement("PassAll");
        Asyncrone = e.HasElement("Asyncrone");
        NoAlt = e.HasElement("NoAlt");
        Orbiting = e.HasElement("Orbit");
        Falldown = e.HasElement("Falldown");
        Missile = e.HasElement("Missile");
        OneWayFrequency = e.HasElement("OneWayFrequency");
        NumProjectiles = e.GetValue("NumProjectiles", 0);
        HitAmount = e.GetValue("HitAmount", 1);

        Amplitude = e.GetValue<float>("Amplitude");
        Frequency = e.GetValue("Frequency", 1f);
        StartFrequency = e.GetValue<float>("StartFrequency");
        Magnitude = e.GetValue("Magnitude", 3f);
        Fade = e.GetValue<int>("Fade");
        
        if (HasAcceleration)
        {
            var attr = e.Element("Acceleration");
            Acceleration = e.GetValue<float>("Acceleration");
            AccelerationDelay = attr.GetAttribute<float>("delay");
            MaxAcceleration = attr.GetAttribute("max", 100000f);
            MinAcceleration = attr.GetAttribute<float>("min");
        }

        if (Orbiting)
        {
            var attr = e.Element("Orbit");
            StartRotationRadius = attr.GetAttribute<float>("startRadius");
            MaxRotationRadius = attr.GetAttribute<float>("maxRadius");
            RotationSpeed = attr.GetAttribute<float>("speed");
            RadiusIncreaseSpeed = attr.GetAttribute<float>("increaseSpeed");
            FixedRotationAngle = attr.GetAttribute<float>("startAngle");
            OnlyClockwise = attr.GetAttribute<bool>("clockwise");
            OnlyCounterClockwise = attr.GetAttribute<bool>("counter");
            OrbitDelay = attr.GetAttribute<int>("delay");
        }
        ForceOrbiting = e.HasElement("OwnerLock");
        DynamicRotationAngle = e.HasElement("DynamicRotationAngle");

        StaticFallAngle = e.GetValue("StaticFallAngle", -10000f);
        ProjDelayMs = e.GetValue<int>("ProjDelay");
        Ricochet = e.GetValue<int>("Ricochet");

        if (ParticleTrail)
        {
            var attr = e.Element("ParticleTrail");
            ParticleTrailColor = string.IsNullOrEmpty(attr?.Value) ? 0xFF00FF : Convert.ToUInt32(attr.Value, 16);
            ParticleTrailLifetime = attr.GetAttribute("lifetimeMS", 600);
            ParticleTrailIntensity = attr.GetAttribute("intensity", 0.3f);
        }

        RCLifetimeMs = e.GetValue<int>("RCLifetimeMS");
        RCSpeed = e.GetValue<int>("RCSpeed");
        ReturnRayCast = e.HasElement("ReturnRC");

        DynamicAngle = e.HasElement("DynamicAngle");
        Animated = e.HasElement("Animated");
        AnimationTime = e.GetValue<int>("AnimationTime");
        AltAnimation = e.GetValue<int>("AltAnimation");
        BonusSize = e.GetValue<float>("BonusSize");
        RayCast = e.HasElement("RayCast");
        if (RayCast)
        {
            var attr = e.Element("RayCast");
            AlphaDecay = attr.GetAttribute("alphaDecay", 10);
        }
        CheckRayCast = e.HasElement("CheckRC");

        SecondId = e.GetValue<string>("SecondID");
        SplitAmount = e.GetValue<int>("ArcAmount");
        Split = e.HasElement("ArcBlade");
        CircularAngle = e.HasElement("CircularAngle");
        ForceHit = e.HasElement("ForceHit");

        Effects = e.Elements("ConditionEffect")
                   .Select(x => new ConditionEffectDesc(x.Value, x.GetAttribute<float>("duration"))).ToArray();

        VanillaAngle = !Orbiting && !DynamicAngle;
    }
}