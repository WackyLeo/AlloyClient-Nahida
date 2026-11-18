using System;
using System.Collections.Generic;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Editor;

/// <summary>
/// A sprite made of many quads that all share one texture type. Used for the map layers and overlays.
/// Keep it under <see cref="MaxQuads"/> quads, the ui renderer flushes per sprite and can't split one.
/// </summary>
public sealed class QuadBatch : Sprite {
    public const int MaxQuads = 2048;

    private readonly List<VertexUi> _vertices = [];

    public QuadBatch(TextureType textureType) {
        TextureId = textureType;
        if (textureType == TextureType.GameAtlas) {
            // Disable the object shader's bottom shading (x > 1 never triggers), outline (w) and glow (Extra2.z)
            Extra1 = new Vector4(10f, 0f, 1f, -1f);
            Extra2 = new Vector4(0f, 0f, -1f, 0f);
        }
    }

    public int QuadCount => _vertices.Count / 4;

    public void Clear() => _vertices.Clear();

    public void AddTexturedQuad(float x, float y, float w, float h, AtlasData uv) {
        _vertices.Add(new VertexUi(new Vector2(x, y + h), new Vector2(uv.U, uv.V + uv.H)));
        _vertices.Add(new VertexUi(new Vector2(x, y), new Vector2(uv.U, uv.V)));
        _vertices.Add(new VertexUi(new Vector2(x + w, y), new Vector2(uv.U + uv.W, uv.V)));
        _vertices.Add(new VertexUi(new Vector2(x + w, y + h), new Vector2(uv.U + uv.W, uv.V + uv.H)));
    }

    /// <param name="argb">0xAARRGGBB</param>
    public void AddColorQuad(float x, float y, float w, float h, uint argb) {
        var c = Pack(argb);
        _vertices.Add(new VertexUi(new Vector2(x, y + h), c));
        _vertices.Add(new VertexUi(new Vector2(x, y), c));
        _vertices.Add(new VertexUi(new Vector2(x + w, y), c));
        _vertices.Add(new VertexUi(new Vector2(x + w, y + h), c));
    }

    /// <summary>Rectangle outline made of 4 thin quads.</summary>
    public void AddOutline(float x, float y, float w, float h, float thickness, uint argb) {
        AddColorQuad(x, y, w, thickness, argb);
        AddColorQuad(x, y + h - thickness, w, thickness, argb);
        AddColorQuad(x, y + thickness, thickness, h - thickness * 2, argb);
        AddColorQuad(x + w - thickness, y + thickness, thickness, h - thickness * 2, argb);
    }

    // Shader unpacks colors as ABGR
    private static uint Pack(uint argb) {
        var a = (argb >> 24) & 0xFF;
        var r = (argb >> 16) & 0xFF;
        var g = (argb >> 8) & 0xFF;
        var b = argb & 0xFF;
        if (a == 0) a = 1; // a vertex color of 0 means "use the sprite color"
        return a << 24 | b << 16 | g << 8 | r;
    }

    /// <summary>Uploads the quads added since the last <see cref="Clear"/>.</summary>
    public void Commit() {
        var quads = Math.Min(QuadCount, MaxQuads);
        if (quads == 0) {
            VertexData = [];
            Indices = [];
            SetGraphicsBuffer();
            return;
        }

        if (VertexData.Length != quads * 4) VertexData = new VertexUi[quads * 4];
        _vertices.CopyTo(0, VertexData, 0, quads * 4);

        if (Indices.Length != quads * 6) {
            Indices = new ushort[quads * 6];
            for (var i = 0; i < quads; i++) {
                var i6 = i * 6;
                var i4 = (ushort)(i * 4);
                Indices[i6] = i4;
                Indices[i6 + 1] = (ushort)(i4 + 1);
                Indices[i6 + 2] = (ushort)(i4 + 2);
                Indices[i6 + 3] = i4;
                Indices[i6 + 4] = (ushort)(i4 + 2);
                Indices[i6 + 5] = (ushort)(i4 + 3);
            }
        }

        SetGraphicsBuffer();
    }
}

/// <summary>Any number of quads, split over as many <see cref="QuadBatch"/> children as needed.</summary>
public sealed class QuadLayer : Sprite {
    private readonly TextureType _type;
    private readonly List<QuadBatch> _batches = [];
    private int _current;

    public QuadLayer(TextureType type) {
        _type = type;
    }

    public void Clear() {
        foreach (var b in _batches) b.Clear();
        _current = 0;
    }

    private QuadBatch Current() {
        while (true) {
            if (_current >= _batches.Count) {
                var batch = new QuadBatch(_type);
                _batches.Add(batch);
                AddChild(batch);
            }

            if (_batches[_current].QuadCount < QuadBatch.MaxQuads) return _batches[_current];
            _current++;
        }
    }

    public void AddColorQuad(float x, float y, float w, float h, uint argb) => Current().AddColorQuad(x, y, w, h, argb);

    public void AddTexturedQuad(float x, float y, float w, float h, AtlasData uv) => Current().AddTexturedQuad(x, y, w, h, uv);

    public void AddOutline(float x, float y, float w, float h, float thickness, uint argb) {
        AddColorQuad(x, y, w, thickness, argb);
        AddColorQuad(x, y + h - thickness, w, thickness, argb);
        AddColorQuad(x, y + thickness, thickness, h - thickness * 2, argb);
        AddColorQuad(x + w - thickness, y + thickness, thickness, h - thickness * 2, argb);
    }

    public void Commit() {
        foreach (var b in _batches) b.Commit();
    }
}
