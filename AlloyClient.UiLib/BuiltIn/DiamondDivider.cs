using System;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;

namespace AlloyClient.UiLib.BuiltIn;

public struct DiamondDividerConfig {
    public int X = 0;
    public int Y = 0;
    public int LineLength = 100;
    public int LineThickness = 2;
    public int DiamondSize = 6;
    public uint Color = 0x363636;
    public float Alpha = 1.0f;
    public UiAnchor Anchor = UiAnchor.LeftTop;

    public DiamondDividerConfig() {
    }
}

public class DiamondDivider : Sprite {
    private readonly int _diamondSize;
    private int _lineLength;
    private readonly int _lineThickness;

    public DiamondDivider(DiamondDividerConfig config) {
        X = config.X;
        Y = config.Y;
        _lineLength = config.LineLength;
        _lineThickness = config.LineThickness;
        _diamondSize = config.DiamondSize;

        SetColor(config.Color);
        Alpha = config.Alpha;
        SetAnchor(config.Anchor);
        MouseEnabled = false;

        TextureId = (TextureType)12;
        ResizeBackBuffer();
        FillData();
    }

    private void ResizeBackBuffer() {
        VertexData = new VertexUi[4];
        Indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    }

    private void FillData() {
        var totalW = _lineLength + _diamondSize * 2f;
        var totalH = Math.Max(_lineThickness, _diamondSize * 2f);
        VertexData[0] = new VertexUi(new Vector2(0, 0), new Vector2(0, 0));
        VertexData[1] = new VertexUi(new Vector2(totalW, 0), new Vector2(1, 0));
        VertexData[2] = new VertexUi(new Vector2(totalW, totalH), new Vector2(1, 1));
        VertexData[3] = new VertexUi(new Vector2(0, totalH), new Vector2(0, 1));
        Extra1 = new Vector4(_lineLength, _lineThickness, _diamondSize, 0);

        SetGraphicsBuffer();
    }

    public void Resize(int length) {
        _lineLength = length;
        FillData();
    }
}