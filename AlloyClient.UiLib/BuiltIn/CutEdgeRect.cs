using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;

namespace AlloyClient.UiLib.BuiltIn;

public struct CutEdgeConfig {
    public int X = 0;
    public int Y = 0;
    public int Width = 0;
    public int Height = 0;
    public int CutX = 0;
    public int CutY = 0;
    public CutEdges Cuts = CutEdges.All;
    public uint Color = 0x000000;
    public float Alpha = 1.0f;
    public UiAnchor Anchor = UiAnchor.LeftTop;

    public bool MouseEnabled = false;

    public CutEdgeConfig() {
    }
}

public sealed class CutEdgeRect : Sprite {
    private readonly CutEdges _cuts;

    private readonly int _cx;
    private readonly int _cy;
    private int _h;

    private int _w;

    public CutEdgeRect(CutEdgeConfig config) {
        X = config.X;
        Y = config.Y;
        _w = config.Width;
        _h = config.Height;
        _cx = config.CutX;
        _cy = config.CutY;
        _cuts = config.Cuts;

        SetColor(config.Color);
        Alpha = config.Alpha;
        SetAnchor(config.Anchor);
        MouseEnabled = config.MouseEnabled;

        TextureId = (TextureType)13;
        SetHitboxType(CollisionType.Square);

        ResizeBackBuffer();
        FillData();
    }

    private void ResizeBackBuffer() {
        VertexData = new VertexUi[4];
        Indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    }

    private void FillData() {
        VertexData[0] = new VertexUi(new Vector2(0, 0), new Vector2(0, 0));
        VertexData[1] = new VertexUi(new Vector2(_w, 0), new Vector2(1, 0));
        VertexData[2] = new VertexUi(new Vector2(_w, _h), new Vector2(1, 1));
        VertexData[3] = new VertexUi(new Vector2(0, _h), new Vector2(0, 1));

        Extra1 = new Vector4(_w, _h, _cx, _cy);

        var cutMask = 0;
        if ((_cuts & CutEdges.TopLeft) != 0) cutMask |= 1;
        if ((_cuts & CutEdges.TopRight) != 0) cutMask |= 2;
        if ((_cuts & CutEdges.BottomLeft) != 0) cutMask |= 4;
        if ((_cuts & CutEdges.BottomRight) != 0) cutMask |= 8;

        Extra2 = new Vector4(cutMask, 0, 0, 0);
        SetGraphicsBuffer();

        SelfContentWidth = _w;
        SelfContentHeight = _h;
        UpdateBounds();
    }

    public void Resize(int width, int height) {
        _w = width;
        _h = height;
        FillData();
    }
}