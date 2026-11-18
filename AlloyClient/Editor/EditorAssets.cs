using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using AlloyClient.Assets;
using AlloyClient.Assets.Libraries;
using Common.Structs;

namespace AlloyClient.Editor;

public sealed record RegionInfo(int Type, string Id, uint Color, XElement Xml);

/// <summary>
/// Asset lookups used by the map editor. Ground and object data come from the client's own libraries
/// (populated from the server xmls); regions and raw xml (for the property filter / tooltips) are kept here
/// because the game itself never needs them.
/// </summary>
public static class EditorAssets {
    private static readonly object Lock = new();

    public static readonly Dictionary<int, RegionInfo> Regions = new();
    public static readonly Dictionary<string, int> RegionIdToType = new();
    public static readonly Dictionary<int, XElement> ObjectXml = new();
    public static readonly Dictionary<int, XElement> GroundXml = new();

    private static int[] _groundTypes;
    private static int[] _objectTypes;
    private static int[] _regionTypes;

    public static void RegisterXml(XElement root) {
        lock (Lock) {
            foreach (var region in root.Elements("Region")) {
                try {
                    var type = ParseInt(region.Attribute("type")?.Value);
                    var id = region.Attribute("id")?.Value ?? $"Region {type}";
                    var color = (uint)ParseInt(region.Element("Color")?.Value);
                    Regions[type] = new RegionInfo(type, id, color & 0xFFFFFF, region);
                    RegionIdToType[id] = type;
                } catch {
                    // Malformed region entry, skip it
                }
            }

            foreach (var obj in root.Elements("Object")) {
                var type = ParseInt(obj.Attribute("type")?.Value);
                ObjectXml[type] = obj;
            }

            foreach (var ground in root.Elements("Ground")) {
                var type = ParseInt(ground.Attribute("type")?.Value);
                GroundXml[type] = ground;
            }

            _groundTypes = null;
            _objectTypes = null;
            _regionTypes = null;
        }
    }

    public static bool IsLoaded => GroundLibrary.TypeToGroundProps.Count > 0 || ObjectLibrary.TypeToObjectProps.Count > 0;

    public static int ParseInt(string value) {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return (int)Convert.ToUInt32(value[2..], 16);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;
    }

    #region Type lists

    public static IReadOnlyList<int> GetTypes(DrawType drawType) {
        try {
            return GetTypesInternal(drawType);
        } catch (InvalidOperationException) {
            return Array.Empty<int>(); // Libraries are still being filled by the asset loader thread
        }
    }

    private static IReadOnlyList<int> GetTypesInternal(DrawType drawType) {
        lock (Lock) {
            switch (drawType) {
                case DrawType.Ground:
                    return _groundTypes ??= GroundLibrary.TypeToGroundProps.Keys
                        .Select(k => (int)k)
                        .Where(t => TryGetGroundTexture(t, out _))
                        .OrderBy(t => t).ToArray();
                case DrawType.Objects:
                    return _objectTypes ??= ObjectLibrary.TypeToObjectProps
                        .Where(kv => IsPlaceableObject(kv.Value.Class) && TryGetObjectTexture(kv.Key, out _))
                        .Select(kv => (int)kv.Key)
                        .OrderBy(t => t).ToArray();
                case DrawType.Regions:
                    return _regionTypes ??= Regions.Keys.OrderBy(t => t).ToArray();
                default:
                    return Array.Empty<int>();
            }
        }
    }

    private static bool IsPlaceableObject(string cls) => cls is not ("Projectile" or "Equipment" or "Dye");

    #endregion

    #region Id <-> type

    public static string GetId(DrawType drawType, int type) => drawType switch {
        DrawType.Ground => GetGroundId(type),
        DrawType.Objects => GetObjectId(type),
        DrawType.Regions => GetRegionId(type),
        _ => null
    };

    public static string GetGroundId(int type) {
        if (type < 0 || type > ushort.MaxValue) return null;
        return GroundLibrary.TypeToGroundProps.TryGetValue((ushort)type, out var props) ? props.ObjectId : null;
    }

    public static string GetObjectId(int type) {
        if (type <= 0 || type > ushort.MaxValue) return null;
        return ObjectLibrary.TypeToObjectProps.TryGetValue((ushort)type, out var props) ? props.ObjectId : null;
    }

    public static string GetRegionId(int type) {
        lock (Lock) return Regions.TryGetValue(type, out var r) ? r.Id : null;
    }

    public static bool TryGetGroundType(string id, out int type) {
        type = -1;
        if (id == null || !GroundLibrary.IdToTileType.TryGetValue(id, out var t)) return false;
        type = t;
        return true;
    }

    public static bool TryGetObjectType(string id, out int type) {
        type = 0;
        if (id == null || !ObjectLibrary.IdToObjectType.TryGetValue(id, out var t)) return false;
        type = t;
        return true;
    }

    public static bool TryGetRegionType(string id, out int type) {
        type = 0;
        lock (Lock) return id != null && RegionIdToType.TryGetValue(id, out type);
    }

    public static uint GetRegionColor(int type) {
        lock (Lock) return Regions.TryGetValue(type, out var r) ? r.Color : 0;
    }

    public static XElement GetXml(DrawType drawType, int type) {
        lock (Lock) {
            return drawType switch {
                DrawType.Ground => GroundXml.GetValueOrDefault(type),
                DrawType.Objects => ObjectXml.GetValueOrDefault(type),
                DrawType.Regions => Regions.TryGetValue(type, out var r) ? r.Xml : null,
                _ => null
            };
        }
    }

    public static List<int> Search(DrawType drawType, string text) {
        var ret = new List<int>();
        if (string.IsNullOrEmpty(text)) return ret;

        IEnumerable<KeyValuePair<string, int>> source = drawType switch {
            DrawType.Ground => GroundLibrary.IdToTileType.Select(kv => new KeyValuePair<string, int>(kv.Key, kv.Value)),
            DrawType.Objects => ObjectLibrary.IdToObjectType.Select(kv => new KeyValuePair<string, int>(kv.Key, kv.Value)),
            DrawType.Regions => RegionIdToType.ToArray(),
            _ => Array.Empty<KeyValuePair<string, int>>()
        };

        foreach (var (id, type) in source)
            if (id.Contains(text, StringComparison.OrdinalIgnoreCase))
                ret.Add(type);
        return ret;
    }

    #endregion

    #region Textures

    public static bool TryGetGroundTexture(int type, out AtlasData uv) {
        uv = default;
        if (type < 0 || type > ushort.MaxValue) return false;
        if (!GroundLibrary.TypeToTextureData.TryGetValue((ushort)type, out var td) || td == null) return false;
        if (td.IsRemoteSheet) return false;

        uv = td.GetTexture();
        if (uv.W <= 0 || uv.H <= 0) return false;
        uv.RemovePadding();
        return true;
    }

    public static bool TryGetObjectTexture(int type, out AtlasData uv) {
        uv = default;
        if (type <= 0 || type > ushort.MaxValue) return false;
        if (!ObjectLibrary.TypeToTextureData.TryGetValue((ushort)type, out var td) || td == null) return false;
        if (td.IsRemoteSheet) return false;

        if (td.EditorTexture is { W: > 0, H: > 0 } editor)
            uv = editor;
        else if (td.Texture is { W: > 0, H: > 0 })
            uv = td.Texture;
        else if (td.HasAnimationData && td.AnimatedTextures.FaceRight?.Length > 0)
            uv = td.AnimatedTextures.FaceRight[0];
        else if (td.RandomTextures?.Length > 0 && td.RandomTextures[0].Texture is { W: > 0, H: > 0 })
            uv = td.RandomTextures[0].Texture;
        else
            return false;

        if (uv.W <= 0 || uv.H <= 0) return false;
        uv.RemovePadding();
        return true;
    }

    /// <summary>Texture of a draw element. Regions have no texture (they're drawn as a color).</summary>
    public static bool TryGetTexture(DrawType drawType, int type, out AtlasData uv) {
        switch (drawType) {
            case DrawType.Ground:
                return TryGetGroundTexture(type, out uv);
            case DrawType.Objects:
                return TryGetObjectTexture(type, out uv);
            default:
                uv = default;
                return false;
        }
    }

    #endregion
}
