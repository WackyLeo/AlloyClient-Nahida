using AlloyClient.Ui.Storage;

namespace AlloyClient.Networking.Packets.Incoming;

public class StorageUpdate : IncomingPacket<StorageUpdate> {
    public int Capacity;
    public int Level;
    public string Str;

    public override PacketId PacketId => PacketId.StorageUpdate;

    public override void Reset() {
        Level = 0;
        Capacity = 0;
        Str = "";
    }

    public override void Read(NetworkReader reader) {
        Level = reader.ReadInt32();
        Capacity = reader.ReadInt32();
        Str = reader.ReadUTF();
    }

    public override void Handle() {
        StorageItems.Level = Level;
        StorageItems.Capacity = Capacity;
        StorageItems.ResetCounts();

        if (!string.IsNullOrEmpty(Str)) {
            var items = Str.Split(';');
            foreach (var itemPair in items) {
                var parts = itemPair.Split(':');
                if (parts.Length == 2)
                    if (ushort.TryParse(parts[0], out var objType) && int.TryParse(parts[1], out var count))
                        if (StorageItems.ItemDict.TryGetValue(objType, out var storageItem))
                            storageItem.UpdateCount(count, Capacity);
            }
        }

        StorageWindow.Instance?.FullRefresh();
    }

    public override string ToString() {
        return $"StorageUpdate: Level {Level}, Capacity {Capacity}, Str {Str}";
    }
}