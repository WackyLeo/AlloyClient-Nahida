using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.State;
using OpenTK.Mathematics;

namespace AlloyClient.Networking.Packets.Incoming;

public class ServerPlayerShoot : IncomingPacket<ServerPlayerShoot> {
    public int BulletId;
    public int OwnerId;
    public int ContainerType;
    public Position StartingPos;
    public float Angle;
    public short Damage;
    public int ProjectileIndex;
    public bool UsePlayerPos;

    public override PacketId PacketId => PacketId.ServerPlayerShoot;

    public override void Reset() {
        BulletId = 0;
        OwnerId = 0;
        ContainerType = 0;
        StartingPos.Reset();
        Angle = 0;
        Damage = 0;
        ProjectileIndex = 0;
        UsePlayerPos = false;
    }

    public override void Read(NetworkReader reader) {
        BulletId = reader.ReadInt32();
        OwnerId = reader.ReadInt32();
        ContainerType = reader.ReadInt32();
        StartingPos.Read(reader);
        Angle = reader.ReadSingle();
        Damage = reader.ReadInt16();
        ProjectileIndex = reader.ReadInt32();
        UsePlayerPos = reader.ReadBoolean();
    }

    public override void Handle() {
        if (Settings.AllyProjOpacity == 0 && OwnerId != Map.LocalPlayerId)
            return;
        if (!Map.Entities.TryGetValue(OwnerId, out var owner))
            return;

        if (OwnerId == Map.LocalPlayerId &&
            Map.LocalPlayer.PendingLocalProjectiles.TryGetValue(BulletId, out var existingProj)) {
            Map.LocalPlayer.PendingLocalProjectiles.Remove(BulletId);

            if (!existingProj.IsInPool) {
                existingProj.SetDamage(Damage);
                return;
            }
            // Fell out of the pool already (expired/recycled) — fall through and spawn fresh below.
        }

        if (!ObjectLibrary.TypeToObjectProps.TryGetValue((ushort)ContainerType, out var itemDesc))
            return;
        if (itemDesc.Projectiles == null || itemDesc.Projectiles.Count == 0)
            return;
        var projDesc = itemDesc.Projectiles[Math.Clamp(ProjectileIndex, 0, itemDesc.Projectiles.Count - 1)];
        if (string.IsNullOrEmpty(projDesc.ObjectId))
            return;

        if (!ObjectLibrary.IdToObjectType.TryGetValue(projDesc.ObjectId, out var projObjType))
            return;
        if (!ObjectLibrary.TypeToObjectProps.TryGetValue(projObjType, out var projObjDesc))
            return;

        var proj = ObjectPools.Projectiles.Pop();
        proj.Reset(BulletId, Damage, Angle, owner, projObjDesc, projDesc);

        if (UsePlayerPos)
            proj.SetStartPosition(new Vector2(owner.Position.X, owner.Position.Y));
        else
            proj.SetStartPosition(new Vector2(StartingPos.X, StartingPos.Y));

        if (OwnerId != Map.LocalPlayerId)
            proj.RenderBaseType.SetAlpha(Settings.AllyProjOpacity);

        Map.AddProjectile(proj);
    }

    public override string ToString() {
        return
            $"BulletId: {BulletId}, OwnerId: {OwnerId}, ContainerType: {ContainerType}, StartingPos: {StartingPos}, Angle: {Angle}, Damage: {Damage}";
    }
}