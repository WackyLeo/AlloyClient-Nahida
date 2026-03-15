using System;
using System.Xml.Linq;
using Common;

public sealed class ProjectileProperties {
    public readonly int BulletType;
    public readonly string ObjectId;
    public readonly float LifetimeMs;
    public readonly float Speed;
    public readonly float RealSpeed;
    public readonly int Size;
    public readonly int MinDamage;
    public readonly int MaxDamage;
    public readonly bool MultiHit;
    public readonly bool PassesCover;
    public readonly bool ArmorPiercing;
    public readonly bool ParticleTrail;
    public readonly bool Wavy;
    public readonly bool Parametric;
    public readonly bool Boomerang;
    public readonly bool NoRotation;
    public readonly bool PassAll;
    public readonly bool Asyncrone;
    public readonly bool NoAlt;
    public readonly bool Orbiting;
    public readonly bool OnlyClockwise;
    public readonly bool OnlyCounterClockwise;
    public readonly bool DynamicRotationAngle;
    public readonly bool Falldown;
    public readonly bool Missile;

    public readonly float Amplitude;
    public readonly float Frequency;
    public readonly float StartFrequency;
    public readonly float Magnitude;

    public readonly float Acceleration;
    public readonly float AccelerationDelay;
    public readonly float MaxAcceleration;
    public readonly float MinAcceleration;

    public readonly float StartRotationRadius;
    public readonly float MaxRotationRadius;
    public readonly float RotationSpeed;
    public readonly float RadiusIncreaseSpeed;
    public readonly float FixedRotationAngle;

    public readonly float StaticFallAngle;
    public readonly int ProjDelayMs;
    public readonly int Ricochet;

    public readonly uint ParticleTrailColor;
    public readonly int ParticleTrailLifetime;
    public readonly float ParticleTrailIntensity;

    public readonly bool DynamicAngle; 
    public readonly bool Animated;
    public readonly int AnimationTime;
    public readonly int AltAnimation;
    public readonly float BonusSize;
    public readonly bool RayCast;
    public readonly bool CheckRayCast;
    
    public ProjectileProperties(XElement e) {
        BulletType = e.GetAttribute<int>("id");
        ObjectId = e.GetValue<string>("ObjectId");
        LifetimeMs = e.GetValue<float>("LifetimeMS");
        RealSpeed = e.GetValue<float>("Speed");
        Speed = RealSpeed / 10000f;
        Size = e.GetValue<int>("Size", -1);
        MinDamage = e.HasElement("Damage") ? e.GetValue<int>("Damage") : e.GetValue<int>("MinDamage");
        MaxDamage = e.HasElement("Damage") ? e.GetValue<int>("Damage") : e.GetValue<int>("MaxDamage");
        MultiHit = e.HasElement("MultiHit");
        PassesCover = e.HasElement("PassesCover");
        ArmorPiercing = e.HasElement("ArmorPiercing");
        ParticleTrail = e.HasElement("ParticleTrail");
        Wavy = e.HasElement("Wavy");
        Parametric = e.HasElement("Parametric");
        Boomerang = e.HasElement("Boomerang");
        NoRotation = e.HasElement("NoRotation");
        PassAll = e.HasElement("PassAll");
        Asyncrone = e.HasElement("Asyncrone");
        NoAlt = e.HasElement("NoAlt");
        Orbiting = e.HasElement("Orbiting");
        OnlyClockwise = e.HasElement("OnlyClockwise");
        OnlyCounterClockwise = e.HasElement("OnlyCounterClockwise");
        DynamicRotationAngle = e.HasElement("DynamicRotationAngle");
        Falldown = e.HasElement("Falldown");
        Missile = e.HasElement("Missile");

        Amplitude = e.GetValue<float>("Amplitude");
        Frequency = e.GetValue<float>("Frequency", 1f);
        StartFrequency = e.GetValue<float>("StartFrequency", 0f);
        Magnitude = e.GetValue<float>("Magnitude", 3f);

        Acceleration = e.GetValue<float>("Acceleration", 0f);
        AccelerationDelay = e.GetValue<float>("AccelerationDelay", 0f);
        MaxAcceleration = e.GetValue<float>("MaxAcceleration", 100000f);
        MinAcceleration = e.GetValue<float>("MinAcceleration", 0f);

        StartRotationRadius = e.GetValue<float>("StartRotationRadius", 0f);
        MaxRotationRadius = e.GetValue<float>("MaxRotationRadius", 0f);
        RotationSpeed = e.GetValue<float>("RotationSpeed", 0f);
        RadiusIncreaseSpeed = e.GetValue<float>("RadiusIncreaseSpeed", 0f);
        FixedRotationAngle = e.GetValue<float>("FixedRotationAngle", 0f);

        StaticFallAngle = e.GetValue<float>("StaticFallAngle", -10000f);
        ProjDelayMs = e.GetValue<int>("ProjDelayMS", 0);
        Ricochet = e.GetValue<int>("Ricochet", 0);

        if (ParticleTrail) {
            var attr = e.Element("ParticleTrail");
            ParticleTrailColor = string.IsNullOrEmpty(attr?.Value) ? 0xFF00FF : Convert.ToUInt32(attr.Value, 16);
            ParticleTrailLifetime = attr.GetAttribute("lifetimeMS", 600);
            ParticleTrailIntensity = attr.GetAttribute("intensity", 0.3f);
        }
        
        DynamicAngle = e.HasElement("DynamicAngle");
        Animated = e.HasElement("Animated");
        AnimationTime = e.GetValue<int>("AnimationTime", 0);
        AltAnimation = e.GetValue<int>("AltAnimation", 0);
        BonusSize = e.GetValue<float>("BonusSize", 0f);
        RayCast = e.HasElement("RayCast");
        CheckRayCast = e.HasElement("CheckRC");
    }
}