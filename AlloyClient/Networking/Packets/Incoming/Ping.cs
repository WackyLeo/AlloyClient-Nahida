using AlloyClient.Networking;
using AlloyClient.Networking.Packets;
using AlloyClient.Networking.Packets.Outgoing;
using System;
using AlloyClient;

public class Ping : IncomingPacket<Ping>
{
    public int Serial;
    public int RTT;
    public override PacketId PacketId => PacketId.Ping;
    public override void Reset()
    {
        Serial = 0;
        RTT = 0;
    }
    public override void Read(NetworkReader reader)
    {
        Serial = reader.ReadInt32();
        RTT = reader.ReadInt32();
    }
    public override void Handle()
    {
        var pong = Pong.CreatePacket();
        pong.Serial = Serial;
        pong.Time = (int)Main.GameTime.TotalMs;
        Client.QueuePacket(pong);
    }
}