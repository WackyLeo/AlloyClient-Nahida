namespace AlloyClient.Editor;

public enum DrawType {
    Ground = 0,
    Objects = 1,
    Regions = 2
}

public sealed class MapTileData {
    public int GroundType = -1; // -1: no ground
    public int ObjType;         // 0: no object
    public string ObjCfg;
    public int RegType;         // 0: no region
    public int TerrainType;
    public int Elevation;

    public bool IsEmpty => GroundType == -1 && ObjType == 0 && RegType == 0 && TerrainType == 0 && Elevation == 0 && string.IsNullOrEmpty(ObjCfg);

    public void Copy(MapTileData other) {
        GroundType = other?.GroundType ?? -1;
        ObjType = other?.ObjType ?? 0;
        ObjCfg = other?.ObjCfg;
        RegType = other?.RegType ?? 0;
        TerrainType = other?.TerrainType ?? 0;
        Elevation = other?.Elevation ?? 0;
    }

    public MapTileData Clone() {
        return new MapTileData {
            GroundType = GroundType,
            ObjType = ObjType,
            ObjCfg = ObjCfg,
            RegType = RegType,
            TerrainType = TerrainType,
            Elevation = Elevation
        };
    }

    public bool SameAs(MapTileData other) {
        return other != null &&
               GroundType == other.GroundType &&
               ObjType == other.ObjType &&
               RegType == other.RegType &&
               TerrainType == other.TerrainType &&
               Elevation == other.Elevation &&
               (ObjCfg ?? "") == (other.ObjCfg ?? "");
    }
}

public sealed class MapSelectData {
    public static readonly MapSelectData Empty = new(-1, -1, -1, -1);

    public readonly int StartX;
    public readonly int StartY;
    public readonly int EndX;
    public readonly int EndY;
    public readonly int Width;
    public readonly int Height;
    public MapDragAction LastDragAction;

    public MapSelectData(int startX, int startY, int endX, int endY, MapDragAction dragAction = null) {
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        LastDragAction = dragAction;
        Width = EndX - StartX + 1;
        Height = EndY - StartY + 1;
    }

    public bool IsEmpty => StartX == -1;

    public MapSelectData Clone() => IsEmpty ? Empty : new MapSelectData(StartX, StartY, EndX, EndY, LastDragAction);
}

public sealed class MEClipboard {
    public int Width;
    public int Height;
    private MapTileData[] _tiles;

    public void SetSize(int width, int height) {
        Width = width;
        Height = height;
        _tiles = new MapTileData[width * height];
    }

    public void AddTile(MapTileData tile, int x, int y) {
        var idx = x + y * Width;
        if (_tiles == null || idx < 0 || idx >= _tiles.Length) return;
        _tiles[idx] = tile;
    }

    public MapTileData GetTile(int x, int y) {
        var idx = x + y * Width;
        if (_tiles == null || idx < 0 || idx >= _tiles.Length) return null;
        return _tiles[idx];
    }

    public void Clear() {
        Width = 0;
        Height = 0;
        _tiles = null;
    }
}
