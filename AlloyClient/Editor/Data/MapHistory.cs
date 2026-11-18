using System;
using System.Collections.Generic;

namespace AlloyClient.Editor;

public abstract class MapAction {
    public bool Swapped; // Makes the action redo when undoAction is called and viceversa

    public abstract void DoAction();
    public abstract void UndoAction();
    public abstract MapAction Clone();
}

public sealed class MapActionSet {
    private readonly List<MapAction> _reversed = []; // Used for undoing
    private readonly List<MapAction> _normal = [];   // Used for redoing

    public bool Empty => _normal.Count == 0;

    public void Push(MapAction action) {
        if (action == null) return;
        _reversed.Insert(0, action); // Start undoing from last action to first
        _normal.Add(action);         // Start redoing from first to last
    }

    public void UndoAll() {
        foreach (var action in _reversed) {
            if (action.Swapped) action.DoAction();
            else action.UndoAction();
        }
    }

    public void RedoAll() {
        foreach (var action in _normal) {
            if (action.Swapped) action.UndoAction();
            else action.DoAction();
        }
    }
}

public sealed class MapHistory {
    // We can change the present, revert actions from it and erase them (they are now the past),
    // or make the erased actions come back (they are now the present, again)
    private readonly List<MapActionSet> _present = [];
    private readonly List<MapActionSet> _erased = [];

    public void Record(MapAction action) {
        if (action == null) return;
        var set = new MapActionSet();
        set.Push(action);
        _present.Add(set);
        _erased.Clear(); // Erased events are now forgotten, forever
    }

    public void RecordSet(MapActionSet actions) {
        if (actions == null || actions.Empty) return;
        _present.Add(actions);
        _erased.Clear();
    }

    public bool Undo() {
        if (_present.Count == 0) return false;
        var last = _present[^1];
        _present.RemoveAt(_present.Count - 1);
        last.UndoAll();
        _erased.Add(last);
        return true;
    }

    public bool Redo() {
        if (_erased.Count == 0) return false;
        var last = _erased[^1];
        _erased.RemoveAt(_erased.Count - 1);
        last.RedoAll();
        _present.Add(last);
        return true;
    }
}

/// <summary>Holds one history per opened map (key: map id).</summary>
public sealed class TimeControl {
    private readonly Dictionary<int, MapHistory> _history = new();

    public void CreateHistory(int mapId) => _history[mapId] = new MapHistory();

    public MapHistory GetHistory(int mapId) => _history.GetValueOrDefault(mapId);

    public void EraseHistory(int mapId) => _history.Remove(mapId);

    public bool UndoLastAction(int mapId) => GetHistory(mapId)?.Undo() ?? false;

    public bool RedoLastUndoneAction(int mapId) => GetHistory(mapId)?.Redo() ?? false;
}

#region Actions

public sealed class MapReplaceTileAction(MapView view, int mapX, int mapY, MapTileData prevTile, MapTileData newTile) : MapAction {
    public override void DoAction() {
        view.TileMap.SetTileData(mapX, mapY, newTile);
    }

    public override void UndoAction() {
        view.TileMap.SetTileData(mapX, mapY, prevTile);
    }

    public override MapAction Clone() => new MapReplaceTileAction(view, mapX, mapY, prevTile, newTile);
}

public sealed class MapEditAction(MapView view, int mapX, int mapY, string prevCfg, string newCfg) : MapAction {
    public override void DoAction() => view.EditTileObjCfg(mapX, mapY, newCfg);

    public override void UndoAction() => view.EditTileObjCfg(mapX, mapY, prevCfg);

    public override MapAction Clone() => new MapEditAction(view, mapX, mapY, prevCfg, newCfg);
}

public sealed class MapSelectAction(MapView view, MapSelectData prevData, MapSelectData newData) : MapAction {
    public override void DoAction() => Apply(newData);

    public override void UndoAction() => Apply(prevData);

    private void Apply(MapSelectData data) {
        if (data.IsEmpty) {
            view.ClearTileSelection();
            return;
        }

        view.SetLastDragAction(data.LastDragAction);
        view.DrawTileSelection(data.StartX, data.StartY, data.EndX, data.EndY);
    }

    public override MapAction Clone() => new MapSelectAction(view, prevData, newData);
}

public sealed class MapDragAction : MapAction {
    private readonly MapDragController _controller;
    private readonly MapDragAction _prevAction;
    public readonly MapTileData[] OldTiles; // Tiles before dragging
    public readonly MapTileData[] NewTiles; // Tiles to draw on new area
    public MapTileData[] UserNewTiles;      // Tiles the user drew on
    private readonly MapSelectData _prevSelection;
    private readonly MapSelectData _newSelection;

    public MapDragAction(MapDragController controller, MapDragAction prevAction, MapTileData[] oldTiles, MapTileData[] newTiles,
        MapSelectData oldSelection, MapSelectData newSelection, MapTileData[] userNewTiles = null) {
        _controller = controller;
        _prevAction = prevAction;
        OldTiles = oldTiles;
        NewTiles = newTiles;
        UserNewTiles = userNewTiles;
        _prevSelection = oldSelection;
        _newSelection = newSelection;
    }

    public override void DoAction() => DragNewTiles(false);

    public override void UndoAction() {
        _controller.PasteTiles(OldTiles, _newSelection.StartX, _newSelection.StartY, _newSelection.EndX, _newSelection.EndY);

        if (_prevAction != null) {
            _prevAction.DragNewTiles(true);
        } else { // Back where we started
            _controller.PasteTiles(NewTiles, _prevSelection.StartX, _prevSelection.StartY, _prevSelection.EndX, _prevSelection.EndY);
            _controller.View.SelectTileArea(_prevSelection.StartX, _prevSelection.StartY, _prevSelection.EndX, _prevSelection.EndY);
            _controller.View.SetLastDragAction(null);
        }
    }

    private void DragNewTiles(bool userTiles) {
        var tiles = userTiles && UserNewTiles != null ? UserNewTiles : NewTiles;
        if (_prevAction == null)
            _controller.ClearTileArea(_prevSelection.StartX, _prevSelection.StartY, _prevSelection.EndX, _prevSelection.EndY);
        else
            _controller.PasteTiles(_prevAction.OldTiles, _prevSelection.StartX, _prevSelection.StartY, _prevSelection.EndX, _prevSelection.EndY);

        _controller.PasteTiles(tiles, _newSelection.StartX, _newSelection.StartY, _newSelection.EndX, _newSelection.EndY);
        _controller.View.SelectTileArea(_newSelection.StartX, _newSelection.StartY, _newSelection.EndX, _newSelection.EndY);
        _controller.View.SetLastDragAction(this);
    }

    public override MapAction Clone() =>
        new MapDragAction(_controller, _prevAction, OldTiles, NewTiles, _prevSelection, _newSelection, UserNewTiles);
}

#endregion

public sealed class MapDragController(MapView view) {
    public readonly MapView View = view;
    public MapDragAction LastDragAction;

    public void Reset() => View.SetLastDragAction(null);

    // Only called when the user drags tiles, not when a drag action is undone/redone
    public MapDragAction DragSelection(int startX, int startY, int endX, int endY) {
        var prevSelection = View.Selection.Clone();

        var firstMove = LastDragAction == null;
        var newTiles = GetSelectedTiles();
        if (firstMove)
            ClearTileArea(prevSelection.StartX, prevSelection.StartY, prevSelection.EndX, prevSelection.EndY);
        else // Undo previously moved tiles
            PasteTiles(LastDragAction.OldTiles, prevSelection.StartX, prevSelection.StartY, prevSelection.EndX, prevSelection.EndY);

        View.SelectTileArea(startX, startY, endX, endY);

        var oldTiles = GetSelectedTiles(); // Original tiles of the new area
        PasteTiles(newTiles, startX, startY, endX, endY);

        MapDragAction prev = null;
        if (LastDragAction != null) {
            LastDragAction.UserNewTiles = newTiles; // Updated tile set in case the user drew on the selection before moving again
            prev = LastDragAction;
        }

        var action = new MapDragAction(this, prev, oldTiles, newTiles, prevSelection, View.Selection.Clone());
        View.SetLastDragAction(action);
        return action;
    }

    public void PasteTiles(MapTileData[] tiles, int startX, int startY, int endX, int endY) {
        var idx = 0;
        for (var y = startY; y <= endY; y++) {
            for (var x = startX; x <= endX; x++) {
                if (idx < tiles.Length) View.TileMap.SetTileData(x, y, tiles[idx].Clone());
                idx++;
            }
        }
    }

    private MapTileData[] GetSelectedTiles() {
        var sel = View.Selection;
        var ret = new MapTileData[Math.Max(0, sel.Width * sel.Height)];
        var idx = 0;
        for (var y = sel.StartY; y <= sel.EndY; y++)
            for (var x = sel.StartX; x <= sel.EndX; x++)
                ret[idx++] = View.TileMap.GetTileData(x, y)?.Clone() ?? new MapTileData();
        return ret;
    }

    public void ClearTileArea(int startX, int startY, int endX, int endY) {
        for (var y = startY; y <= endY; y++)
            for (var x = startX; x <= endX; x++)
                View.TileMap.ClearTile(x, y);
    }
}
