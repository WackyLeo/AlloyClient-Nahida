using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlloyClient.Editor;

/// <summary>
/// Tile data of one map plus the .jm (json) and .wmap (binary) formats, ported from the standalone editor's MapData.as.
/// </summary>
public sealed class MapData {
    private static readonly Logger Log = new(typeof(MapData));

    public string MapName = "";
    public int MapWidth;
    public int MapHeight;
    public MapTileData[] Tiles = [];
    public bool SavedChanges = true;

    public event Action Changed;
    public event Action Saved;

    public MapTileData GetTile(int x, int y) {
        if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight) return null;
        return Tiles[x + y * MapWidth];
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < MapWidth && y < MapHeight;

    public void MarkChanged() {
        if (SavedChanges) {
            SavedChanges = false;
            Changed?.Invoke();
        }
    }

    public void MarkSaved() {
        SavedChanges = true;
        Saved?.Invoke();
    }

    public void NewMap(string name, int width, int height) {
        MapName = name;
        MapWidth = width;
        MapHeight = height;
        Tiles = new MapTileData[width * height];
        for (var i = 0; i < Tiles.Length; i++) Tiles[i] = new MapTileData();
        SavedChanges = false;
    }

    /// <summary>Changing the dimensions re-centers the "real" map area (bounding box of non-empty tiles) into the new size.</summary>
    public void ChangeDimensions(int width, int height) {
        var (rectX, rectY, rectW, rectH) = GetRealMapRect();
        var rectEndX = rectX + rectW - 1;
        var rectEndY = rectY + rectH - 1;
        var oldWidth = MapWidth;
        var oldHeight = MapHeight;
        var oldTiles = Tiles;

        var xOffset = (width - oldWidth) / 2;
        var yOffset = (height - oldHeight) / 2;

        var newTiles = new MapTileData[width * height];
        for (var i = 0; i < newTiles.Length; i++) newTiles[i] = new MapTileData();

        for (var y = rectY; y <= rectEndY; y++) {
            for (var x = rectX; x <= rectEndX; x++) {
                if (x >= oldWidth || y >= oldHeight) continue;
                var newX = x + xOffset;
                var newY = y + yOffset;
                if (newX < 0 || newY < 0 || newX >= width || newY >= height) continue;
                newTiles[newX + newY * width] = oldTiles[x + y * oldWidth].Clone();
            }
        }

        MapWidth = width;
        MapHeight = height;
        Tiles = newTiles;
        SavedChanges = true; // force the changed event below
        MarkChanged();
    }

    private (int x, int y, int w, int h) GetRealMapRect() {
        int minX = MapWidth, minY = MapHeight, maxX = 0, maxY = 0;
        for (var y = 0; y < MapHeight; y++) {
            for (var x = 0; x < MapWidth; x++) {
                if (Tiles[x + y * MapWidth].IsEmpty) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return (minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    #region Loading

    /// <summary>Loads a .jm or .wmap file. Returns an error message, or null on success.</summary>
    public string LoadFile(string path) {
        try {
            var bytes = File.ReadAllBytes(path);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            MapName = Path.GetFileNameWithoutExtension(path);
            switch (ext) {
                case ".wmap":
                    LoadWMap(bytes);
                    break;
                case ".jm":
                    LoadJson(Encoding.UTF8.GetString(bytes));
                    break;
                default:
                    return $"Unsupported map format: {ext}";
            }

            SavedChanges = true;
            return null;
        } catch (Exception e) {
            Log.Error($"Failed to load map {path}: {e}");
            return $"Failed to load map: {e.Message}";
        }
    }

    private void LoadJson(string json) {
        var jm = JObject.Parse(json);
        var width = jm.Value<int>("width");
        var height = jm.Value<int>("height");
        var dict = (JArray)jm["dict"] ?? new JArray();
        var data = Decompress(Convert.FromBase64String(jm.Value<string>("data") ?? ""));

        // Pre-parse dictionary entries once
        var entries = new MapTileData[dict.Count];
        for (var i = 0; i < dict.Count; i++) {
            if (dict[i] is not JObject entry) continue;
            var tile = new MapTileData();

            var groundId = entry.Value<string>("ground");
            if (groundId != null) {
                if (EditorAssets.TryGetGroundType(groundId, out var groundType)) tile.GroundType = groundType;
                else Log.Warn($"Unable to find ground: {groundId}");
            }

            if (entry["objs"] is JArray objs) {
                foreach (var obj in objs) {
                    var objId = obj.Value<string>("id");
                    if (!EditorAssets.TryGetObjectType(objId, out var objType)) {
                        Log.Warn($"Unable to find object: {objId}");
                        continue;
                    }

                    tile.ObjType = objType;
                    var name = obj.Value<string>("name");
                    if (name != null) tile.ObjCfg = name;
                }
            }

            if (entry["regions"] is JArray regions) {
                foreach (var region in regions) {
                    var regionId = region.Value<string>("id");
                    if (EditorAssets.TryGetRegionType(regionId, out var regType)) tile.RegType = regType;
                    else Log.Warn($"Unable to find region: {regionId}");
                }
            }

            var terrain = entry["terrain"];
            if (terrain != null) tile.TerrainType = EditorAssets.ParseInt(terrain.ToString());

            entries[i] = tile;
        }

        MapWidth = width;
        MapHeight = height;
        Tiles = new MapTileData[width * height];
        var pos = 0;
        for (var i = 0; i < Tiles.Length; i++) {
            MapTileData entry = null;
            if (pos + 2 <= data.Length) {
                var idx = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(pos, 2)); // flash ByteArray default is big endian
                pos += 2;
                if (idx >= 0 && idx < entries.Length) entry = entries[idx];
            }

            Tiles[i] = entry?.Clone() ?? new MapTileData();
        }
    }

    private void LoadWMap(byte[] orig) {
        var ver = orig[0];
        if (ver > 2) throw new InvalidDataException($"Invalid wmap version {ver}");

        var data = Decompress(orig.AsSpan(1).ToArray());
        using var reader = new BinaryReader(new MemoryStream(data)); // little endian

        var tileCount = reader.ReadUInt16();
        var tiles = new MapTileData[tileCount];
        for (var i = 0; i < tileCount; i++) {
            var tileType = reader.ReadUInt16();
            var objId = Encoding.UTF8.GetString(reader.ReadBytes(reader.Read7BitEncodedInt()));
            var objCfg = Encoding.UTF8.GetString(reader.ReadBytes(reader.Read7BitEncodedInt()));
            var terrainType = reader.ReadByte();
            var regionType = reader.ReadByte();
            var elevation = ver == 1 ? reader.ReadByte() : 0;

            var tile = new MapTileData {
                GroundType = tileType == ushort.MaxValue ? -1 : tileType,
                ObjCfg = objCfg == "" ? null : objCfg,
                TerrainType = terrainType,
                RegType = regionType,
                Elevation = elevation
            };

            if (objId != "") {
                if (EditorAssets.TryGetObjectType(objId, out var objType)) tile.ObjType = objType;
                else Log.Warn($"Unable to find object: {objId}");
            }

            tiles[i] = tile;
        }

        var width = reader.ReadInt32();
        var height = reader.ReadInt32();

        MapWidth = width;
        MapHeight = height;
        Tiles = new MapTileData[width * height];
        for (var i = 0; i < Tiles.Length; i++) {
            var idx = reader.ReadUInt16();
            var tile = idx < tiles.Length ? tiles[idx].Clone() : new MapTileData();
            if (ver == 2) tile.Elevation = reader.ReadByte();
            Tiles[i] = tile;
        }
    }

    #endregion

    #region Saving

    public byte[] Export(bool wmap) => wmap ? ExportWMap() : Encoding.UTF8.GetBytes(GetMapJsonString());

    /// <summary>Saves to the given path. Returns an error message, or null on success.</summary>
    public string SaveFile(string path, bool wmap, bool markSaved = true) {
        try {
            var bytes = Export(wmap);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, bytes);
            if (markSaved) MarkSaved();
            return null;
        } catch (Exception e) {
            Log.Error($"Failed to save map {path}: {e}");
            return $"Failed to save map: {e.Message}";
        }
    }

    private byte[] ExportWMap() {
        const int ver = 1;

        var defs = new List<MapTileData>();
        var lookup = new Dictionary<(int, int, int, string, int, int), int>();
        var indices = new ushort[MapWidth * MapHeight];
        for (var i = 0; i < Tiles.Length; i++) {
            var t = Tiles[i];
            var key = (t.GroundType, t.ObjType, t.RegType, t.ObjCfg ?? "", t.TerrainType, t.Elevation);
            if (!lookup.TryGetValue(key, out var idx)) {
                idx = defs.Count;
                lookup[key] = idx;
                defs.Add(t);
            }

            indices[i] = (ushort)idx;
        }

        if (defs.Count > ushort.MaxValue)
            throw new InvalidDataException("Map has too many unique tiles for the wmap format");

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, true)) {
            writer.Write((ushort)defs.Count);
            foreach (var tile in defs) {
                writer.Write((ushort)(tile.GroundType < 0 ? ushort.MaxValue : tile.GroundType));
                var objId = Encoding.UTF8.GetBytes(EditorAssets.GetObjectId(tile.ObjType) ?? "");
                writer.Write7BitEncodedInt(objId.Length);
                writer.Write(objId);
                var objCfg = Encoding.UTF8.GetBytes(tile.ObjCfg ?? "");
                writer.Write7BitEncodedInt(objCfg.Length);
                writer.Write(objCfg);
                writer.Write((byte)tile.TerrainType);
                writer.Write((byte)tile.RegType);
                writer.Write((byte)tile.Elevation); // ver 1
            }

            writer.Write(MapWidth);
            writer.Write(MapHeight);
            foreach (var idx in indices) writer.Write(idx);
        }

        var compressed = Compress(ms.ToArray());
        var ret = new byte[compressed.Length + 1];
        ret[0] = ver;
        compressed.CopyTo(ret, 1);
        return ret;
    }

    public string GetMapJsonString() {
        var entries = new JArray();
        var dict = new Dictionary<string, int>();
        var indices = new byte[MapWidth * MapHeight * 2];

        for (var i = 0; i < Tiles.Length; i++) {
            var entry = GetJsonTile(Tiles[i]);
            var key = entry.ToString(Formatting.None);
            if (!dict.TryGetValue(key, out var idx)) {
                idx = entries.Count;
                dict[key] = idx;
                entries.Add(entry);
            }

            BinaryPrimitives.WriteInt16BigEndian(indices.AsSpan(i * 2, 2), (short)idx);
        }

        var jm = new JObject {
            ["width"] = MapWidth,
            ["height"] = MapHeight,
            ["dict"] = entries,
            ["data"] = Convert.ToBase64String(Compress(indices))
        };
        return jm.ToString(Formatting.None);
    }

    private static JObject GetJsonTile(MapTileData tile) {
        var ret = new JObject();
        if (tile == null) return ret;

        if (tile.GroundType != -1) {
            var id = EditorAssets.GetGroundId(tile.GroundType);
            if (id != null) ret["ground"] = id;
        }

        if (tile.ObjType != 0) {
            var id = EditorAssets.GetObjectId(tile.ObjType);
            if (id != null) {
                var obj = new JObject { ["id"] = id };
                if (!string.IsNullOrEmpty(tile.ObjCfg)) obj["name"] = tile.ObjCfg;
                ret["objs"] = new JArray(obj);
            }
        }

        if (tile.RegType != 0) {
            var id = EditorAssets.GetRegionId(tile.RegType);
            if (id != null) ret["regions"] = new JArray(new JObject { ["id"] = id });
        }

        if (tile.TerrainType != 0) ret["terrain"] = tile.TerrainType.ToString();
        return ret;
    }

    #endregion

    // Flash's ByteArray.compress()/uncompress() use the zlib format
    private static byte[] Compress(byte[] data) {
        using var output = new MemoryStream();
        using (var z = new ZLibStream(output, CompressionLevel.Optimal, true)) z.Write(data, 0, data.Length);
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data) {
        using var input = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
