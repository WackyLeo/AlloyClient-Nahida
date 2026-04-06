namespace AlloyClient.Ui.Storage;

public readonly struct StorageStatus {
    public readonly string Text;
    public readonly uint Color;

    private StorageStatus(string text, uint color) {
        Text = text;
        Color = color;
    }

    public static StorageStatus Full(int count, int capacity) {
        return new StorageStatus($"{count}/{capacity}", 0xFFFF3333u);
    }

    public static StorageStatus Normal(int count, int capacity) {
        return new StorageStatus($"{count}/{capacity}", 0xFFFFFFFFu);
    }

    public static StorageStatus Empty() {
        return new StorageStatus("EMPTY", 0xFF666666u);
    }

    public static StorageStatus Get(int count, int capacity) {
        if (count == 0) return Empty();
        if (count >= capacity) return Full(count, capacity);
        return Normal(count, capacity);
    }
}