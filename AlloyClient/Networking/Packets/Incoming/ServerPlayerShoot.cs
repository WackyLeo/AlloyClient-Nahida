using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Networking.Structs.DataObjects;
using OpenTK.Mathematics;

namespace AlloyClient.Networking.Packets.Incoming;

public class ServerPlayerShoot : IncomingPacket<ServerPlayerShoot> {
    public int BulletId;
    public int OwnerId;
    public int ContainerType;
    public Position StartingPos;
    public float Angle;
    public short Damage;

    public override PacketId PacketId => PacketId.ServerPlayerShoot;

    public override void Reset() {
        BulletId = 0;
        OwnerId = 0;
        ContainerType = 0;
        StartingPos.Reset();
        Angle = 0;
        Damage = 0;
    }

    public override void Read(NetworkReader reader) {
        BulletId = reader.ReadInt32();
        OwnerId = reader.ReadInt32();
        ContainerType = reader.ReadInt32();
        StartingPos.Read(reader);
        Angle = reader.ReadSingle();
        Damage = reader.ReadInt16();
    }

    public override void Handle() {
        if (!Map.Entities.TryGetValue(OwnerId, out var owner)) return;
        if (!ObjectLibrary.TypeToObjectProps.TryGetValue((ushort)ContainerType, out var objDesc)) return;
        
        var enumerator = objDesc.Projectiles.GetEnumerator();
        if (!enumerator.MoveNext()) return;
        var projDesc = enumerator.Current;

        var proj = new Projectile();
        proj.Reset(BulletId, Damage, Angle, owner, objDesc, projDesc);
        
        proj.SetStartPosition(new Vector2(StartingPos.X, StartingPos.Y));
        proj.MarkActive(Map.CurrentTime);
        
        Map.AddProjectile(proj);
    }

    public override string ToString() {
        return $"BulletId: {BulletId}, OwnerId: {OwnerId}, ContainerType: {ContainerType}, StartingPos: {StartingPos}, Angle: {Angle}, Damage: {Damage}";
    }
}