using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Objects.Enums;
using AlloyClient.UiLib.Data;
using AlloyClient.UiLib.Enums;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Utils;

public static class TextureHelper
{
    public static TextureInfo FromUiAtlas(string lookup, int index = 0, bool padding = true)
    {
        var uv = Main.UiAtlas.GetAtlasData(lookup, index);
        if (!padding) uv.RemovePadding();
        return new TextureInfo(uv.ToPosition(), TextureType.UiAtlas);
    }

    public static TextureInfo FromGameAtlas(string lookup, int index, bool padding = true)
    {
        var uv = Main.Atlas.GetAtlasData(lookup, index);
        if (!padding) uv.RemovePadding();
        return new TextureInfo(uv.ToPosition(), TextureType.GameAtlas);
    }

    public static TextureInfo FromGameAtlas(string lookup, int index, uint removePadding)
    {
        var uv = Main.Atlas.GetAtlasData(lookup, index);
        uv.RemovePadding(removePadding);
        return new TextureInfo(uv.ToPosition(), TextureType.GameAtlas);
    }

    public static TextureInfo FromGameAtlas(ushort id) {
        if (!ObjectLibrary.TypeToTextureData.TryGetValue(id, out var data))
            return FromGameAtlas("invisible", 0);
    
        var uv = data.GetTexture();
        if (id == 323) {
            Console.WriteLine($"[Texture] id=0x{id:X4} uv={uv.U},{uv.V},{uv.W},{uv.H}");
        }
        return new TextureInfo(uv.ToPosition(), TextureType.GameAtlas);
    }
    public static TextureInfo Create(AtlasData uv, TextureType type, bool padding = true)
    {
        if (!padding) uv.RemovePadding();
        return new TextureInfo(uv.ToPosition(), type);
    }

    public static AtlasPosition ToPosition(this AtlasData data)
    {
        return new AtlasPosition(data.U, data.V, data.W, data.H);
    }
    
    extension(AnimationAtlasData data)
    {
        public AtlasData TextureFromFacing(float angle, AnimationType action, float idx, out bool attackFrame, out bool flipped)
        {
            attackFrame = false;
            flipped = false;

            var ca = MathUtils.BoundToPi(angle - -Camera.CameraAngle);
            var sec = (int)(ca / MathHelper.PiOver4 + 4) % 8;
            var frames = sec switch
            {
                0 or 0 => data.FaceRight,
                1 or 2 => data.FaceUp,
                3 or 3 => data.FaceRight,
                5 or 6 => data.FaceDown,
                _ => data.FaceRight
            };

            if (frames == null || frames.Length == 0)
                frames = data.FaceRight;
            if (frames == null || frames.Length == 0)
                frames = data.FaceDown;
            if (frames == null || frames.Length == 0)
                return new AtlasData();

            flipped = sec is 0 or 7;
            bool isSide = sec is 0 or 3 or 4 or 7;

            var i = action switch
            {
                AnimationType.Attack => 4 + (int)(idx * 2),
                AnimationType.Walk => isSide
                    ? (int)(idx * 2) % 2
                    : new[] { 0, 1, 0, 2 }[(int)(idx * 4) % 4],
                _ => 0
            };

            if (i >= frames.Length)
                i = isSide ? (int)(idx * 2) % 2 : new[] { 0, 1, 0, 2 }[(int)(idx * 4) % 4];

            attackFrame = i == 5;
            return frames[i];
        }
    }
}