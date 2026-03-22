using AlloyClient.Networking.Structs.DataObjects;

namespace AlloyClient.Networking.Packets.Outgoing;

public class GroundDamage : OutgoingPacket<GroundDamage> {
    public int Time;
    public Position Position;
    
    public override PacketId PacketId => PacketId.GroundDamage;

    public override void Reset() {
        Time = 0;
        Position = default;
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(Time);
        writer.Write(Position.X);
        writer.Write(Position.Y);
    }

    public override string ToString() {
        return $"Time: {Time}, Position: ({Position.X}, {Position.Y})";
    }
}