using System.Collections.Generic;
using System.Xml.Linq;
using AlloyClient.Assets.XmlStructs;
using Common;

namespace AlloyClient.Assets.Libraries;

public static class ParticleLibrary {
    private static readonly Logger Log = new(nameof(ParticleLibrary));
    private static readonly Dictionary<string, ParticleProperties> Props = new();

    public static void Load(string xmlPath) {
        var doc = XDocument.Load(xmlPath);
        foreach (var e in doc.Root!.Elements("Particle")) {
            var props = new ParticleProperties(e);
            if (!string.IsNullOrEmpty(props.Id))
                Props[props.Id] = props;
        }

        Log.Info($"Loaded {Props.Count} particle definitions");
    }

    public static bool TryGet(string id, out ParticleProperties props) {
        return Props.TryGetValue(id, out props);
    }
}