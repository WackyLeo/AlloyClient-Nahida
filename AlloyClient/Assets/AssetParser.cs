using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AlloyClient.Engine.Common;
using AlloyClient.Engine.Graphics;
using Common;
using Common.Structs;

namespace AlloyClient.Assets;

public sealed class TextureData {
    private static readonly Logger Log = new(typeof(TextureData));
    public Dictionary<int, TextureData> AltTextures;
    public AnimationAtlasData AnimatedTextures;
    public TextureData CornerTexture;
    public Color DominantColor = Color.Transparent;

    public TextureData EdgeTexture;
    public AtlasData? EditorTexture;

    public bool HasAnimationData;
    public TextureData InnerCornerTexture;
    public Texture OverrideTexture;
    public TextureData[] RandomTextures;

    public AtlasData Texture;
    public TextureData TopTexture;

    public TextureData() {
    }

    public TextureData(XElement xml) {
        if (xml.GetElement("Texture", out var elem)) {
            var file = elem.GetValue<string>("File");
            var index = (int)elem.GetValue<uint>("Index");

            var atlasData = Main.Atlas.GetAtlasData(file, index);

            if (atlasData.W == 0 && atlasData.H == 0) {
                if (SpriteSheetLibrary.TryGetSprite(file, index, out var sheetData)) {
                    Texture = sheetData;
                    var entry = SpriteSheetLibrary.GetEntry(file);
                    OverrideTexture = entry?.Texture;
                } else {
                    Texture = atlasData;
                }
            } else {
                Texture = atlasData;
                DominantColor = Main.Atlas.GetDominantColor(file, index);
            }
        }

        if (xml.GetElement("Top", out elem)) TopTexture = new TextureData(elem);

        if (xml.GetElement("AnimatedTexture", out elem)) {
            var file = elem.GetValue<string>("File");
            var index = elem.GetValue<int>("Index");

            var animData = Main.Atlas.GetAnimationAtlasData(file, index);

            if (animData.FaceRight == null || animData.FaceRight.Length == 0) {
                var sheetEntry = SpriteSheetLibrary.GetEntry(file);
                if (sheetEntry != null) {
                    AnimatedTextures = BuildAnimationFromSheet(sheetEntry, index);
                    OverrideTexture = sheetEntry.Texture;
                    HasAnimationData = AnimatedTextures.FaceRight != null && AnimatedTextures.FaceRight.Length > 0;
                } else {
                    HasAnimationData = false;
                    //Log.Warn($"Animated sheet '{file}'[{index}] not found in atlas or sheet library.");
                }
            } else {
                AnimatedTextures = animData;
                HasAnimationData = true;
            }
        }

        var randomTextureNode = xml.Element("RandomTexture");
        if (randomTextureNode != null) {
            var texElems = Enumerable.ToArray(randomTextureNode.Elements("Texture"));
            RandomTextures = new TextureData[texElems.Length];
            for (var i = 0; i < texElems.Length; i++) {
                var wrapper = new XElement("DummyWrapper", texElems[i]);
                RandomTextures[i] = new TextureData(wrapper);
            }
        }

        if (xml.GetElements("AltTexture", out var elems)) {
            AltTextures = [];
            foreach (var e in elems) {
                var id = e.GetAttribute("id", 0);
                if (AltTextures.ContainsKey(id))
                    Log.Warn($"[Dupe AltTextureId] object: {xml.GetAttribute("id", "")} has dupe id: {id}");
                AltTextures[id] = new TextureData(e);
            }
        }

        if (xml.GetElement("EditorTexture", out elem))
            EditorTexture = Main.Atlas.GetAtlasData(elem.GetValue<string>("File"), elem.GetValue<int>("Index"));

        if (xml.GetElement("Edge", out elem)) EdgeTexture = new TextureData(elem);

        if (xml.GetElement("Corner", out elem)) CornerTexture = new TextureData(elem);

        if (xml.GetElement("InnerCorner", out elem)) InnerCornerTexture = new TextureData(elem);
    }

    public bool IsRemoteSheet => OverrideTexture != null;

    private static AnimationAtlasData BuildAnimationFromSheet(SpriteSheetLibrary.SheetEntry entry, int index)
    {
        var framesPerRow = entry.SpritesPerRow;
        var totalRows = entry.TextureHeight / entry.SpriteHeight;
        var isFullFacing = totalRows % 3 == 0;
        var baseRow = isFullFacing ? index * 3 : index;

        Log.Info($"[AnimBuild] tex={entry.TextureWidth}x{entry.TextureHeight} sprite={entry.SpriteWidth}x{entry.SpriteHeight} framesPerRow={framesPerRow} totalRows={totalRows} isFullFacing={isFullFacing} index={index} baseRow={baseRow}");

        AnimationAtlasData anim = new();
        anim.FaceRight = BuildFrameRow(entry, baseRow, framesPerRow);

        if (isFullFacing)
        {
            anim.FaceDown = BuildFrameRow(entry, baseRow + 1, framesPerRow);
            anim.FaceUp = BuildFrameRow(entry, baseRow + 2, framesPerRow);
        }
        else
        {
            anim.FaceDown = anim.FaceRight;
            anim.FaceUp = anim.FaceRight;
        }

        return anim;
    }

    private static AtlasData[] BuildFrameRow(SpriteSheetLibrary.SheetEntry entry, int row, int frameCount)
    {
        var frames = new AtlasData[frameCount];

        for (var f = 0; f < frameCount; f++)
        {
            frames[f] = new AtlasData
            {
                U = (float)(f * entry.SpriteWidth) / entry.TextureWidth,
                V = (float)(row * entry.SpriteHeight) / entry.TextureHeight,
                W = (float)entry.SpriteWidth / entry.TextureWidth,
                H = (float)entry.SpriteHeight / entry.TextureHeight
            };
        }

        return frames;
    }

    public AtlasData GetTexture(out Color color, bool random = false, int id = 0) {
        if (RandomTextures == null) {
            color = DominantColor;
            return Texture;
        }

        if (random) id = Random.Shared.Next(0, RandomTextures.Length);
        return RandomTextures[id].GetTexture(out color, false, id);
    }

    public AtlasData GetTexture(bool random = false, int id = 0) {
        if (RandomTextures == null) return Texture;
        if (random) id = Random.Shared.Next(0, RandomTextures.Length);
        return RandomTextures[id].GetTexture(false, id);
    }

    public AtlasData GetTopTexture() {
        if (TopTexture == null) return new AtlasData();
        return TopTexture.GetTexture();
    }

    public bool GetAltTexture(int id, out TextureData data) {
        if (AltTextures == null || !AltTextures.ContainsKey(id)) {
            Log.Warn($"[Missing AltTextureId] object: {id}");
            data = null;
            return false;
        }

        AltTextures.TryGetValue(id, out data);
        return true;
    }
}