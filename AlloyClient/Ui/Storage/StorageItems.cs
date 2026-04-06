using System.Collections.Generic;

namespace AlloyClient.Ui.Storage;

public static class StorageItems {
    public static readonly ushort[] Potions = {
        0xae9, 0xaea, 0xa1f, 0xa20, 0xa21, 0xa4c, 0xa34, 0xa35
    };

    public static readonly ushort[] Marks = {
        0x1e3b, 0x1e2f, 0x1e28, 0x1e27, 0x1e26, 0x15E8, 0x1e2b, 0x1e2c,
        0x1e2a, 0x1e29, 0x1e2d, 0x15E9, 0x1e2e, 0x1e31, 0x1e34, 0x1e36,
        0x1e3a, 0x1e38, 0x1e37, 0x1e3c, 0x1e30, 0x1e32, 0x1e33, 0xEBB,
        0x15E5, 0xEBC, 0xBB9, 0xBAF, 0xBB8, 0xEBD, 0x1e35, 0x41f,
        0x41e, 0x13ae, 0x13af
    };

    public static readonly ushort[] Tokens = {
        0x15B6, 0x15B7, 0x15B8, 0x15B9, 0x15BA, 0x15BB
    };

    public static int Level = 0;
    public static int Capacity = 0;
    public static int PurchasePrice = 1000;
    public static Dictionary<ushort, StorageItem> ItemDict = new();

    public static void Load() {
        Reset();
        foreach (var id in Potions) ItemDict[id] = new StorageItem(id, true);
        foreach (var id in Marks) ItemDict[id] = new StorageItem(id, false);
        foreach (var id in Tokens) ItemDict[id] = new StorageItem(id, false, true);
    }

    public static void Reset() {
        ItemDict = new Dictionary<ushort, StorageItem>();
    }

    public static void ResetCounts() {
        foreach (var item in ItemDict.Values)
            item.UpdateCount(0, Capacity);
    }

    public static int GetUpgradePrice() {
        if (Level <= 7)
            return (Level + 1) * 500;
        return (Level + 1) * 500 * (Level - 6);
    }
}