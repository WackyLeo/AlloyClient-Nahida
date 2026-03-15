using System.Collections.Generic;

namespace AlloyClient.Networking.Packets.Outgoing;

public class PlayerHit : OutgoingPacket<PlayerHit> {

    public int Stack;
    public List<int> HitInfo = new();

    public override PacketId PacketId => PacketId.PlayerHit;

    public override void Reset() {
        Stack = 0;
        HitInfo.Clear();
    }

    public void AddHit(int bulletId, int objectId) {
        Stack++;
        HitInfo.Add(bulletId);
        HitInfo.Add(objectId);
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(Stack);
        foreach (var val in HitInfo)
            writer.Write(val);
    }
}