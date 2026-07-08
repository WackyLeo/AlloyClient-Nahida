using System;
using System.Globalization;
using System.Xml.Linq;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public sealed class EffectProperties {
    public readonly int Amount;
    public readonly string BitmapFile;
    public readonly int BitmapIndex;
    public readonly uint Color;
    public readonly uint Color2;
    public readonly float Cooldown;
    public readonly string Id;
    public readonly float Life;
    public readonly float LifeVariance;
    public readonly float MaxRadius;
    public readonly float MinRadius;
    public readonly float MoveSpeed;
    public readonly float Offset;
    public readonly string Particle;
    public readonly int RangeX;
    public readonly int RangeY;
    public readonly float Rate;
    public readonly float Rise;
    public readonly float RiseAcc;
    public readonly float RiseVariance;
    public readonly int Size;
    public readonly float Speed;
    public readonly float SpeedVariance;
    public readonly float Spread;
    public readonly float ZOffset;

    public EffectProperties(XElement e) {
        Id = e.Value;
        Particle = e.GetAttribute<string>("particle");
        Cooldown = e.GetAttribute("cooldown", 0f);
        Color = ParseColor(e.GetAttribute<string>("color"));
        Color2 = ParseColor(e.GetAttribute<string>("color2"));
        Rate = e.GetAttribute("rate", 5f);
        Speed = e.GetAttribute("speed", 0f);
        MoveSpeed = e.GetAttribute("moveSpeed", 1f);
        Offset = e.GetAttribute("offset", 0f);
        SpeedVariance = e.GetAttribute("speedVariance", 0.5f);
        Spread = e.GetAttribute("spread", 0f);
        Life = e.GetAttribute("life", 1f);
        LifeVariance = e.GetAttribute("lifeVariance", 0f);
        Size = e.GetAttribute("size", 1);
        Rise = e.GetAttribute("rise", 3.0f);
        RiseVariance = e.GetAttribute("riseVariance", 0f);
        RiseAcc = e.GetAttribute("riseAcc", 0f);
        RangeX = e.GetAttribute("rangeX", 0);
        RangeY = e.GetAttribute("rangeY", 0);
        ZOffset = e.GetAttribute("zOffset", 0f);
        MinRadius = e.GetAttribute("minRadius", 0f);
        MaxRadius = e.GetAttribute("maxRadius", 1.0f);
        Amount = e.GetAttribute("amount", 1);
        BitmapFile = e.GetAttribute<string>("bitmapFile");
        BitmapIndex = e.GetAttribute("bitmapIndex", 0);
    }

    private static uint ParseColor(string value) {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            value = value[2..];
        if (value.StartsWith("#"))
            value = value[1..];

        return uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var color)
            ? color
            : 0;
    }
}
