using System;
using System.Collections.Generic;
using AlloyClient.Assets;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public sealed class TypeCaveWall : RenderBase {

    public override ModelType ModelType => ModelType.PbCaveWall;
    public override bool HasShadow => false;

    internal readonly List<CaveWallVertex> Triangles = new(128);

    private int _tileX;
    private int _tileY;
    private bool _positionSet;
    private bool _geometryDirty = true;

    public TypeCaveWall(Entity entity) {
        Entity = entity;
        Extra  = new ExtraData(RenderConfig.TypeWall, RenderConfig.Shade);
        SetTexture(entity.GetTexture());
    }

    public override void SetPosition(float x, float y, float z = 0) {
        Position.X = x;
        Position.Y = y;
        Position.Z = z;

        int newTileX = (int)x;
        int newTileY = (int)y;

        if (!_positionSet || newTileX != _tileX || newTileY != _tileY) {
            _tileX = newTileX;
            _tileY = newTileY;
            _positionSet = true;
            _geometryDirty = true;
        }
    }
    
    public void SetConnectType(int connectType) {
        if (Entity.ConnectType == connectType && !_geometryDirty) return;
        Entity.ConnectType = connectType;
        _geometryDirty = true;
    }

    public override void SetVisibility(bool visible) => Visible = visible;

    public override void SetDepth(float depth) => Extra.SortId = depth;

    public override void SetAlpha(float alpha) {
        throw new NotSupportedException("Walls do not support alpha");
    }

    public override void SetName(string name) { }

    public override void SetTexture(AtlasData texture, bool attackFrame) {
        UV = texture.ToVector4(true);
        _geometryDirty = true;
    }

    private int _lastConnectType = -1;

    public override void Draw() {
        if (Entity.ConnectType != _lastConnectType) {
            _lastConnectType = Entity.ConnectType;
            _geometryDirty = true;
        }

        if (_geometryDirty && _positionSet) {
            Rebuild();
            _geometryDirty = false;
        }

        if (Triangles.Count == 0) return;
        Render.DrawCaveWall(this);
    }

    private void Rebuild() {
        Triangles.Clear();
        TypeCaveWallGeo.Build(
            Position.X, Position.Y,
            Entity.ConnectType,
            UV,
            Triangles);
    }
}