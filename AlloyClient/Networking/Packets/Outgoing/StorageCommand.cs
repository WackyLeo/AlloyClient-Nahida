namespace AlloyClient.Networking.Packets.Outgoing;

public class StorageCommand : OutgoingPacket<StorageCommand> {
    public int Cmd;
    public int ObjType;
    public override PacketId PacketId => PacketId.StorageCommand;

    public override void Reset() {
        Cmd = 0;
        ObjType = 0;
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(Cmd);
        writer.Write(ObjType);
    }

    public override string ToString() {
        return $"StorageCommand: Cmd {Cmd},  ObjType {ObjType}";
    }
}