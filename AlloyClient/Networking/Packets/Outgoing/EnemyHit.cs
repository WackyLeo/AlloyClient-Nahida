using System;
using System.Collections.Generic;
using Common;

namespace AlloyClient.Networking.Packets.Outgoing;

public class EnemyHit : OutgoingPacket<EnemyHit> {

    public int Stack;
    public List<int> HitInfo = new();

    public override PacketId PacketId => PacketId.EnemyHit;

    public override void Reset() {
        Stack = 0;
        HitInfo.Clear();
    }

    public void AddHit(int time, int bulletId, int targetId, bool killed) {
        Stack++;
        HitInfo.Add(time);
        HitInfo.Add(bulletId);
        HitInfo.Add(targetId);
        HitInfo.Add(killed ? 1 : 0);
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(Stack);
        foreach (var val in HitInfo)
            writer.Write(val);
    }

    public override string ToString() {
        return $"Stack: {Stack}, HitInfo: {HitInfo}";
    }
}