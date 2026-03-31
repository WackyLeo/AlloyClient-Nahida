using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Data;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;

namespace AlloyClient.UiLib.BuiltIn;

public struct TextConfig {
    public string Text = "";
    public float FontSize = 1f;
    public FontType FontType = FontType.Normal;
    public int X = 0;
    public int Y = 0;
    public int MaxWidth = -1;
    public float OutlineThickness = 0;
    public float Glow = 0.5f;
    public uint Color = 0xFFFFFF;
    public uint OutlineColor = 0x0;
    public float Alpha = 1.0f;
    public UiAnchor Anchor = UiAnchor.LeftTop;
    public int ShaderEffect = 0;

    public TextConfig() {
    }
}

public static class TextEffect {
    public const int None = 0;
    public const int Fire = 1;
    public const int Rainbow = 2;
    public const int Glitch = 3;
    public const int Electric = 4;
    public const int Void = 5;
}

public sealed class SimpleText : Sprite {
    private readonly BitmapFont _font;

    private readonly float _glow;
    private readonly int _maxWidth;

    private int _capacity;
    private float _fontScale;
    private float _lineWrapStart;
    private float _outlineThickness;

    public SimpleText(TextConfig config) {
        Text = config.Text ?? "";

        _glow = config.Glow;
        _fontScale = config.FontSize;
        _font = UiRender.GetFont(config.FontType);
        _maxWidth = config.MaxWidth;

        X = config.X;
        Y = config.Y;
        Alpha = config.Alpha;

        Extra2 = new Vector4(config.ShaderEffect, 0, 0, 0);
        _outlineThickness = _font.ValidateOutlineSize(config.OutlineThickness);

        SetColor(config.Color);
        SetColorSecondary(config.OutlineColor);
        SetAnchor(config.Anchor);

        TextureId = TextureType.Text;

        ResizeBackBuffer(Text.Length);
        FillData();
    }

    public string Text { get; private set; }

    private void ResizeBackBuffer(int size) {
        if (size <= _capacity) return;

        _capacity = size;

        VertexData = new VertexUi[size * 4];
        Indices = new ushort[size * 6];

        for (var i = 0; i < size; i++) {
            var idx6 = i * 6;
            var idx4 = i * 4;

            Indices[idx6 + 0] = (ushort)(idx4 + 0);
            Indices[idx6 + 1] = (ushort)(idx4 + 1);
            Indices[idx6 + 2] = (ushort)(idx4 + 2);
            Indices[idx6 + 3] = (ushort)(idx4 + 0);
            Indices[idx6 + 4] = (ushort)(idx4 + 2);
            Indices[idx6 + 5] = (ushort)(idx4 + 3);
        }
    }

    private void FillData() {
        if (Text == null) return;

        var scale = _fontScale;
        var pen = new Vector2(0f, _font.Ascender * scale);

        var lastSpaceIndex = 0;
        var len = Text.Length;
        var idx = 0;

        for (var i = 0; i < len; i++) {
            var c = Text[i];

            if (c == ' ')
                lastSpaceIndex = i;

            switch (c) {
                case '\n':
                    pen.X = _lineWrapStart;
                    pen.Y += _font.LineHeight * scale;
                    continue;

                case '\r':
                    continue;
            }

            if (!_font.Glyphs.TryGetValue(c, out var glyph))
                continue;

            var uv = glyph.UV;
            var pos = glyph.Position;

            var baseIdx = idx * 4;

            if (baseIdx + 3 >= VertexData.Length)
                break;

            VertexData[baseIdx + 0] = new VertexUi(new Vector2(pen.X + pos.X0 * scale, pen.Y - pos.Y1 * scale),
                new Vector2(uv.X0, uv.Y1));
            VertexData[baseIdx + 1] = new VertexUi(new Vector2(pen.X + pos.X0 * scale, pen.Y - pos.Y0 * scale),
                new Vector2(uv.X0, uv.Y0));
            VertexData[baseIdx + 2] = new VertexUi(new Vector2(pen.X + pos.X1 * scale, pen.Y - pos.Y0 * scale),
                new Vector2(uv.X1, uv.Y0));
            VertexData[baseIdx + 3] = new VertexUi(new Vector2(pen.X + pos.X1 * scale, pen.Y - pos.Y1 * scale),
                new Vector2(uv.X1, uv.Y1));

            idx++;

            if (i < len - 1) {
                _font.Kernings.TryGetValue((c, Text[i + 1]), out var kern);
                pen.X += kern * scale;
            }

            pen.X += glyph.Advance * scale;

            // ✅ Word wrap
            if (_maxWidth > -1 && pen.X >= _maxWidth) {
                if (lastSpaceIndex > 0) {
                    idx -= i - lastSpaceIndex;
                    i = lastSpaceIndex;
                    lastSpaceIndex = 0;
                }

                pen.X = _lineWrapStart;
                pen.Y += _font.LineHeight * scale;
            }
        }

        SetGraphicsBuffer();

        Extra1.X = _outlineThickness;
        Extra1.Z = _glow;
    }

    public void SetText(string text) {
        if (text == Text)
            return;

        Text = text;

        ResizeBackBuffer(text.Length);
        FillData();
    }

    public void SetFontSize(float size) {
        if (size == _fontScale)
            return;

        _fontScale = size;
        FillData();
    }

    public void SetOutlineSize(float size) {
        var validated = _font.ValidateOutlineSize(size);
        if (validated == _outlineThickness)
            return;

        _outlineThickness = validated;
        FillData();
    }

    public void OffsetLineWrapBy(float x) {
        if (_lineWrapStart == x)
            return;

        _lineWrapStart = x;
        FillData();
    }

    public void SetEffect(int effect) {
        if ((int)Extra2.X == effect)
            return;

        Extra2 = new Vector4(effect, 0, 0, 0);
    }

    private enum RenderType {
        Normal = 0,
        Small = 1
    }
}