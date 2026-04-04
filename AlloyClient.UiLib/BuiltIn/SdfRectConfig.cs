using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;

namespace AlloyClient.UiLib.BuiltIn;

public struct SdfRectConfig {
    public int X = 0;
    public int Y = 0;
    public int Width = 0;
    public int Height = 0;
    public int Cut = 4;
    public CutEdges Cuts = CutEdges.All;
    public uint Color = 0x363636;
    public uint OutlineColor = 0x676767;
    public float OutlineSize = 2f;
    public float ShadowRadius = 3f;
    public float ShadowAlpha = 0.5f;
    public float Alpha = 1.0f;
    public UiAnchor Anchor = UiAnchor.LeftTop;
    public bool MouseEnabled = false;

    public SdfRectConfig() {
    }
}

public class SdfRect : Sprite {
    private readonly int _cut;
    private readonly CutEdges _cuts;
    private int _h;
    private readonly float _outlineSize;
    private readonly float _shadowAlpha;
    private readonly float _shadowRadius;
    private int _w;

    public SdfRect(SdfRectConfig config) {
        X = config.X;
        Y = config.Y;
        _w = config.Width;
        _h = config.Height;
        _cut = config.Cut;
        _cuts = config.Cuts;
        _outlineSize = config.OutlineSize;
        _shadowRadius = config.ShadowRadius;
        _shadowAlpha = config.ShadowAlpha;

        var a = (config.Color >> 24) / 255f;
        SetColor(config.Color & 0x00FFFFFFu, a);
        SetColorSecondary(config.OutlineColor);
        Alpha = config.Alpha;
        SetAnchor(config.Anchor);
        MouseEnabled = config.MouseEnabled;

        TextureId = (TextureType)11;
        SetHitboxType(CollisionType.Square);

        ResizeBackBuffer();
        FillData();
    }

    private void ResizeBackBuffer() {
        VertexData = new VertexUi[4];
        Indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    }

    private void FillData() {
        var pad = _shadowRadius + _outlineSize;

        VertexData[0] = new VertexUi(new Vector2(-pad, -pad), new Vector2(0, 0));
        VertexData[1] = new VertexUi(new Vector2(_w + pad, -pad), new Vector2(1, 0));
        VertexData[2] = new VertexUi(new Vector2(_w + pad, _h + pad), new Vector2(1, 1));
        VertexData[3] = new VertexUi(new Vector2(-pad, _h + pad), new Vector2(0, 1));

        Extra1 = new Vector4(_w, _h, _cut, _shadowRadius);

        var cutMask = 0;
        if ((_cuts & CutEdges.TopLeft) != 0) cutMask |= 1;
        if ((_cuts & CutEdges.TopRight) != 0) cutMask |= 2;
        if ((_cuts & CutEdges.BottomLeft) != 0) cutMask |= 4;
        if ((_cuts & CutEdges.BottomRight) != 0) cutMask |= 8;

        Extra2 = new Vector4(_shadowAlpha, cutMask, _outlineSize, 0);

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