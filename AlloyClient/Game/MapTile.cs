using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AlloyClient.Assets;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Components;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.VertexData;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game;

public class MapTile(int x, int y) {

    public readonly int X = x;
    public readonly int Y = y;

    public ushort Type = Const.DefaultTile;
    public GroundProperties GroundProperties = GroundLibrary.TypeToGroundProps[Const.DefaultTile];
    public TextureData TextureData = GroundLibrary.TypeToTextureData[Const.DefaultTile];

    public double LastDamage;
    public bool HasTop => GroundProperties.TopTD != null;
    public float ZOffset => 0;
    public bool IsElevated => GroundProperties.ZOffset != 0f;

    public Entity OccupiedObject { 
        get;
        set => SetMinimapColor(field = value);
    }

    private Color _color;
    
    private RenderData _data;
    private int _dataCount;
    private TileData _topData;

    private void SetMinimapColor(Entity entity) {
        if (entity != null && entity.Properties.Static && entity.Properties.OccupySquare && !entity.Properties.NoMiniMap) {
            MinimapTexture.UncoverTile(X, Y, entity.GetDominateColor());
        } else {
            MinimapTexture.UncoverTile(X, Y, _color);
        }
    }

    public void DrawTileBase() {
        Render.DrawTiles(_data.AsSpan(0, 1));
    }

    public void DrawTileBlends() {
        if (_dataCount > 1) {
            Render.DrawTiles(_data.AsSpan(1, _dataCount - 1));
        }
    }

    public void DrawTile() {
        Render.DrawTiles(_data.AsSpan(0, _dataCount));
    }

    public void DrawTop() {
        if (HasTop) {
            Render.DrawTiles(MemoryMarshal.CreateReadOnlySpan(ref _topData, 1));
        }
    }

    public void SetType(ushort type) {
        Type = type;
        GroundProperties = GroundLibrary.TypeToGroundProps[type];
        TextureData = GroundLibrary.TypeToTextureData[type];

        var texture = TextureData.GetTexture(out _color, true);
        texture.RemovePadding();
        
        SetMinimapColor(OccupiedObject);

        var offx = GroundProperties.XOffset;
        var offy = GroundProperties.YOffset;

        if (GroundProperties.RandomOffset) {
            offx = (int)(Random.Shared.NextSingle() * texture.RawW()) / (float)texture.RawW();
            offy = (int)(Random.Shared.NextSingle() * texture.RawH()) / (float)texture.RawH();
        }

        var animate = new Vector4(0);
        var animateProp = GroundProperties.Animate;
        switch (animateProp.Type) {
            case GroundAnimate.State.Wave:
                animate.X = animateProp.DeltaX;
                animate.Y = animateProp.DeltaY;
                break;
            case GroundAnimate.State.Flow:
                animate.Z = animateProp.DeltaX;
                animate.W = animateProp.DeltaY;
                break;
        }
        
        // Base tile data
        _data[0] = new TileData(new Vector4(X, Y, offx, offy), texture.ToVector4(), animate, new Vector4(-1));
        _dataCount = 1;

        // Top layer logic
        if (HasTop) {
            var topTexture = GroundProperties.TopTD.GetTexture(out _, true);
            topTexture.RemovePadding();

            var topAnimateProp = GroundProperties.TopAnimate;
            var topAnimate = new Vector4(0);
            switch (topAnimateProp.Type) {
                case GroundAnimate.State.Wave:
                    topAnimate.X = topAnimateProp.DeltaX;
                    topAnimate.Y = topAnimateProp.DeltaY;
                    break;
                case GroundAnimate.State.Flow:
                    topAnimate.Z = topAnimateProp.DeltaX;
                    topAnimate.W = topAnimateProp.DeltaY;
                    break;
            }

            _topData = new TileData(new Vector4(X, Y, offx, offy), topTexture.ToVector4(), topAnimate, new Vector4(-1));
        }
    }

    public void Rebuild(Span<MapTile> tiles) {
        _dataCount = TileBuilder.Build(this, _data.AsSpan(1, 16), tiles) + 1;
    }

    public TileData CloneWithBlend(int x, int y, Vector4 mask) {
        var tile = _data[0];
        tile.Position.X = x;
        tile.Position.Y = y;
        tile.Mask = mask;
        return tile;
    }
    
    public TileData CloneWithEdge(AtlasData uv, bool swizzle = false) {
        var tile = _data[0];
        tile.UV = uv.ToVector4();
        tile.Temp.X = swizzle ? 1 : 0;
        return tile;
    }

    public bool IsWalkable() {
        return !GroundProperties.NoWalk && (OccupiedObject == null || !OccupiedObject.Properties.OccupySquare);
    }

    public TileData GetTileData() {
        return _data[0];
    }

    [System.Runtime.CompilerServices.InlineArray(17)]
    private struct RenderData {
        private TileData _;

        public Span<TileData> AsSpan() => MemoryMarshal.CreateSpan(ref _, 17);
        
        public Span<TileData> AsSpan(int offset, int length) => MemoryMarshal.CreateSpan(ref _, 17).Slice(offset, length);
    }
}