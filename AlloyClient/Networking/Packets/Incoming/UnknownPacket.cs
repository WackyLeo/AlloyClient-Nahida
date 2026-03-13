using AlloyClient.Networking;
using AlloyClient.Networking.Packets;

public class UnknownPacket : IncomingPacket<UnknownPacket>
{
    public override PacketId PacketId => PacketId.Unknown;
    public override void Reset() { }
    public override void Read(NetworkReader reader) { }
    public override void Handle() { }
}