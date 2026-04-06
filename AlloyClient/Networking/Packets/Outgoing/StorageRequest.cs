namespace AlloyClient.Networking.Packets.Outgoing;

public class StorageRequest : OutgoingPacket<StorageRequest> {
    public override PacketId PacketId => PacketId.StorageRequest;

    public override void Reset() {
    }

    public override void Write(NetworkWriter writer) {
    }

    public override string ToString() {
        return "StorageRequest";
    }
}