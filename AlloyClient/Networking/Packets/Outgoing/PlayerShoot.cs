using System.Collections.Generic;
using AlloyClient.Networking.Structs.DataObjects;

namespace AlloyClient.Networking.Packets.Outgoing;

public struct ShootData {
    public int Time;
    public int BulletId;
    public ushort ItemType;
    public Position StartingPos;
    public float Angle;
}

public class PlayerShoot : OutgoingPacket<PlayerShoot> {
    public List<ShootData> Shots = new();

    public override PacketId PacketId => PacketId.PlayerShoot;

    public override void Reset() {
        Shots.Clear();
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(Shots.Count);
        foreach (var shot in Shots) {
            writer.Write(shot.Time);
            writer.Write(shot.BulletId);
            writer.Write(shot.ItemType);
            writer.Write((double)shot.StartingPos.X);
            writer.Write((double)shot.StartingPos.Y);
            writer.Write(shot.Angle);
        }
    }

    public override string ToString() {
        return $"PlayerShoot Stacked Shots: {Shots.Count}";
    }
}