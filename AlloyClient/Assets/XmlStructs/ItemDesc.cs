using System;
using System.Linq;
using System.Xml.Linq;
using AlloyClient.Game.Objects.Util;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public class ItemDesc {
    public readonly ActivateEffectDesc[] ActivateEffects;
    public readonly ProjectileDesc[] AllProjectiles;
    public readonly float ArcGap;
    public readonly bool Backpack;
    public readonly int BagType;
    public readonly string Class;
    public readonly bool Consumable;
    public readonly float Cooldown;
    public readonly string Description;
    public readonly string DisplayId;
    public readonly string DisplayName;
    public readonly bool Div;
    public readonly string Divinity;
    public readonly int Doses;
    public readonly ExtraTooltipDataDesc[] ExtraTooltipData;
    public readonly int FameBonus;
    public readonly bool Ferozium;
    public readonly bool InvUse;
    public readonly bool LDBoosted;
    public readonly bool LTBoosted;
    public readonly int MpCost;
    public readonly int MpEndCost;
    public readonly int NextCount;
    public readonly int NumProjectiles;
    public readonly string ObjectId;
    public readonly ushort ObjectType;
    public readonly string OldSound;
    public readonly bool Potion;
    public readonly ProjectileDesc Projectiles;
    public readonly int Quantity;
    public readonly float RateOfFire;
    public readonly bool Resurrects;
    public readonly string SetName;
    public readonly int SlotType;
    public readonly bool Soulbound;

    public readonly string Sound;
    public readonly bool ST;
    public readonly int StartCount;

    public readonly StatBoostDesc[] StatBoosts;
    public readonly string SuccessorId;
    public readonly int Texture1;
    public readonly int Texture2;
    public readonly int Tier;
    public readonly float Timer;
    public readonly int TributePoints;
    public readonly bool TypeOfConsumable;
    public readonly bool UnHoly;
    public readonly bool Usable;
    public readonly bool XpBoost;

    public ItemDesc(ushort type, XElement xml) {
        ObjectType = type;
        ObjectId = xml.GetAttribute<string>("id");
        Class = xml.GetValue<string>("Class");
        DisplayId = xml.GetValue<string>("DisplayId");
        DisplayName = string.IsNullOrWhiteSpace(DisplayId) ? ObjectId : DisplayId;
        Texture1 = xml.GetValue<int>("Tex1");
        Texture2 = xml.GetValue<int>("Tex2");
        SlotType = xml.GetValue<int>("SlotType");
        Description = xml.GetValue<string>("Description");
        Consumable = xml.HasElement("Consumable");
        Soulbound = xml.HasElement("Soulbound");
        Potion = xml.HasElement("Potion");
        Usable = xml.HasElement("Usable");
        Resurrects = xml.HasElement("Resurrects");
        RateOfFire = xml.GetValue("RateOfFire", 1f);
        if (xml.HasElement("Tier"))
            Tier = xml.GetValue<int>("Tier");
        BagType = xml.GetValue<int>("BagType");
        FameBonus = xml.GetValue<int>("FameBonus");
        NumProjectiles = xml.GetValue("NumProjectiles", 1);
        ArcGap = xml.GetValue("ArcGap", 11.25f);
        MpCost = xml.GetValue<int>("MpCost");
        Cooldown = xml.GetValue("Cooldown", 0.5f);
        Doses = xml.GetValue<int>("Doses");
        Quantity = xml.GetValue<int>("Quantity");
        SuccessorId = xml.GetValue<string>("SuccessorId");
        Backpack = xml.HasElement("Backpack");
        LDBoosted = xml.HasElement("LDBoosted");
        LTBoosted = xml.HasElement("LTBoosted");
        XpBoost = xml.HasElement("XpBoost");
        Timer = xml.GetValue<float>("Timer");
        MpEndCost = xml.GetValue<int>("MpEndCost");
        InvUse = xml.HasElement("InvUse");
        TypeOfConsumable = InvUse || Consumable;

        Sound = xml.GetValue<string>("Sound");
        OldSound = xml.GetValue<string>("OldSound");

        StatBoosts = xml.Elements("ActivateOnEquip").Select(i => new StatBoostDesc(i)).ToArray();
        ActivateEffects = xml.Elements("Activate").Select(i => new ActivateEffectDesc(i)).ToArray();
        Projectiles = xml.HasElement("Projectile") ? new ProjectileDesc(xml.Element("Projectile")) : null;

        StartCount = xml.GetValue<int>("StartCount");
        TributePoints = xml.GetValue<int>("tributePoints");
        Div = xml.HasElement("DIV");
        UnHoly = xml.HasElement("Unholy");
        Ferozium = xml.HasElement("Ferozium");
        ST = xml.HasAttribute("setType");
        SetName = xml.GetValue<string>("setName");
        Divinity = xml.GetValue<string>("Divinity");

        NextCount = xml.GetValue("NextCount", 99999);
        AllProjectiles = xml.Elements("Projectile")
            .Select(p => new ProjectileDesc(p))
            .ToArray();

        ExtraTooltipData = xml.Element("ExtraTooltipData")?.Elements("EffectInfo")
            .Select(e => new ExtraTooltipDataDesc(e))
            .ToArray();
    }
}

public class StatBoostDesc(XElement xml) {
    public int Amount = xml.GetAttribute<int>("amount");
    public int Stat = xml.GetAttribute<int>("stat");
}

public class ProjectileDesc {
    public readonly float Amplitude;
    public readonly bool ArmorPiercing;
    public readonly bool Boomerang;
    public readonly int BulletType;

    public readonly ConditionEffectDesc[] Effects;
    public readonly float Frequency;
    public readonly float LifetimeMS;
    public readonly float Magnitude;
    public readonly int MaxDamage;
    public readonly int MinDamage;
    public readonly bool MultiHit;
    public readonly string ObjectId;
    public readonly bool Parametric;
    public readonly bool PassesCover;
    public readonly float Speed;
    public readonly ParticleTrailDesc Trail;
    public readonly bool Wavy;
    public readonly int NumProjectiles;
    public readonly float RateOfFire;

    public ProjectileDesc(XElement xml) {
        BulletType = xml.GetAttribute<int>("id");
        ObjectId = xml.GetValue<string>("ObjectId");
        LifetimeMS = xml.GetValue<float>("LifetimeMS");
        Speed = xml.GetValue<float>("Speed", 100);
        NumProjectiles = xml.GetValue("NumProjectiles", 0);
        RateOfFire = xml.GetValue("RateOfFire", 0f);

        var dmg = xml.Element("Damage");
        if (dmg != null) {
            MinDamage = MaxDamage = xml.GetValue<int>("Damage");
        } else {
            MinDamage = xml.GetValue<int>("MinDamage");
            MaxDamage = xml.GetValue<int>("MaxDamage");
        }

        Effects = xml.Elements("ConditionEffect")
            .Select(x => new ConditionEffectDesc(x.Value, x.GetAttribute<float>("duration"))).ToArray();

        Trail = xml.HasElement("ParticleTrail") ? new ParticleTrailDesc(xml.Element("ParticleTrail")) : null;

        MultiHit = xml.HasElement("MultiHit");
        PassesCover = xml.HasElement("PassesCover");
        ArmorPiercing = xml.HasElement("ArmorPiercing");
        Wavy = xml.HasElement("Wavy");
        Parametric = xml.HasElement("Parametric");
        Boomerang = xml.HasElement("Boomerang");

        Amplitude = xml.GetValue<float>("Amplitude");
        Frequency = xml.GetValue<float>("Frequency", 1);
        Magnitude = xml.GetValue<float>("Magnitude", 3);
    }
}

public class ConditionEffectDesc {
    public int DurationMS;

    public int EffectId;
    public string EffectName;
    public float Range;

    public ConditionEffectDesc(string eff, float duration, float range = 0f) {
        EffectName = eff;
        EffectId = ConditionEffectUtil.GetConditionEffectId(eff);
        if (duration < 100) // nah, this is crazy
            duration *= 1000;
        DurationMS = (int)duration;
        Range = range;
    }
}

public class ParticleTrailDesc {
    public uint Color;
    public int LifetimeMS;
    public int Size;

    public ParticleTrailDesc(XElement xml) {
        Color = string.IsNullOrEmpty(xml.Value) ? 0 : Convert.ToUInt32(xml.Value, 16);
        LifetimeMS = xml.GetAttribute<int>("lifetimeMS");
        Size = xml.GetAttribute<int>("size");
    }
}

public class ActivateEffectDesc {
    public readonly int Amount;
    public readonly ConditionEffectIndex CheckExistingEffect;
    public readonly uint? Color;
    public readonly ConditionEffectIndex ConditionEffect;
    public readonly float Cooldown;
    public readonly string DungeonName;
    public readonly int DurationMS;
    public readonly float DurationSec;
    public readonly string Effect;
    public readonly float EffectDuration;
    public readonly string Id;
    public readonly string LockedName;
    public readonly float MaximumDistance;
    public readonly int MaxTargets;
    public readonly string ObjectId;
    public readonly float Radius;
    public readonly float Range;
    public readonly bool RemoveSelf;
    public readonly int Stats;

    public readonly int TotalDamage;

    public ActivateEffectDesc(XElement xml) {
        Effect = xml.Value;

        if (xml.HasAttribute("effect"))
            Enum.TryParse(xml.GetAttribute<string>("effect"), out ConditionEffect);

        if (xml.HasAttribute("condEffect"))
            Enum.TryParse(xml.GetAttribute<string>("condEffect"), out ConditionEffect);

        if (xml.HasAttribute("checkExistingEffect"))
            Enum.TryParse(xml.GetAttribute<string>("checkExistingEffect"), out CheckExistingEffect);

        if (xml.HasAttribute("color")) Color = xml.GetAttribute<uint>("color");

        TotalDamage = xml.GetAttribute<int>("totalDamage");
        Radius = xml.GetAttribute<float>("radius");
        EffectDuration = xml.GetAttribute<float>("condDuration");
        DurationSec = xml.GetAttribute<float>("duration");
        DurationMS = (int)(DurationSec * 1000.0f);
        Amount = xml.GetAttribute<int>("amount");
        Range = xml.GetAttribute<float>("range");
        ObjectId = xml.GetAttribute<string>("objectId");
        Id = xml.GetAttribute<string>("id");
        MaximumDistance = xml.GetAttribute<float>("maxDistance");
        MaxTargets = xml.GetAttribute<int>("maxTargets");
        Stats = xml.GetAttribute<int>("stat");
        Cooldown = xml.GetAttribute<float>("cooldown");
        RemoveSelf = xml.GetAttribute<bool>("removeSelf");
        DungeonName = xml.GetAttribute<string>("dungeonName");
        LockedName = xml.GetAttribute<string>("lockedName");
    }
}

public class ExtraTooltipDataDesc {
    public readonly string Description;
    public readonly string Name;

    public ExtraTooltipDataDesc(XElement xml) {
        Name = xml.GetAttribute<string>("name");
        Description = xml.GetAttribute<string>("description");
    }
}