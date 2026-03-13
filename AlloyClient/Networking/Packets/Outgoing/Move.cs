using AlloyClient.Networking.Structs.DataObjects;
using System;
namespace AlloyClient.Networking.Packets.Outgoing;

public class Move : OutgoingPacket<Move>
{
    public int ObjectId;
    public int TickId;
    public int Time;
    public Position NewPosition;
    public TimedPosition[] Records = Array.Empty<TimedPosition>();

    public override PacketId PacketId => PacketId.Move;

    public override void Reset()
    {
        ObjectId = 0;
        TickId = 0;
        Time = 0;
        NewPosition.Reset();
        Records = Array.Empty<TimedPosition>();
    }

    public override void Write(NetworkWriter writer)
    {
        writer.Write(ObjectId);
        writer.Write(TickId);
        writer.Write(Time);
        NewPosition.Write(writer);
        writer.Write((short)Records.Length);
        foreach (var r in Records)
            r.Write(writer);
    }
}