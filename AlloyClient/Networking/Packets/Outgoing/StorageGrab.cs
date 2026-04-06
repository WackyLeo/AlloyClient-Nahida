namespace AlloyClient.Networking.Packets.Outgoing;

public class StorageGrab : OutgoingPacket<StorageGrab> {
    public override PacketId PacketId => PacketId.StorageGrab;

    public override void Reset() {
    }

    public override void Write(NetworkWriter writer) {
    }

    public override string ToString() {
        return "StorageGrab";
    }
}