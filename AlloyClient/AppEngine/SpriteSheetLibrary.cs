using System;
using System.Collections.Generic;
using AlloyClient.Engine.Graphics;
using Common;
using Common.Structs;

namespace AlloyClient.Assets;

public static class SpriteSheetLibrary {
    private static readonly Logger Log = new(nameof(SpriteSheetLibrary));

    public class SheetEntry {
        public Texture Texture;
        public int SpriteWidth;
        public int SpriteHeight;
        public int TextureWidth;
        public int TextureHeight;
        public int SpritesPerRow => TextureWidth / SpriteWidth;
    }

    private static readonly Dictionary<string, SheetEntry> Sheets = new(StringComparer.OrdinalIgnoreCase);

    public static bool HasSheet(string id) => Sheets.ContainsKey(id);

    public static void AddSheet(string id, byte[] pngBytes, int spriteW, int spriteH) {
        
        if (Sheets.ContainsKey(id)) return;

        var (texW, texH) = ReadPngDimensions(pngBytes);

        var entry = new SheetEntry {
            Texture = new Texture(pngBytes),
            SpriteWidth = spriteW,
            SpriteHeight = spriteH,
            TextureWidth = texW,
            TextureHeight = texH
        };

        Sheets[id] = entry;
        Log.Info($"Registered sheet '{id}' ({texW}x{texH}, sprite {spriteW}x{spriteH}, {texW / spriteW * (texH / spriteH)} sprites)");
    }
    
    public static bool TryGetSprite(string id, int index, int spriteW, int spriteH, out AtlasData data) {
        data = default;
        if (!Sheets.TryGetValue(id, out var entry)) return false;

        var baseTileW = entry.SpriteWidth;
        var baseTileH = entry.SpriteHeight;
        var tilesPerRow = entry.TextureWidth / baseTileW;

        var tileCol = index % tilesPerRow;
        var tileRow = index / tilesPerRow;

        data = new AtlasData {
            U = (float)(tileCol * baseTileW) / entry.TextureWidth,
            V = (float)(tileRow * baseTileH) / entry.TextureHeight,
            W = (float)spriteW / entry.TextureWidth,
            H = (float)spriteH / entry.TextureHeight,
        };
        return true;
    }
    
    public static bool TryGetSprite(string id, int index, out AtlasData data) {
        data = default;

        if (!Sheets.TryGetValue(id, out var entry)) {
            return false;
        }

        var col = index % entry.SpritesPerRow;
        var row = index / entry.SpritesPerRow;

        data = new AtlasData {
            U = (float)(col * entry.SpriteWidth) / entry.TextureWidth,
            V = (float)(row * entry.SpriteHeight) / entry.TextureHeight,
            W = (float)entry.SpriteWidth / entry.TextureWidth,
            H = (float)entry.SpriteHeight / entry.TextureHeight
        };

        return true;
    }

    public static AtlasData GetSprite(string id, int index) {
        TryGetSprite(id, index, out var data);
        return data;
    }

    public static SheetEntry GetEntry(string id) {
        Sheets.TryGetValue(id, out var entry);
        return entry;
    }

    public static IReadOnlyDictionary<string, SheetEntry> GetAll() => Sheets;
    private static (int width, int height) ReadPngDimensions(byte[] png) {
        if (png.Length < 24) return (0, 0);
        var w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        var h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return (w, h);
    }
}
