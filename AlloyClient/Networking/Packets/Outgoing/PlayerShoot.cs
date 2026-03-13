using System;
using AlloyClient.Networking.Structs.DataObjects;

namespace AlloyClient.Networking.Packets.Outgoing;

public class PlayerShoot : OutgoingPacket<PlayerShoot> {

    public float Angle;
    public Position StartingPos;
    public int Time;
    public bool IsServerShoot;
    public float AngleInc;
    public int[] DamageList;
    public float[] CritList;
    public ushort ItemType;

    public override PacketId PacketId => PacketId.PlayerShoot;

    public override void Reset() {
        Time = 0;
        ItemType = 0;
        StartingPos.Reset();
        Angle = 0f;
        DamageList = null;
        CritList = null;
        AngleInc = 0f;
        IsServerShoot = false;
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(1);
        writer.Write(Time);
        writer.Write(0);
        writer.Write(ItemType);
        writer.Write((double)StartingPos.X);
        writer.Write((double)StartingPos.Y);
        writer.Write(Angle);
    }

    public override string ToString() {
        return $"Angle: {Angle}, Time: {Time}, ContainerType: {ItemType}";
    }
}