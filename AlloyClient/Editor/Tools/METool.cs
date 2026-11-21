using System;
using System.Collections.Generic;
using AlloyClient.UiLib.Input;
using OpenTK.Mathematics;

namespace AlloyClient.Editor;

public abstract class METool
{
    public const int SelectId = 0;
    public const int DrawId = 1;
    public const int LineId = 2;
    public const int ShapeId = 3;
    public const int FillId = 4;
    public const int SampleId = 5;
    public const int EraserId = 6;
    public const int EditId = 7;

    public readonly int Id;
    protected readonly MapEditorScreen Editor;

    protected METool(int id, MapEditorScreen editor)
    {
        Id = id;
        Editor = editor;
    }

    protected MapView View => Editor.MapView;
    protected MEBrush Brush => Editor.UserBrush;

    public virtual void Init(Vector2i? tilePos, MapHistory history)
    {
    }

    public virtual void Reset()
    {
    }

    public virtual void MouseDrag(Vector2i? tilePos, MapHistory history)
    {
    }

    public virtual void MouseDragEnd(Vector2i? tilePos, MapHistory history)
    {
    }

    public virtual void TileClick(Vector2i? tilePos, MapHistory history)
    {
    }

    public virtual void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
    }

    public virtual void BrushChanged(Vector2i? tilePos, MapHistory history)
    {
    }

    /// <summary>Paints the brush's element on a tile, returns the undo action or null if nothing changed.</summary>
    protected MapReplaceTileAction PaintTile(int mapX, int mapY)
    {
        var tileMap = View.TileMap;
        var prev = tileMap.GetTileData(mapX, mapY)?.Clone();
        if (prev == null) return null;

        switch (Brush.ElementType)
        {
            case DrawType.Ground:
                if (Brush.GroundType == -1 || prev.GroundType == Brush.GroundType) return null;
                if (!Brush.Replace && prev.GroundType != -1) return null;
                tileMap.SetTileGround(mapX, mapY, Brush.GroundType);
                break;
            case DrawType.Objects:
                if (Brush.ObjType == 0 || prev.ObjType == Brush.ObjType) return null;
                if (!Brush.Replace && prev.ObjType != 0) return null;
                tileMap.SetTileObject(mapX, mapY, Brush.ObjType);
                break;
            case DrawType.Regions:
                if (Brush.RegType == 0 || prev.RegType == Brush.RegType) return null;
                if (!Brush.Replace && prev.RegType != 0) return null;
                tileMap.SetTileRegion(mapX, mapY, Brush.RegType);
                break;
        }

        return new MapReplaceTileAction(View, mapX, mapY, prev, tileMap.GetTileData(mapX, mapY).Clone());
    }

    /// <summary>Calls <paramref name="action"/> for every tile of a circular brush of the given size around a center.</summary>
    protected static void ForEachInBrush(int centerX, int centerY, int size, Action<int, int> action)
    {
        for (var y = centerY - size; y <= centerY + size; y++)
        {
            for (var x = centerX - size; x <= centerX + size; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                if (dx * dx + dy * dy > size * size) continue;
                action(x, y);
            }
        }
    }

#region Registry

    private static readonly Dictionary<int, METool> Tools = new();

    public static void ClearCache() => Tools.Clear();

    public static METool GetTool(int toolId, MapEditorScreen editor)
    {
        if (!Tools.TryGetValue(toolId, out var tool) || tool.Editor != editor)
        {
            tool = CreateTool(toolId, editor);
            Tools[toolId] = tool;
        }

        return tool;
    }

    private static METool CreateTool(int toolId, MapEditorScreen editor) => toolId switch
    {
        SelectId => new MESelectTool(editor),
        DrawId => new MEPencilTool(editor),
        LineId => new MELineTool(editor),
        ShapeId => new MEShapeTool(editor),
        FillId => new MEBucketTool(editor),
        EraserId => new MEEraserTool(editor),
        SampleId => new MEPickerTool(editor),
        EditId => new MEEditTool(editor),
        _ => new MESelectTool(editor)
    };

    public static string ToolIdToName(int id) => id switch
    {
        SelectId => "Select",
        DrawId => "Draw",
        LineId => "Line",
        ShapeId => "Shape",
        FillId => "Fill",
        SampleId => "Sample",
        EraserId => "Eraser",
        EditId => "Edit",
        _ => "?"
    };

    public static string GetToolDescription(int id) => id switch
    {
        SelectId =>
            "Keybind: M\nHold Shift while dragging to use (not with the Draw tool).\nEscape to unselect.\nDelete to delete tiles in selection.\nArrow keys move the selection.",
        DrawId =>
            "Keybind: D\nCtrl + Scroll to change brush size.\nHold Shift and move the mouse to make a rectangle, click to fill it.",
        EraserId => "Keybind: E\nCtrl + Scroll to change brush size.",
        SampleId => "Keybind: A\nClick a tile to pick its ground/object/region.",
        FillId => "Keybind: F\nFills connected tiles.\nCtrl + Click to fill every matching tile. \nShift + Click to erase connected tiles.",
        LineId => "Keybind: L\nNot implemented.",
        ShapeId =>
            "Keybind: S\nDraw random shapes with your brush.\nHold Shift and move the mouse left/right to change the density.",
        EditId => "Keybind: I\nClick an object to edit its name/cfg (useful for signs).",
        _ => ""
    };

#endregion
}

public sealed class MESelectTool(MapEditorScreen editor) : METool(SelectId, editor)
{
    private Vector2i? _selectionStart;
    private MapSelectData _prevSelection;
    private bool _draggingSelection;
    private Vector2i? _lastDragPos;

    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void Reset()
    {
        _selectionStart = null;
        _draggingSelection = false;
        _prevSelection = null;
        _lastDragPos = null;
    }

    public override void MouseDrag(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } pos)
        {
            Reset();
            return;
        }

        if (_selectionStart == null)
        {
            if (_draggingSelection || View.IsInsideSelection(pos.X, pos.Y, true))
            {
                // Drag selected tiles
                _draggingSelection = true;
                DragSelectionTo(pos.X, pos.Y, history);
                return;
            }

            // Select new tiles
            _draggingSelection = false;
            _selectionStart = pos;
            _prevSelection = View.Selection.Clone();
            View.ClearTileSelection();
        }

        var start = _selectionStart.Value;
        if (start != pos) View.SelectTileArea(start.X, start.Y, pos.X, pos.Y);
    }

    public override void MouseDragEnd(Vector2i? tilePos, MapHistory history)
    {
        if (_selectionStart == null || tilePos is not { } pos)
        {
            Reset();
            return;
        }

        var start = _selectionStart.Value;
        var beginX = Math.Min(start.X, pos.X);
        var beginY = Math.Min(start.Y, pos.Y);
        var endX = Math.Max(start.X, pos.X);
        var endY = Math.Max(start.Y, pos.Y);
        if (endX == beginX && endY == beginY)
        {
            SingleSelection(pos, history, (_prevSelection ?? MapSelectData.Empty).Clone());
            return;
        }

        history?.Record(new MapSelectAction(View, (_prevSelection ?? MapSelectData.Empty).Clone(),
            View.Selection.Clone()));
        Reset();
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } pos)
        {
            Reset();
            return;
        }

        if (View.IsInsideSelection(pos.X, pos.Y, true)) return; // Clicking once inside the selection does nothing
        SingleSelection(pos, history, View.Selection.Clone());
    }

    private void SingleSelection(Vector2i pos, MapHistory history, MapSelectData prevSelection)
    {
        if (prevSelection.IsEmpty)
        {
            // Empty selection, select 1 tile
            View.SelectTileArea(pos.X, pos.Y, pos.X, pos.Y);
            history?.Record(new MapSelectAction(View, prevSelection, View.Selection.Clone()));
        }
        else
        {
            // Clear selection
            View.RecordSelectionClear(history);
            View.ClearTileSelection();
        }

        Reset();
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } pos)
        {
            Reset();
            return;
        }

        View.HighlightTile(pos.X, pos.Y);
    }

    private void DragSelectionTo(int mapX, int mapY, MapHistory history)
    {
        _lastDragPos ??= new Vector2i(mapX, mapY);
        DragSelection(mapX - _lastDragPos.Value.X, mapY - _lastDragPos.Value.Y, history);
        _lastDragPos = new Vector2i(mapX, mapY);
    }

    public void DragSelection(int diffX, int diffY, MapHistory history)
    {
        var selection = View.Selection;
        if (selection.IsEmpty) return;

        var beginX = selection.StartX + diffX;
        var beginY = selection.StartY + diffY;
        var endX = beginX + selection.Width - 1;
        var endY = beginY + selection.Height - 1;

        if ((diffX == 0 && diffY == 0) || beginX < 0 || endX >= View.MapData.MapWidth || beginY < 0 ||
            endY >= View.MapData.MapHeight)
            return;

        history?.Record(View.DragController.DragSelection(beginX, beginY, endX, endY));
    }
}

public sealed class MEPencilTool(MapEditorScreen editor) : METool(DrawId, editor)
{
    // Holding shift anchors a rectangle at the tile under the mouse, clicking fills it (like the flash editor)
    private Vector2i? _rectAnchor;

    // Everything painted during one mouse drag, recorded as a single undo step when the drag ends
    private MapActionSet _stroke;
    private MapHistory _strokeHistory;

    public bool RectActive => _rectAnchor != null;

    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        CommitStroke();
        _rectAnchor = null;
        if (tilePos is { } p) View.MoveBrushOverlay(p.X, p.Y, Brush, false, true);
    }

    public override void Reset()
    {
        CommitStroke(); // A stroke interrupted by a tool switch stays undoable
        _rectAnchor = null;
        View?.HideRectPreview();
    }

    public override void MouseDrag(Vector2i? tilePos, MapHistory history)
    {
        if (RectActive)
        {
            return; // Dragging with shift only resizes the rectangle, it's filled on release
        }

        if (tilePos is { } p) UsePencil(p, history, true);
    }

    public override void MouseDragEnd(Vector2i? tilePos, MapHistory history)
    {
        CommitStroke(); // Always, even if the mouse ended outside the map

        if (RectActive && tilePos is { } p)
        {
            FillRect(p, history);
        }
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p)
        {
            return;
        }

        if (RectActive)
        {
            FillRect(p, history);
        }
        else
        {
            UsePencil(p, history, false);
        }
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p)
        {
            return;
        }

        if (KeyboardInput.IsShiftDown())
        {
            _rectAnchor ??= p;
            View.HideBrushOverlay();
            View.DrawRectPreview(_rectAnchor.Value.X, _rectAnchor.Value.Y, p.X, p.Y);
            return;
        }

        if (RectActive)
        {
            Reset();
            View.MoveBrushOverlay(p.X, p.Y, Brush, false, true);
            return;
        }

        View.MoveBrushOverlay(p.X, p.Y, Brush);
    }

    private void FillRect(Vector2i pos, MapHistory history)
    {
        var anchor = _rectAnchor!.Value;
        var actions = new MapActionSet();
        for (var y = Math.Min(anchor.Y, pos.Y); y <= Math.Max(anchor.Y, pos.Y); y++)
        {
            for (var x = Math.Min(anchor.X, pos.X); x <= Math.Max(anchor.X, pos.X); x++)
            {
                if (View.IsInsideSelection(x, y))
                    actions.Push(PaintTile(x, y));
            }
        }

        history?.RecordSet(actions);
    }

    /// <param name="stroke">True while dragging: stamps are collected into one undo step. False for a single click.</param>
    private void UsePencil(Vector2i pos, MapHistory history, bool stroke)
    {
        if (!View.IsInsideSelection(pos.X, pos.Y)) return;

        MapActionSet actions;
        if (stroke)
        {
            if (_stroke != null && _strokeHistory != history) CommitStroke(); // Switched maps mid-stroke
            actions = _stroke ??= new MapActionSet();
            _strokeHistory = history;
        }
        else
        {
            actions = new MapActionSet();
        }

        if (Brush.Size == 0)
        {
            actions.Push(PaintTile(pos.X, pos.Y));
        }
        else
        {
            ForEachInBrush(pos.X, pos.Y, Brush.Size, (x, y) =>
            {
                if (View.IsInsideSelection(x, y)) actions.Push(PaintTile(x, y));
            });
        }

        if (!stroke) history?.RecordSet(actions); // A single click is its own undo step
    }

    private void CommitStroke()
    {
        var set = _stroke;
        var history = _strokeHistory;
        _stroke = null;
        _strokeHistory = null;
        if (set != null) history?.RecordSet(set); // RecordSet ignores empty sets
    }
}

public sealed class MEEraserTool(MapEditorScreen editor) : METool(EraserId, editor)
{
    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.MoveBrushOverlay(p.X, p.Y, Brush, true, true);
    }

    public override void MouseDrag(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) UseEraser(p, history);
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) UseEraser(p, history);
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.MoveBrushOverlay(p.X, p.Y, Brush, true);
    }

    private void UseEraser(Vector2i pos, MapHistory history)
    {
        if (!View.IsInsideSelection(pos.X, pos.Y)) return;

        if (Brush.Size == 0)
        {
            history?.Record(EraseTile(View, Brush, pos.X, pos.Y));
            return;
        }

        var actions = new MapActionSet();
        ForEachInBrush(pos.X, pos.Y, Brush.Size, (x, y) =>
        {
            if (View.IsInsideSelection(x, y)) actions.Push(EraseTile(View, Brush, x, y));
        });
        history?.RecordSet(actions);
    }

    public static void EraseSelection(MapView view, MEBrush brush, MapSelectData selection, MapHistory history)
    {
        var actions = new MapActionSet();
        for (var y = selection.StartY; y <= selection.EndY; y++)
            for (var x = selection.StartX; x <= selection.EndX; x++)
                if (view.IsInsideSelection(x, y))
                    actions.Push(EraseTile(view, brush, x, y));
        history?.RecordSet(actions);
    }

    public static MapReplaceTileAction EraseTile(MapView view, MEBrush brush, int mapX, int mapY)
    {
        var tileMap = view.TileMap;
        var prev = tileMap.GetTileData(mapX, mapY)?.Clone();
        if (prev == null) return null;

        switch (brush.ElementType)
        {
            case DrawType.Ground:
                if (prev.GroundType == -1) return null;
                tileMap.ClearGround(mapX, mapY);
                break;
            case DrawType.Objects:
                if (prev.ObjType == 0) return null;
                tileMap.ClearObject(mapX, mapY);
                break;
            case DrawType.Regions:
                if (prev.RegType == 0) return null;
                tileMap.ClearRegion(mapX, mapY);
                break;
        }

        return new MapReplaceTileAction(view, mapX, mapY, prev, tileMap.GetTileData(mapX, mapY).Clone());
    }
}

public sealed class MEBucketTool(MapEditorScreen editor) : METool(FillId, editor)
{
    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p && View.IsInsideSelection(p.X, p.Y)) DoFill(p, history); // Restricted to the selected area
    }

    public override void MouseDragEnd(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p && View.IsInsideSelection(p.X, p.Y)) DoFill(p, history);
    }

    private void DoFill(Vector2i start, MapHistory history)
    {
        var erase = KeyboardInput.IsShiftDown(); // Shift: erase instead of fill
        var actions = new MapActionSet();
        var origTile = View.TileMap.GetTileData(start.X, start.Y)?.Clone();
        if (origTile == null) return;

        if (KeyboardInput.IsCtrlDown())
        {
            // Ctrl: every matching tile
            for (var y = 0; y < View.MapData.MapHeight; y++)
                for (var x = 0; x < View.MapData.MapWidth; x++)
                    if (View.IsInsideSelection(x, y))
                        FillTile(x, y, origTile, actions, erase);
        }
        else
        {
            // Flood fill (iterative, recursion overflows the stack)
            var pending = new Stack<Vector2i>();
            pending.Push(start);
            while (pending.Count != 0)
            {
                var pos = pending.Pop();
                if (!View.IsInsideSelection(pos.X, pos.Y)) continue;
                if (!FillTile(pos.X, pos.Y, origTile, actions, erase)) continue;

                pending.Push(new Vector2i(pos.X + 1, pos.Y));
                pending.Push(new Vector2i(pos.X - 1, pos.Y));
                pending.Push(new Vector2i(pos.X, pos.Y + 1));
                pending.Push(new Vector2i(pos.X, pos.Y - 1));
            }
        }

        history?.RecordSet(actions);
    }

    private bool FillTile(int mapX, int mapY, MapTileData origTile, MapActionSet actions, bool erase)
    {
        var tileMap = View.TileMap;
        var prev = tileMap.GetTileData(mapX, mapY)?.Clone();
        if (prev == null) return false;

        switch (Brush.ElementType)
        {
            case DrawType.Ground:
                if (prev.GroundType != origTile.GroundType) return false;
                if (erase)
                {
                    if (prev.GroundType == -1) return false; // Nothing to erase
                    tileMap.ClearGround(mapX, mapY);
                }
                else
                {
                    if (prev.GroundType == Brush.GroundType) return false;
                    tileMap.SetTileGround(mapX, mapY, Brush.GroundType);
                }

                break;
            case DrawType.Objects:
                if (prev.ObjType != origTile.ObjType) return false;
                if (erase)
                {
                    if (prev.ObjType == 0) return false;
                    tileMap.ClearObject(mapX, mapY);
                }
                else
                {
                    if (prev.ObjType == Brush.ObjType) return false;
                    tileMap.SetTileObject(mapX, mapY, Brush.ObjType);
                }

                break;
            case DrawType.Regions:
                if (prev.RegType != origTile.RegType) return false;
                if (erase)
                {
                    if (prev.RegType == 0) return false;
                    tileMap.ClearRegion(mapX, mapY);
                }
                else
                {
                    if (prev.RegType == Brush.RegType) return false;
                    tileMap.SetTileRegion(mapX, mapY, Brush.RegType);
                }

                break;
        }

        actions.Push(new MapReplaceTileAction(View, mapX, mapY, prev, tileMap.GetTileData(mapX, mapY).Clone()));
        return true;
    }
}

public sealed class MEPickerTool(MapEditorScreen editor) : METool(SampleId, editor)
{
    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p) return;
        var tile = View.TileMap.GetTileData(p.X, p.Y);
        if (tile == null) return;

        if (tile.GroundType != -1) Brush.GroundType = tile.GroundType;
        if (tile.ObjType != 0) Brush.ObjType = tile.ObjType;
        if (tile.RegType != 0) Brush.RegType = tile.RegType;
        Editor.UpdateDrawElements();
    }
}

public sealed class MEEditTool(MapEditorScreen editor) : METool(EditId, editor)
{
    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p) return;
        var tile = View.TileMap.GetTileData(p.X, p.Y);
        if (tile == null || tile.ObjType == 0) return;
        Editor.ShowEditNameView(p.X, p.Y, tile.ObjCfg);
    }
}

/// <summary>Not implemented in the original editor either, only highlights the hovered tile.</summary>
public sealed class MELineTool(MapEditorScreen editor) : METool(LineId, editor)
{
    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is { } p) View.HighlightTile(p.X, p.Y);
    }
}

public sealed class MEShapeTool(MapEditorScreen editor) : METool(ShapeId, editor)
{
    private readonly List<Vector2i> _tiles = [];
    private static readonly Random Rng = new();

    // Everything stamped during one drag, recorded as a single undo step when the drag ends
    private MapActionSet _stroke;
    private MapHistory _strokeHistory;
    private Vector2i? _lastStamp; // Only stamp when the cursor enters a new tile

    public override void Init(Vector2i? tilePos, MapHistory history)
    {
        CommitStroke();
        MakeRandomTiles();
        if (tilePos is { } p) View.MoveBrushOverlay(p.X, p.Y, Brush, false, true);
    }

    public override void Reset() => CommitStroke();

    public override void MouseMoved(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p) return;
        var regenerated = false;
        if (_tiles.Count == 0)
        {
            MakeRandomTiles();
            regenerated = true;
        }

        View.MoveBrushOverlay(p.X, p.Y, Brush, false, regenerated);
    }

    public override void MouseDrag(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p || _lastStamp == p) return;

        if (_stroke != null && _strokeHistory != history) CommitStroke(); // Switched maps mid-stroke
        _stroke ??= new MapActionSet();
        _strokeHistory = history;
        _lastStamp = p;

        PlaceTiles(p, _stroke);
        MakeRandomTiles(); // Fresh shape for the next stamp
        View.MoveBrushOverlay(p.X, p.Y, Brush, false, true);
    }

    public override void MouseDragEnd(Vector2i? tilePos, MapHistory history) => CommitStroke();

    public override void BrushChanged(Vector2i? tilePos, MapHistory history)
    {
        MakeRandomTiles();
        if (tilePos is { } p) View.MoveBrushOverlay(p.X, p.Y, Brush, false, true);
    }

    public override void TileClick(Vector2i? tilePos, MapHistory history)
    {
        if (tilePos is not { } p) return;
        var actions = new MapActionSet();
        PlaceTiles(p, actions);
        history?.RecordSet(actions); // A single click is its own undo step
        MakeRandomTiles();
        View.MoveBrushOverlay(p.X, p.Y, Brush, false, true);
    }

    private void PlaceTiles(Vector2i pos, MapActionSet actions)
    {
        if (!View.IsInsideSelection(pos.X, pos.Y)) return;

        if (Brush.Size == 0)
        {
            actions.Push(PaintTile(pos.X, pos.Y));
            return;
        }

        foreach (var t in _tiles)
        {
            var x = t.X + pos.X;
            var y = t.Y + pos.Y;
            if (View.IsInsideSelection(x, y)) actions.Push(PaintTile(x, y));
        }
    }

    private void CommitStroke()
    {
        var set = _stroke;
        var history = _strokeHistory;
        _stroke = null;
        _strokeHistory = null;
        _lastStamp = null;
        if (set != null) history?.RecordSet(set); // RecordSet ignores empty sets
    }

    private void MakeRandomTiles()
    {
        _tiles.Clear();
        var size = Brush.Size;

        var candidates = new List<Vector2i>();
        for (var y = -size; y <= size; y++)
            for (var x = -size; x <= size; x++)
                if (x * x + y * y <= size * size)
                    candidates.Add(new Vector2i(x, y));

        // Pick chance% of the brush area at random (without repeats)
        var total = Math.Clamp((int)(Brush.Chance / 100.0 * candidates.Count), size == 0 ? 1 : 0, candidates.Count);
        for (var i = 0; i < total; i++)
        {
            var j = Rng.Next(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            _tiles.Add(candidates[i]);
        }

        Brush.ShapeTiles = _tiles;
    }
}