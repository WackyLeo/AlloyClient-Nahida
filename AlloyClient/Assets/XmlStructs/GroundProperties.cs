using System;
using System.Xml.Linq;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public class GroundProperties {
    public readonly ushort ObjectType;
    public readonly string ObjectId;
    public readonly float ZOffset;
    
    public readonly bool NoWalk;

    public readonly int MinDamage;
    public readonly int MaxDamage;
    public readonly bool Push;
    public readonly GroundAnimate Animate;
    public readonly int BlendPriority;
    public readonly int CompositePriority;
    public readonly float Speed;
    public readonly float SlideAmount;
    public readonly float XOffset;
    public readonly float YOffset;
    public readonly bool Sink;
    public readonly bool Sinking;
    public readonly bool RandomOffset;


    public readonly bool HasEdge;
    public readonly TextureData EdgeTD;
    public readonly TextureData CornerTD;
    public readonly TextureData InnerCornerTD;
    public readonly bool SameTypeEdgeMode;
    public readonly TextureData TopTD;
    public readonly GroundAnimate TopAnimate;

    public GroundProperties(XElement e) {
        ObjectType = e.GetAttribute<ushort>("type");
        ObjectId = e.GetAttribute<string>("id");
        
        ZOffset = e.GetValue("ZOffset", 0f);
        Logger.Info($"Ground 0x{ObjectType:X4} ({ObjectId}) ZOffset={ZOffset}"); // ← add this
        NoWalk = e.HasElement("NoWalk"); 
        MinDamage = e.GetValue("MinDamage", 0);
        MaxDamage = e.GetValue("MaxDamage", 0);
        Push = e.HasElement("Push");
        Animate = e.HasElement("Animate") ? new GroundAnimate(e.Element("Animate")) : new GroundAnimate();
        BlendPriority = e.GetValue("BlendPriority", -1);
        CompositePriority = e.GetValue<int>("CompositePriority");
        Speed = e.GetValue("Speed", 1f);
        SlideAmount = e.GetValue<float>("SlideAmount", 0.0f);
        XOffset = e.GetValue("XOffset", 0f);
        YOffset = e.GetValue("YOffset", 0f);
        Sink = e.HasElement("Sink");
        Sinking = e.HasElement("Sinking");
        RandomOffset = e.HasElement("RandomOffset");

        if (e.HasElement("Edge")) {
            HasEdge = true;
            EdgeTD = new TextureData(e.Element("Edge"));
            
            if (e.HasElement("Corner")) {
                CornerTD = new TextureData(e.Element("Corner"));
            }
            if (e.HasElement("InnerCorner")) {
                InnerCornerTD = new TextureData(e.Element("InnerCorner"));
            }
        }

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