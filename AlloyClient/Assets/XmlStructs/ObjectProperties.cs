using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Common;
using Common.Structs;

namespace AlloyClient.Assets.XmlStructs;

public class ObjectProperties {
    public readonly bool AlwaysLoaded;
    public readonly float AngleCorrection;
    public readonly float ArcGap;

    public readonly float BaseIndent;

    public readonly string Class;

    public readonly bool Container;
    public readonly AtlasData CrossTexture;
    public readonly string DeathSound;
    public readonly int Defense;

    public readonly string Description;

    public readonly string DisplayId;
    public readonly string DisplayName;

    public readonly AtlasData DotTexture;
    public readonly bool DrawOnGround;
    public readonly string Effect;
    public readonly EffectProperties EffectProperties;

    public readonly bool EnemyOccupySquare;
    public readonly List<ushort?> Equipment;
    public readonly bool FullOccupy;
    public readonly float Height;
    public readonly string HitSound;
    public readonly bool IsAlly;
    public readonly bool IsEnemy;

    public readonly bool IsPlayer;
    public readonly bool IsQuest;
    public readonly AtlasData LineTexture;
    public readonly bool LockedPortal;
    public readonly AtlasData LTexture;
    public readonly int MaxSize;
    public readonly int MinSize;
    public readonly string Model;
    public readonly bool NoMiniMap;
    public readonly int NumProjectiles;
    public readonly string ObjectId;
    public readonly ushort ObjectType;
    public readonly bool OccupySquare;
    public readonly string OldSound;

    public readonly ushort PlayerClassType;
    public readonly PlayerProperties PlayerProperties;

    public readonly List<ProjectileProperties> Projectiles;
    public readonly Dictionary<int, ProjectileProperties> ProjectilesDict;
    public readonly bool ProtectFromGroundDamage;
    public readonly bool ProtectFromSink;
    public readonly float Rotation;
    public readonly int ShadowSize;
    public readonly AtlasData ShortLineTexture;
    public readonly bool ShowName;

    public readonly int Size;
    public readonly int SizeStep = 5;
    public readonly bool Skin;

    public readonly List<int> SlotTypes;

    public readonly string Sound;
    public readonly bool Static;
    public readonly float TopIndent;
    public readonly AtlasData TTexture;


    public ObjectProperties(XElement e) {
        ObjectType = e.GetAttribute<ushort>("type");
        ObjectId = e.GetAttribute<string>("id");

        DisplayId = e.GetValue<string>("DisplayId");
        DisplayName = string.IsNullOrWhiteSpace(DisplayId) ? ObjectId : DisplayId;

        Projectiles = [];
        ProjectilesDict = [];
        foreach (var proj in e.Elements("Projectile")) {
            var props = new ProjectileProperties(proj);
            Projectiles.Add(props);
            ProjectilesDict.TryAdd(props.BulletType, props);
        }

        Sound = e.GetValue<string>("Sound");
        OldSound = e.GetValue<string>("OldSound");
        HitSound = e.GetValue<string>("HitSound");
        DeathSound = e.GetValue<string>("DeathSound");

        NumProjectiles = e.GetValue("NumProjectiles", 1);
        ArcGap = e.GetValue("ArcGap", 11.25f);

        Class = e.GetValue<string>("Class");
        ShowName = e.HasElement("ShowName");
        Model = e.GetValue<string>("Model");
        Effect = e.GetValue<string>("Effect");
        Rotation = e.GetValue<float>("Rotation");
        AngleCorrection = e.GetValue<float>("AngleCorrection");

        IsPlayer = e.GetValue<bool>("Player");
        IsEnemy = e.GetValue<bool>("Enemy");
        IsQuest = e.GetValue<bool>("Quest");
        IsAlly = e.GetValue<bool>("Ally");

        if (IsPlayer) PlayerProperties = new PlayerProperties(e);

        EnemyOccupySquare = e.GetValue<bool>("EnemyOccupySquare");
        OccupySquare = e.GetValue<bool>("OccupySquare");
        FullOccupy = e.GetValue<bool>("FullOccupy");

        Static = e.GetValue<bool>("Static");
        AlwaysLoaded = e.GetValue<bool>("AlwaysLoaded");
        NoMiniMap = e.GetValue<bool>("NoMiniMap");
        DrawOnGround = e.GetValue<bool>("DrawOnGround");
        ProtectFromGroundDamage = e.HasElement("ProtectFromGroundDamage");
        ProtectFromSink = e.HasElement("ProtectFromSink");

        Size = e.GetValue("Size", -1);
        ShadowSize = e.GetValue("ShadowSize", 100);
        MinSize = e.GetValue<int>("MinSize");
        MaxSize = e.GetValue<int>("MaxSize");
        SizeStep = e.GetValue("SizeStep", 5);
        Defense = e.GetValue<int>("Defense");

        Description = e.GetValue<string>("Description");

        if (e.HasElement("Effect")) EffectProperties = new EffectProperties(e.Element("Effect"));

        PlayerClassType = e.GetValue<ushort>("PlayerClassType");
        Skin = e.GetValue<bool>("Skin");

        SlotTypes = [];
        var slotTypes = e.GetValue<string>("SlotTypes");
        if (slotTypes != null)
            foreach (var slot in slotTypes.Split(','))
                SlotTypes.Add(int.Parse(slot));

        Container = e.HasElement("Container");
        LockedPortal = e.HasElement("LockedPortal");

        AtlasData GetShapeTexture(string elemName) {
            if (!e.HasElement(elemName)) return default;
            var el = e.Element(elemName);
            return Main.Atlas.GetAtlasData(el.GetValue<string>("File"), el.GetValue<int>("Index"));
        }

        DotTexture = GetShapeTexture("DotTexture");
        ShortLineTexture = GetShapeTexture("ShortLineTexture");
        LTexture = GetShapeTexture("LTexture");
        LineTexture = GetShapeTexture("LineTexture");
        TTexture = GetShapeTexture("TTexture");
        CrossTexture = GetShapeTexture("CrossTexture");

        Equipment = new List<ushort?>();
        var equipment = e.GetValue<string>("Equipment");
        if (equipment != null)
            foreach (var equip in equipment.Split(',')) {
                var clean = equip.Replace(" ", "");
                if (clean == "-1") {
                    Equipment.Add(null);
                    continue;
                }

                var objectType = Convert.ToUInt16(PadHexIfNeeded(clean), Common.Utils.GetBase(clean));
                Equipment.Add(objectType);
            }

        BaseIndent = e.HasElement("BaseIndent") ? 0.5f - e.GetValue<float>("BaseIndent") : 0.5f;
        TopIndent = e.HasElement("TopIndent") ? 0.5f - e.GetValue<float>("TopIndent") : 0.25f;
        Height = e.GetValue("Height", 1.0f);
    }

    private static string PadHexIfNeeded(string hexValue) {
        if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
            hexValue = hexValue.Substring(2);

            if (hexValue.Length < 4) hexValue = hexValue.PadLeft(4, '0');

            return "0x" + hexValue;
        }

        return hexValue;
    }
}