namespace AlloyClient.Networking.Packets.Outgoing;

public class Load : OutgoingPacket<Load> {
    public int CharId;
    public bool IsFromArena;

    public override PacketId PacketId => PacketId.Load;

    public override void Reset() {
        CharId = 0;
        IsFromArena = false;
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(CharId);
        writer.Write(IsFromArena);
    }

    public override string ToString() {
        return $"CharId: {CharId}, IsFromArena: {IsFromArena}";
    }
}