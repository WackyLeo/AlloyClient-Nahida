using System;
using System.Collections.Generic;
using AlloyClient.Assets;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public sealed class TypeConnectedWall : RenderBase {
    private readonly float _bI;
    private readonly float _h;
    private readonly float _tI;

    private readonly Vector4 _uvDot, _uvShortLine, _uvL, _uvLine, _uvT, _uvCross;
    internal readonly List<CaveWallVertex> Triangles = new(128);
    private bool _geometryDirty = true;
    private int _lastConnectType = -1;
    private bool _positionSet;

    private int _tileX;
    private int _tileY;

    public TypeConnectedWall(Entity entity, float bI = 0.5f, float tI = 0.25f, float h = 1.0f) {
        Entity = entity;
        _bI = bI;
        _tI = tI;
        _h = h;
        Extra = new ExtraData(RenderConfig.TypeWall, RenderConfig.Shade);
        var props = entity.Properties;
        _uvDot = props.DotTexture.W > 0 ? props.DotTexture.ToVector4(true) : UV;
        _uvShortLine = props.ShortLineTexture.W > 0 ? props.ShortLineTexture.ToVector4(true) : UV;
        _uvL = props.LTexture.W > 0 ? props.LTexture.ToVector4(true) : UV;
        _uvLine = props.LineTexture.W > 0 ? props.LineTexture.ToVector4(true) : UV;
        _uvT = props.TTexture.W > 0 ? props.TTexture.ToVector4(true) : UV;
        _uvCross = props.CrossTexture.W > 0 ? props.CrossTexture.ToVector4(true) : UV;
        SetTexture(entity.GetTexture());
    }

    public override ModelType ModelType => ModelType.PbCaveWall;
    public override bool HasShadow => false;

    public override void SetPosition(float x, float y, float z = 0) {
        Position.X = x;
        Position.Y = y;
        Position.Z = z;

        var newTileX = (int)x;
        var newTileY = (int)y;
        if (_positionSet && newTileX == _tileX && newTileY == _tileY) return;
        _tileX = newTileX;
        _tileY = newTileY;
        _positionSet = true;
        _geometryDirty = true;
    }

    public void SetConnectType(int connectType) {
        if (Entity.ConnectType == connectType && !_geometryDirty) return;
        Entity.ConnectType = connectType;
        _geometryDirty = true;
    }

    public override void SetVisibility(bool visible) {
        Visible = visible;
    }

    public override void SetDepth(float depth) {
        if (Math.Abs(Extra.SortId - depth) < 0.001f) return;
        base.SetDepth(depth);
        _geometryDirty = true;
    }

    public override void SetAlpha(float alpha) {
        throw new NotSupportedException("ConnectedWall does not support alpha");
    }

    public override void SetName(string name) {
    }

    public override void SetTexture(AtlasData texture, bool attackFrame) {
        UV = texture.ToVector4(true);
        _geometryDirty = true;
    }

    public override void Draw() {
        if (Entity.ConnectType != _lastConnectType) {
            _lastConnectType = Entity.ConnectType;
            _geometryDirty = true;
        }

        if (_geometryDirty && _positionSet) {
            Triangles.Clear();
            TypeConnectedWallGeo.Build(
                Position.X, Position.Y,
                Entity.ConnectType,
                UV, Extra.SortId,
                Triangles,
                _bI, _tI, _h,
                _uvDot, _uvShortLine, _uvL, _uvLine, _uvT, _uvCross);
            _geometryDirty = false;
        }

        if (Triangles.Count > 0) Render.DrawConnectedWall(this);
    }
}