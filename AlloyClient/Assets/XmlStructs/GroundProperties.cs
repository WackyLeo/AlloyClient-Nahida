using System;
using System.Xml.Linq;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public class GroundProperties {
    public readonly GroundAnimate Animate;
    public readonly int BlendPriority;
    public readonly int CompositePriority;
    public readonly TextureData CornerTD;
    public readonly TextureData EdgeTD;


    public readonly TextureData InnerCornerTD;
    public readonly int MaxDamage;

    public readonly int MinDamage;

    public readonly bool NoWalk;
    public readonly string ObjectId;
    public readonly ushort ObjectType;
    public readonly bool Push;
    public readonly bool RandomOffset;
    public readonly bool HasEdge;
    public readonly bool SameTypeEdgeMode;
    public readonly bool Sink;
    public readonly bool Sinking;
    public readonly float SlideAmount;
    public readonly float Speed;
    public readonly GroundAnimate TopAnimate;
    public readonly TextureData TopTD;
    public readonly float XOffset;
    public readonly float YOffset;
    public readonly float ZOffset;

    public GroundProperties(XElement e) {
        ObjectType = e.GetAttribute<ushort>("type");
        ObjectId = e.GetAttribute<string>("id");

        ZOffset = e.GetValue("ZOffset", 0f);
        NoWalk = e.HasElement("NoWalk");
        MinDamage = e.GetValue("MinDamage", 0);
        MaxDamage = e.GetValue("MaxDamage", 0);
        Push = e.HasElement("Push");
        Animate = e.HasElement("Animate") ? new GroundAnimate(e.Element("Animate")) : new GroundAnimate();
        BlendPriority = e.GetValue("BlendPriority", -1);
        CompositePriority = e.GetValue<int>("CompositePriority");
        Speed = e.GetValue("Speed", 1f);
        SlideAmount = e.GetValue<float>("SlideAmount");
        XOffset = e.GetValue("XOffset", 0f);
        YOffset = e.GetValue("YOffset", 0f);
        Sink = e.HasElement("Sink");
        Sinking = e.HasElement("Sinking");
        RandomOffset = e.HasElement("RandomOffset");

        HasEdge = e.HasElement("Edge");
        SameTypeEdgeMode = e.HasElement("SameTypeEdgeMode");

        if (e.HasElement("Top")) {
            TopTD = new TextureData(e.Element("Top"));
            TopAnimate = e.HasElement("TopAnimate") ? new GroundAnimate(e.Element("TopAnimate")) : new GroundAnimate();
        }
    }
}

public readonly struct GroundAnimate {
    public readonly State Type;
    public readonly float DeltaX;
    public readonly float DeltaY;

    public GroundAnimate() {
        Type = State.None;
        DeltaX = 0f;
        DeltaY = 0f;
    }

    public GroundAnimate(XElement e) {
        Type = Enum.Parse<State>(e.Value);
        DeltaX = e.GetAttribute("dx", 0f);
        DeltaY = e.GetAttribute("dy", 0f);
    }

    public enum State {
        None,
        Wave,
        Flow
    }
}