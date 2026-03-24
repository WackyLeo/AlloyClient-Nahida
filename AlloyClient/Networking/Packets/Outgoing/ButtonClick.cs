using AlloyClient.Networking.Structs.DataObjects;

namespace AlloyClient.Networking.Packets.Outgoing;

public class ButtonClick : OutgoingPacket<ButtonClick> {
    public Position Position;

    public override PacketId PacketId => PacketId.ButtonClick;

    public override void Reset() {
        Position = default;
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(Position.X);
        writer.Write(Position.Y);
    }

    public override string ToString() {
        return $"Position: {Position}";
    }
}