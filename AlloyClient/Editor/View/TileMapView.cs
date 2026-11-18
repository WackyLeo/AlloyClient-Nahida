using System;
using System.Collections.Generic;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Editor;

/// <summary>
/// Renders a <see cref="MapData"/> as ground, object and region layers. The map is split into chunks so editing a tile
/// only rebuilds its chunk, and off-screen chunks can be hidden. All vertices are in map pixel space
/// (<see cref="TileSize"/> pixels per tile) so the chunks share one transform and never show seams.
/// </summary>
public sealed class TileMapView : Sprite {
    public const int TileSize = 8;
    private const int ChunkSize = 24; // worst case 3 region quads per tile stays under QuadBatch.MaxQuads

    private readonly Sprite _groundLayer = new();
    private readonly Sprite _objectLayer = new();
    private readonly Sprite _regionLayer = new();

    private MapData _mapData;
    private int _chunksX;
    private int _chunksY;
    private QuadBatch[] _groundChunks = [];
    private QuadBatch[] _objectChunks = [];
    private QuadBatch[] _regionChunks = [];
    private bool[] _dirty = [];
    private readonly HashSet<int> _dirtyList = [];

    public event Action MapChanged;

    public TileMapView() {
        AddChild(_groundLayer);
        AddChild(_objectLayer);
        AddChild(_regionLayer);
    }

    public MapData MapData => _mapData;

    /// <summary>(Re)creates all chunks for the map's current dimensions.</summary>
    public void Setup(MapData mapData) {
        _mapData = mapData;

        foreach (var c in _groundChunks) _groundLayer.RemoveChild(c);
        foreach (var c in _objectChunks) _objectLayer.RemoveChild(c);
        foreach (var c in _regionChunks) _regionLayer.RemoveChild(c);

        _chunksX = (mapData.MapWidth + ChunkSize - 1) / ChunkSize;
        _chunksY = (mapData.MapHeight + ChunkSize - 1) / ChunkSize;
        var count = _chunksX * _chunksY;
        _groundChunks = new QuadBatch[count];
        _objectChunks = new QuadBatch[count];
        _regionChunks = new QuadBatch[count];
        _dirty = new bool[count];
        _dirtyList.Clear();

        for (var i = 0; i < count; i++) {
            _groundChunks[i] = _groundLayer.AddChild(new QuadBatch(TextureType.GameAtlas));
            _objectChunks[i] = _objectLayer.AddChild(new QuadBatch(TextureType.GameAtlas));
            _regionChunks[i] = _regionLayer.AddChild(new QuadBatch(TextureType.Color));
            MarkChunkDirty(i);
        }

        RebuildDirty();
    }

    #region Tile access

    public MapTileData GetTileData(int mapX, int mapY) => _mapData?.GetTile(mapX, mapY);

    /// <summary>Copies the given data into the tile (null clears it).</summary>
    public void SetTileData(int mapX, int mapY, MapTileData data) {
        var tile = GetTileData(mapX, mapY);
        if (tile == null) return;
        tile.Copy(data);
        OnTileChanged(mapX, mapY);
    }

    public void SetTileGround(int mapX, int mapY, int groundType) {
        var tile = GetTileData(mapX, mapY);
        if (tile == null) return;
        tile.GroundType = groundType;
        OnTileChanged(mapX, mapY);
    }

    public void SetTileObject(int mapX, int mapY, int objType) {
        var tile = GetTileData(mapX, mapY);
        if (tile == null) return;
        tile.ObjType = objType;
        OnTileChanged(mapX, mapY);
    }

    public void SetTileRegion(int mapX, int mapY, int regType) {
        var tile = GetTileData(mapX, mapY);
        if (tile == null) return;
        tile.RegType = regType;
        OnTileChanged(mapX, mapY);
    }

    public void SetTileObjCfg(int mapX, int mapY, string cfg) {
        var tile = GetTileData(mapX, mapY);
        if (tile == null) return;
        tile.ObjCfg = cfg;
        _mapData.MarkChanged();
        MapChanged?.Invoke();
    }

    public void ClearGround(int mapX, int mapY) => SetTileGround(mapX, mapY, -1);

    public void ClearObject(int mapX, int mapY) => SetTileObject(mapX, mapY, 0);

    public void ClearRegion(int mapX, int mapY) => SetTileRegion(mapX, mapY, 0);

    public void ClearTile(int mapX, int mapY) {
        var tile = GetTileData(mapX, mapY);
        if (tile == null) return;
        tile.GroundType = -1;
        tile.ObjType = 0;
        tile.RegType = 0;
        OnTileChanged(mapX, mapY);
    }

    private void OnTileChanged(int mapX, int mapY) {
        MarkChunkDirty(mapX / ChunkSize + mapY / ChunkSize * _chunksX);
        _mapData.MarkChanged();
        MapChanged?.Invoke();
    }

    #endregion

    #region Rendering

    private void MarkChunkDirty(int idx) {
        if (idx < 0 || idx >= _dirty.Length || _dirty[idx]) return;
        _dirty[idx] = true;
        _dirtyList.Add(idx);
    }

    /// <summary>Rebuilds the vertex data of every chunk touched since the last call. Called once per frame.</summary>
    public void RebuildDirty() {
        if (_dirtyList.Count == 0) return;
        foreach (var idx in _dirtyList) {
            RebuildChunk(idx);
            _dirty[idx] = false;
        }

        _dirtyList.Clear();
    }

    private void RebuildChunk(int idx) {
        var ground = _groundChunks[idx];
        var objects = _objectChunks[idx];
        var regions = _regionChunks[idx];
        ground.Clear();
        objects.Clear();
        regions.Clear();

        var startX = idx % _chunksX * ChunkSize;
        var startY = idx / _chunksX * ChunkSize;
        var endX = Math.Min(startX + ChunkSize, _mapData.MapWidth);
        var endY = Math.Min(startY + ChunkSize, _mapData.MapHeight);

        for (var y = startY; y < endY; y++) {
            for (var x = startX; x < endX; x++) {
                var tile = _mapData.Tiles[x + y * _mapData.MapWidth];
                float px = x * TileSize, py = y * TileSize;

                if (tile.GroundType != -1) {
                    if (EditorAssets.TryGetGroundTexture(tile.GroundType, out var uv))
                        ground.AddTexturedQuad(px, py, TileSize, TileSize, uv);
                    else
                        regions.AddColorQuad(px, py, TileSize, TileSize, 0x80FF00FF); // unknown ground
                }

                if (tile.ObjType != 0) {
                    if (EditorAssets.TryGetObjectTexture(tile.ObjType, out var uv))
                        objects.AddTexturedQuad(px, py, TileSize, TileSize, uv);
                    else
                        regions.AddColorQuad(px + 2, py + 2, TileSize - 4, TileSize - 4, 0xC0FF00FF); // object without texture
                }

                if (tile.RegType != 0)
                    regions.AddColorQuad(px, py, TileSize, TileSize, 0x5F000000 | EditorAssets.GetRegionColor(tile.RegType));
            }
        }

        ground.Commit();
        objects.Commit();
        regions.Commit();
    }

    /// <summary>Hides chunks outside of the visible map pixel rectangle.</summary>
    public void UpdateVisibility(float viewX, float viewY, float viewW, float viewH) {
        const int chunkPx = ChunkSize * TileSize;
        for (var cy = 0; cy < _chunksY; cy++) {
            for (var cx = 0; cx < _chunksX; cx++) {
                var x = cx * chunkPx;
                var y = cy * chunkPx;
                var visible = x + chunkPx >= viewX && x <= viewX + viewW && y + chunkPx >= viewY && y <= viewY + viewH;
                var idx = cx + cy * _chunksX;
                _groundChunks[idx].Visible = visible;
                _objectChunks[idx].Visible = visible;
                _regionChunks[idx].Visible = visible;
            }
        }
    }

    #endregion
}
