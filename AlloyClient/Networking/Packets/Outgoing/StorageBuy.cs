namespace AlloyClient.Networking.Packets.Outgoing;

public class StorageBuy : OutgoingPacket<StorageBuy> {
    public int ObjType;
    public override PacketId PacketId => PacketId.StorageBuy;

    public override void Reset() {
        ObjType = 0;
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(ObjType);
    }

    public override string ToString() {
        return $"StorageBuy: ObjType {ObjType}";
    }
}