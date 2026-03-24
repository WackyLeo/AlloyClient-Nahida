using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public class ObjectProperties {
    public readonly float AngleCorrection;
    public readonly float ArcGap;

    public readonly string Class;

    public readonly bool Container;

    public readonly string Description;

    public readonly string DisplayId;
    public readonly string DisplayName;
    public readonly bool DrawOnGround;
    public readonly string Effect;

    public readonly bool EnemyOccupySquare;
    public readonly List<ushort?> Equipment;
    public readonly bool FullOccupy;
    public readonly bool IsAlly;
    public readonly bool IsEnemy;

    public readonly bool IsPlayer;
    public readonly bool LockedPortal;
    public readonly int MaxSize;
    public readonly int MinSize;
    public readonly string Model;
    public readonly bool NoMiniMap;
    public readonly int NumProjectiles;
    public readonly string ObjectId;
    public readonly ushort ObjectType;
    public readonly bool OccupySquare;

    public readonly ushort PlayerClassType;
    public readonly PlayerProperties PlayerProperties;

    public readonly List<ProjectileProperties> Projectiles;
    public readonly Dictionary<int, ProjectileProperties> ProjectilesDict;
    public readonly bool ProtectFromGroundDamage;
    public readonly bool ProtectFromSink;

    public readonly int RealSize;
    public readonly float Rotation;
    public readonly bool ShowName;
    public readonly int SizeStep = 5;
    public readonly bool Skin;

    public readonly List<int> SlotTypes;

    public readonly bool Static;

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
        IsAlly = e.GetValue<bool>("Ally");

        if (IsPlayer) PlayerProperties = new PlayerProperties(e);

        EnemyOccupySquare = e.GetValue<bool>("EnemyOccupySquare");
        OccupySquare = e.GetValue<bool>("OccupySquare");
        FullOccupy = e.GetValue<bool>("FullOccupy");

        Static = e.GetValue<bool>("Static");
        NoMiniMap = e.GetValue<bool>("NoMiniMap");
        DrawOnGround = e.GetValue<bool>("DrawOnGround");
        ProtectFromGroundDamage = e.HasElement("ProtectFromGroundDamage");
        ProtectFromSink = e.HasElement("ProtectFromSink");

        RealSize = e.GetValue("RealSize", -1);
        MinSize = e.GetValue<int>("MinSize");
        MaxSize = e.GetValue<int>("MaxSize");
        SizeStep = e.GetValue("SizeStep", 5);

        Description = e.GetValue<string>("Description");

        PlayerClassType = e.GetValue<ushort>("PlayerClassType");
        Skin = e.GetValue<bool>("Skin");

        SlotTypes = [];
        var slotTypes = e.GetValue<string>("SlotTypes");
        if (slotTypes != null)
            foreach (var slot in slotTypes.Split(','))
                SlotTypes.Add(int.Parse(slot));

        Container = e.HasElement("Container");
        LockedPortal = e.HasElement("LockedPortal");

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