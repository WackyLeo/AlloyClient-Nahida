using System;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using OpenTK.Mathematics;

namespace AlloyClient.Editor;

/// <summary>One opened map: the tile layers plus grid, selection, highlight and brush preview overlays.</summary>
public sealed class MapView : Sprite {
    private const int T = TileMapView.TileSize;

    public readonly int Id;
    public readonly MapData MapData;
    public readonly TileMapView TileMap;
    public readonly MapDragController DragController;

    public Vector2 MapOffset;
    public int ZoomLevel = 100;
    public bool GridEnabled { get; private set; }

    public MapSelectData Selection = MapSelectData.Empty;

    private readonly QuadLayer _grid = new(TextureType.Color);
    private readonly QuadBatch _highlight = new(TextureType.Color);
    private readonly QuadBatch _selectionRect = new(TextureType.Color);
    private readonly Sprite _brushOverlay = new();
    private readonly QuadLayer _brushTextures = new(TextureType.GameAtlas);
    private readonly QuadLayer _brushColors = new(TextureType.Color);
    private readonly QuadBatch _canvasOutline = new(TextureType.Color);

    // What the brush overlay currently shows, so we only rebuild it when needed
    private DrawType _brushElementType;
    private int _brushSize = -1;
    private int _brushTextureType = int.MinValue;
    private bool _brushIsOutline;

    public MapView(int id, MapData mapData) {
        Id = id;
        MapData = mapData;
        DragController = new MapDragController(this);

        TileMap = new TileMapView();
        AddChild(TileMap);

        _grid.Visible = false;
        AddChild(_grid);
        AddChild(_highlight);
        AddChild(_selectionRect);

        _brushOverlay.Alpha = 0.9f;
        _brushOverlay.Visible = false;
        _brushOverlay.AddChild(_brushTextures);
        _brushOverlay.AddChild(_brushColors);
        AddChild(_brushOverlay);

        AddChild(_canvasOutline);
    }

    /// <summary>Call after the map data was loaded/created/resized.</summary>
    public void OnMapLoaded() {
        Selection = MapSelectData.Empty;
        _selectionRect.Clear();
        _selectionRect.Commit();
        _highlight.Clear();
        _highlight.Commit();
        DragController.LastDragAction = null;

        TileMap.Setup(MapData);
        DrawGrid();
        DrawCanvasOutline();
    }

    private void DrawGrid() {
        _grid.Clear();
        var w = MapData.MapWidth * T;
        var h = MapData.MapHeight * T;
        for (var i = 0; i <= MapData.MapWidth; i++) _grid.AddColorQuad(i * T, 0, 1, h, 0x5FFF0000);
        for (var i = 0; i <= MapData.MapHeight; i++) _grid.AddColorQuad(0, i * T, w, 1, 0x5FFF0000);
        _grid.Commit();
    }

    private void DrawCanvasOutline() {
        _canvasOutline.Clear();
        _canvasOutline.AddOutline(0, 0, MapData.MapWidth * T, MapData.MapHeight * T, 1, 0x5FFFFFFF);
        _canvasOutline.Commit();
    }

    public bool ToggleGrid() {
        GridEnabled = !GridEnabled;
        _grid.Visible = GridEnabled;
        return GridEnabled;
    }

    public void SetGrid(bool value) {
        GridEnabled = value;
        _grid.Visible = value;
    }

    #region Selection

    public void RecordSelectionClear(MapHistory history) {
        history?.Record(new MapSelectAction(this, Selection.Clone(), MapSelectData.Empty));
    }

    public void ClearTileSelection() {
        Selection = MapSelectData.Empty;
        _selectionRect.Clear();
        _selectionRect.Commit();
        DragController.Reset();
    }

    public void SetLastDragAction(MapDragAction dragAction) {
        if (!Selection.IsEmpty) Selection.LastDragAction = dragAction;
        DragController.LastDragAction = dragAction;
    }

    /// <summary>Selects a rectangle of tiles, the corners can be given in any order.</summary>
    public void SelectTileArea(int startX, int startY, int endX, int endY) {
        DrawTileSelection(Math.Min(startX, endX), Math.Min(startY, endY), Math.Max(startX, endX), Math.Max(startY, endY));
    }

    public void DrawTileSelection(int startX, int startY, int endX, int endY) {
        Selection = new MapSelectData(startX, startY, endX, endY);

        _selectionRect.Clear();
        _selectionRect.AddOutline(startX * T, startY * T, (endX - startX + 1) * T, (endY - startY + 1) * T, 0.75f, 0xFFFFFFFF);
        _selectionRect.Commit();
    }

    public bool IsInsideSelection(int mapX, int mapY, bool needsSelection = false) {
        if (Selection.IsEmpty) return !needsSelection;
        return mapX >= Selection.StartX && mapX <= Selection.EndX && mapY >= Selection.StartY && mapY <= Selection.EndY;
    }

    #endregion

    #region Overlays

    public void HighlightTile(int mapX, int mapY) {
        _highlight.Clear();
        if (MapData.InBounds(mapX, mapY))
            _highlight.AddOutline(mapX * T, mapY * T, T, T, 0.75f, 0x80FFFFFF);
        _highlight.Commit();
    }

    public void HideBrushOverlay() => _brushOverlay.Visible = false;

    public void HideOverlays() {
        HighlightTile(-1, -1);
        HideBrushOverlay();
    }

    public void MoveBrushOverlay(int mapX, int mapY, MEBrush brush, bool eraser = false, bool forceDraw = false) {
        if (eraser) {
            if (forceDraw || !_brushIsOutline || brush.Size != _brushSize) {
                DrawBrushOutline(mapX, mapY, brush);
                return;
            }
        } else if (forceDraw || _brushIsOutline || brush.Size != _brushSize || brush.ElementType != _brushElementType ||
                   brush.GetElement(brush.ElementType) != _brushTextureType) {
            DrawBrushTiles(mapX, mapY, brush);
            return;
        }

        PositionBrushOverlay(mapX, mapY, brush.Size);
    }

    private void PositionBrushOverlay(int mapX, int mapY, int size) {
        _brushOverlay.X = (mapX - size) * T;
        _brushOverlay.Y = (mapY - size) * T;
        _brushOverlay.Visible = true;
    }

    /// <summary>Transparent preview of the tiles the brush will paint.</summary>
    public void DrawBrushTiles(int mapX, int mapY, MEBrush brush) {
        var element = brush.GetElement(brush.ElementType);
        if (element == MEBrush.EmptyValue(brush.ElementType)) {
            HideBrushOverlay();
            return;
        }

        var hasTexture = EditorAssets.TryGetTexture(brush.ElementType, element, out var uv);
        var color = 0x5F000000 | (brush.ElementType == DrawType.Regions ? EditorAssets.GetRegionColor(element) : 0xFF00FF);

        _brushElementType = brush.ElementType;
        _brushTextureType = element;
        _brushSize = brush.Size;
        _brushIsOutline = false;
        _brushTextures.Clear();
        _brushColors.Clear();

        var radius = brush.Size;
        void AddTile(int xi, int yi) {
            if (hasTexture) _brushTextures.AddTexturedQuad(xi * T, yi * T, T, T, uv);
            else _brushColors.AddColorQuad(xi * T, yi * T, T, T, color);
        }

        if (brush.BrushShape != 0) {
            foreach (var tile in brush.ShapeTiles) AddTile(tile.X + radius, tile.Y + radius);
        } else {
            for (var yi = 0; yi <= radius * 2; yi++) {
                for (var xi = 0; xi <= radius * 2; xi++) {
                    var dx = xi - radius;
                    var dy = yi - radius;
                    if (dx * dx + dy * dy > radius * radius) continue;
                    AddTile(xi, yi);
                }
            }
        }

        _brushTextures.Commit();
        _brushColors.Commit();
        PositionBrushOverlay(mapX, mapY, brush.Size);
    }

    /// <summary>White outline of the area the eraser affects.</summary>
    public void DrawBrushOutline(int mapX, int mapY, MEBrush brush) {
        _brushSize = brush.Size;
        _brushIsOutline = true;
        _brushTextures.Clear();
        _brushColors.Clear();

        var radius = brush.Size;
        for (var yi = 0; yi <= radius * 2; yi++) {
            for (var xi = 0; xi <= radius * 2; xi++) {
                var dx = xi - radius;
                var dy = yi - radius;
                if (dx * dx + dy * dy > radius * radius) continue;
                _brushColors.AddColorQuad(xi * T, yi * T, T, T, 0x5FFFFFFF);
            }
        }

        _brushTextures.Commit();
        _brushColors.Commit();
        PositionBrushOverlay(mapX, mapY, brush.Size);
    }

    #endregion

    public void EditTileObjCfg(int x, int y, string cfg) {
        var data = TileMap.GetTileData(x, y);
        if (data == null || data.ObjType == 0) return;
        TileMap.SetTileObjCfg(x, y, cfg);
    }

    public void CopySelectionToClipboard(MEClipboard clipboard) {
        if (Selection.IsEmpty) return;

        clipboard.SetSize(Selection.Width, Selection.Height);
        for (var y = Selection.StartY; y < Selection.StartY + Selection.Height; y++)
            for (var x = Selection.StartX; x < Selection.StartX + Selection.Width; x++)
                clipboard.AddTile(TileMap.GetTileData(x, y)?.Clone(), x - Selection.StartX, y - Selection.StartY);
    }

    public void PasteFromClipboard(MEClipboard clipboard, int mapX, int mapY, MapHistory history) {
        if (clipboard.Width <= 0 || clipboard.Height <= 0 || !MapData.InBounds(mapX, mapY) ||
            mapX + clipboard.Width > MapData.MapWidth || mapY + clipboard.Height > MapData.MapHeight)
            return;

        var actions = new MapActionSet();
        var prevSelection = Selection.Clone();

        // Make the pasted tiles the new selection
        ClearTileSelection();
        DrawTileSelection(mapX, mapY, mapX + clipboard.Width - 1, mapY + clipboard.Height - 1);
        actions.Push(new MapSelectAction(this, prevSelection, new MapSelectData(mapX, mapY, mapX + clipboard.Width - 1, mapY + clipboard.Height - 1)));

        for (var y = mapY; y < mapY + clipboard.Height; y++) {
            for (var x = mapX; x < mapX + clipboard.Width; x++) {
                var tile = clipboard.GetTile(x - mapX, y - mapY);
                var prev = TileMap.GetTileData(x, y)?.Clone();
                if (tile == null || prev == null || tile.SameAs(prev)) continue;

                TileMap.SetTileData(x, y, tile);
                actions.Push(new MapReplaceTileAction(this, x, y, prev, tile.Clone()));
            }
        }

        history?.RecordSet(actions);
    }
}
