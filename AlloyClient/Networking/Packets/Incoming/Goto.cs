using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;

namespace AlloyClient.Networking.Packets.Incoming;

public class Goto : IncomingPacket<Goto> {
    public int ObjectId;
    public Position Pos;

    public override PacketId PacketId => PacketId.Goto;

    public override void Reset() {
        ObjectId = 0;
        Pos.Reset();
    }

    public override void Read(NetworkReader reader) {
        ObjectId = reader.ReadInt32();
        Pos.Read(reader);
    }

    public override void Handle() {
        var gt = GotoAck.CreatePacket();
        gt.Time = (int)Main.GameTime.TotalMs;
        Client.QueuePacket(gt);

        if (Map.Entities.TryGetValue(ObjectId, out var entity)) {
            entity.MoveTo(Pos.X, Pos.Y, entity == Map.LocalPlayer);
        
            entity.TickPosition.X = Pos.X;
            entity.TickPosition.Y = Pos.Y;
            entity.PositionAtTick.X = Pos.X;
            entity.PositionAtTick.Y = Pos.Y;
        
            entity.MovementVector.X = 0;
            entity.MovementVector.Y = 0;
        }
    }

    public override string ToString() {
        return $"ObjectId: {ObjectId}, Pos: {Pos}";
    }
}