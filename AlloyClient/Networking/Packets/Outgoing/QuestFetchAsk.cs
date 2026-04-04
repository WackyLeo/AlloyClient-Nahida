namespace AlloyClient.Networking.Packets.Outgoing;

public class QuestFetchAsk : OutgoingPacket<QuestFetchAsk> {
    public override PacketId PacketId => PacketId.QuestFetchAsk;

    public override void Reset() {
    }

    public override void Write(NetworkWriter writer) {
    }

    public override string ToString() {
        return "";
    }
}