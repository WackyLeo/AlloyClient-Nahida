using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;

namespace AlloyClient.Editor;

public sealed class MEBrush {
    public const int ShapeRandom = 1;
    public const int ShapeRandom2 = 2;

    public List<Vector2i> ShapeTiles = [];
    public DrawType ElementType;
    public int GroundType = -1;
    public int ObjType;
    public int RegType;

    public int Size { get; private set; }
    public int BrushShape { get; private set; }
    public int Chance { get; private set; } = 25;
    public bool Replace { get; private set; } = true;

    /// <summary>Raised whenever a brush option (size/shape/chance/replace) changes.</summary>
    public event Action Changed;

    public MEBrush(DrawType drawType) {
        ElementType = drawType;
    }

    public void SetSize(int size) {
        Size = Math.Clamp(size, 0, 99);
        Changed?.Invoke();
    }

    public void SetBrushShape(int shape) {
        BrushShape = shape;
        Changed?.Invoke();
    }

    public void SetChance(int chance) {
        Chance = Math.Clamp(chance, 1, 100);
        Changed?.Invoke();
    }

    public void SetReplace(bool value) {
        Replace = value;
        Changed?.Invoke();
    }

    public int GetElement(DrawType type) => type switch {
        DrawType.Ground => GroundType,
        DrawType.Objects => ObjType,
        DrawType.Regions => RegType,
        _ => 0
    };

    public void SetElement(DrawType type, int value) {
        switch (type) {
            case DrawType.Ground:
                GroundType = value;
                break;
            case DrawType.Objects:
                ObjType = value;
                break;
            case DrawType.Regions:
                RegType = value;
                break;
        }
    }

    /// <summary>The "nothing selected" value for the current element type.</summary>
    public static int EmptyValue(DrawType type) => type == DrawType.Ground ? -1 : 0;
}

/// <summary>Saves the current map to the autoSave folder every 30 seconds (as a backup, it doesn't count as a user save).</summary>
public sealed class AutoMapSaver {
    private const double CooldownMs = 30000;

    private double _cooldown = CooldownMs;
    public bool Disabled;

    public static string AutoSaveFolder => Path.Combine(AppContext.BaseDirectory, "autoSave");

    public void TrySaveMap(MapData mapData, double deltaMs) {
        _cooldown -= deltaMs;
        if (_cooldown > 0) return;
        _cooldown = CooldownMs;

        if (Disabled || mapData == null || mapData.SavedChanges || mapData.Tiles.Length == 0) return;

        var name = string.IsNullOrWhiteSpace(mapData.MapName) ? "untitled" : mapData.MapName;
        mapData.SaveFile(Path.Combine(AutoSaveFolder, name + ".wmap"), true, false); // wmap is faster
    }
}
