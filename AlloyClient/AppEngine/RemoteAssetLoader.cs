using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AlloyClient.AppEngine;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Networking;
using Common;

namespace AlloyClient.Assets;

public static class RemoteAssetLoader {
    private static readonly Logger Log = new(nameof(RemoteAssetLoader));

    private static bool _loaded;

    public static async Task LoadAllAsync() {
        if (_loaded) return;

        Log.Info("Starting remote asset load...");

        //await LoadSheetsAsync();
        //await LoadAnimatedAsync();
        await LoadXmlsAsync();
        await LoadWikiDataAsync();
        //await LoadTexturesAsync();
        _ = LoadEmotesAsync();

        _loaded = true;
        Log.Info("Remote asset load complete.");
    }

    private static async Task LoadSheetsAsync() {
        Log.Info("Fetching /resources/sheets...");
        var bytes = await AppEngineClient.SendRequestRaw("/resources/sheets");
        if (bytes == null || bytes.Length == 0) {
            Log.Error("No data from /resources/sheets.");
            return;
        }

        using var reader = new NetworkReader(new MemoryStream(bytes), false);
        var count = reader.ReadInt32();
        Log.Info($"{count} sheet(s) received.");

        for (var i = 0; i < count; i++)
            try {
                var id = reader.ReadUTF();
                var sizeStr = reader.ReadUTF();
                var length = reader.ReadInt32();
                var png = reader.ReadBytes(length);

                if (SpriteSheetLibrary.HasSheet(id)) continue;
                if (!ParseSize(sizeStr, out var sw, out var sh)) {
                    Log.Warn($"Bad size string '{sizeStr}' for sheet '{id}'");
                    continue;
                }

                SpriteSheetLibrary.AddSheet(id, png, sw, sh);
                Log.Info($"Sheet '{id}' loaded, checking SR8x8items1: {SpriteSheetLibrary.HasSheet("SR8x8items1")}");
            } catch (Exception e) {
                Log.Error($"Sheet {i} error: {e.Message}");
            }

        Log.Info($"All loaded sheets: {string.Join(", ", SpriteSheetLibrary.GetAll().Keys)}");
    }

    private static async Task LoadWikiDataAsync() {
        Log.Info("Fetching /app/getLootdropJson...");

        var formPayload = new Dictionary<string, string> { { "null", "null" } };
        byte[] bytes;
        try {
            bytes = await AppEngineClient.SendRequestRaw("/app/getLootdropJson", formPayload);
        } catch (Exception e) {
            Log.Error($"Failed to send request: {e.Message}");
            return;
        }

        if (bytes == null || bytes.Length == 0) {
            Log.Warn("No data from /app/getLootdropJson.");
            return;
        }

        try {
            var jsonString = Encoding.UTF8.GetString(bytes);
            WikiLibrary.LoadLootDropJson(jsonString);
        } catch (Exception e) {
            Log.Error($"Wiki data read error: {e.Message}");
        }
    }

    private static async Task LoadAnimatedAsync() {
        Log.Info("Fetching /resources/animated...");
        var bytes = await AppEngineClient.SendRequestRaw("/resources/animated");
        if (bytes == null || bytes.Length == 0) {
            Log.Error("No data from /resources/animated.");
            return;
        }

        using var reader = new NetworkReader(new MemoryStream(bytes), false);
        var count = reader.ReadInt32();
        Log.Info($"{count} animated sheet(s) received.");

        for (var i = 0; i < count; i++)
            try {
                var id = reader.ReadUTF();
                var sizeStr = reader.ReadUTF();
                var totalLength = reader.ReadUTF();
                var length = reader.ReadInt32();
                var png = reader.ReadBytes(length);

                if (SpriteSheetLibrary.HasSheet(id)) continue;
                if (!ParseSize(sizeStr, out var sw, out var sh)) {
                    Log.Warn($"Bad size string '{sizeStr}' for animated sheet '{id}'");
                    continue;
                }

                SpriteSheetLibrary.AddSheet(id, png, sw, sh);
            } catch (Exception e) {
                Log.Error($"Animated sheet {i} error: {e.Message}");
            }
    }

    private static async Task LoadXmlsAsync() {
        Log.Info("Fetching /resources/xmls...");
        var bytes = await AppEngineClient.SendRequestRaw("/resources/xmls");
        if (bytes == null || bytes.Length == 0) {
            Log.Error("No data from /resources/xmls.");
            return;
        }

        using var reader = new NetworkReader(new MemoryStream(bytes), false);
        var count = reader.ReadInt32();
        Log.Info($"{count} XML chunk(s) received.");

        for (var i = 0; i < count; i++)
            try {
                var length = reader.ReadInt32();
                var xmlBytes = reader.ReadBytes(length);
                var rawXml = Encoding.UTF8.GetString(xmlBytes);
                var xml = XElement.Parse(rawXml);

                ParseGroundXml(xml);
                ParseObjectsXml(xml);
                Editor.EditorAssets.RegisterXml(xml);
            } catch (Exception e) {
                Log.Error($"XML chunk {i} error: {e.Message}");
            }
    }

    private static void ParseGroundXml(XElement root) {
        foreach (var ground in root.Elements("Ground"))
            try {
                var props = new GroundProperties(ground);

                lock (GroundLibrary.TypeToGroundProps) {
                    GroundLibrary.TypeToGroundProps[props.ObjectType] = props;
                }

                lock (GroundLibrary.TypeToTextureData) {
                    GroundLibrary.TypeToTextureData[props.ObjectType] = new TextureData(ground);
                }

                lock (GroundLibrary.IdToTileType) {
                    GroundLibrary.IdToTileType[props.ObjectId] = props.ObjectType;
                }
            } catch (Exception e) {
                Log.Error($"Ground '{ground.Attribute("id")?.Value}' error: {e.Message}");
            }
    }

    private static void ParseObjectsXml(XElement root) {
        foreach (var obj in root.Elements("Object"))
            try {
                var props = new ObjectProperties(obj);

                lock (ObjectLibrary.TypeToObjectProps) {
                    ObjectLibrary.TypeToObjectProps[props.ObjectType] = props;
                }

                lock (ObjectLibrary.TypeToTextureData) {
                    ObjectLibrary.TypeToTextureData[props.ObjectType] = new TextureData(obj);
                }

                lock (ObjectLibrary.IdToObjectType) {
                    ObjectLibrary.IdToObjectType[props.ObjectId] = props.ObjectType;
                    if (!string.IsNullOrWhiteSpace(props.DisplayId) &&
                        props.DisplayId != props.ObjectId)
                        ObjectLibrary.IdToObjectType.TryAdd(props.DisplayId, props.ObjectType);
                }

                if (props.Class == "Player")
                    lock (ObjectLibrary.TypeToClassProps) {
                        ObjectLibrary.TypeToClassProps[props.ObjectType] = props.PlayerProperties;
                    }

                if (props.Skin) {
                    var skinTuple = new Tuple<ushort, ushort>(props.PlayerClassType, props.ObjectType);
                    lock (ObjectLibrary.TypeToSkins) {
                        if (!ObjectLibrary.TypeToSkins.Contains(skinTuple))
                            ObjectLibrary.TypeToSkins.Add(skinTuple);
                    }
                }

                if (obj.HasElement("Item"))
                    lock (ObjectLibrary.TypeToItem) {
                        ObjectLibrary.TypeToItem[props.ObjectType] = new ItemDesc(props.ObjectType, obj);
                    }
            } catch (Exception e) {
                Log.Error($"Object '{obj.Attribute("id")?.Value}' error: {e.Message}");
            }
    }

    private static async Task LoadTexturesAsync() {
        Log.Info("Fetching /resources/textures...");
        var bytes = await AppEngineClient.SendRequestRaw("/resources/textures");
        if (bytes == null || bytes.Length == 0) {
            Log.Warn("No data from /resources/textures.");
            return;
        }

        using var reader = new NetworkReader(new MemoryStream(bytes), false);
        var count = reader.ReadInt32();
        Log.Info($"{count} remote texture(s) received.");

        var textures = new Dictionary<string, byte[]>(count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
            try {
                var id = reader.ReadUTF();
                var length = reader.ReadInt32();
                var png = reader.ReadBytes(length);
                textures[id] = png;
            } catch (Exception e) {
                Log.Error($"Texture {i} read error: {e.Message}");
            }

        Log.Info(
            $"Remote textures staged ({textures.Count} entries). Apply after TextureData.IsRemote support is added.");
    }

    private static async Task LoadEmotesAsync() {
        Log.Info("Fetching /resources/emotes...");
        var bytes = await AppEngineClient.SendRequestRaw("/resources/emotes");
        if (bytes == null || bytes.Length == 0) return;
        var xmlString = Encoding.UTF8.GetString(bytes);

        if (string.IsNullOrEmpty(xmlString)) {
            Log.Warn("No emotes from server.");
            return;
        }

        try {
            var xml = XElement.Parse(xmlString);
            var registered = 0;
            foreach (var emote in xml.Elements("Emote")) {
                var name = emote.Value;
                var file = (string)emote.Attribute("file");
                var index = (int?)emote.Attribute("index") ?? 0;
                var scale = (float?)emote.Attribute("scale") ?? 1f;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(file)) continue;
                registered++;
            }

            Log.Info($"{registered} emote(s) parsed.");
        } catch (Exception e) {
            Log.Error($"Emotes parse error: {e.Message}");
        }
    }

    private static bool ParseSize(string input, out int w, out int h) {
        w = h = 0;
        if (string.IsNullOrEmpty(input)) return false;
        var parts = input.Split('x');
        return parts.Length == 2 && int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h);
    }
}