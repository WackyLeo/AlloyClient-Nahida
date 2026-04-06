namespace AlloyClient.Ui.Storage;

public class StorageItem {
    public readonly ushort Id;
    public readonly bool IsPotion;
    public readonly bool IsToken;
    public int Capacity;
    public int Count;

    public StorageItem(ushort id, bool isPotion, bool isToken = false, int count = 0, int capacity = 0) {
        Id = id;
        IsPotion = isPotion;
        IsToken = isToken;
        Count = count;
        Capacity = capacity;
    }

    public void UpdateCount(int count, int capacity) {
        Count = count;
        Capacity = capacity;
    }

    public void ResetCount() {
        Count = 0;
        Capacity = 0;
    }
}