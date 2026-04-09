using System.Collections.Generic;
using System.Xml.Linq;
using Common;

namespace AlloyClient.Assets.XmlStructs;

public class ParticleProperties {
    public readonly float Duration;
    public readonly List<ParticleFrame> Frames = new();
    public readonly string Id;
    public readonly float Size;
    public readonly string TextureFile;
    public readonly int TextureIndex;
    public readonly float Z;

    public ParticleProperties(XElement e) {
        Id = e.GetAttribute<string>("id");

        var tex = e.Element("Texture");
        if (tex != null) {
            TextureFile = tex.GetValue<string>("File");
            TextureIndex = tex.GetValue<int>("Index");
        }

        Size = e.GetValue("Size", 100f);
        Z = e.GetValue("Z", 0.2f);
        Duration = e.GetValue("Duration", 1f);

        foreach (var anim in e.Elements("Animation")) {
            foreach (var frame in anim.Elements("Frame")) {
                var frameTex = frame.Element("Texture");
                if (frameTex == null) continue;
                Frames.Add(new ParticleFrame {
                    File = frameTex.GetValue<string>("File"),
                    Index = frameTex.GetValue<int>("Index"),
                    Time = frame.GetAttribute("time", 0.2f)
                });
            }
        }
    }
}

public struct ParticleFrame {
    public string File;
    public int Index;
    public float Time;
}