using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Utils;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Networking.Packets.Incoming;

public class EnemyShoot : IncomingPacket<EnemyShoot> {
    public int BulletId;
    public int OwnerId;
    public int ContainerType;
    public byte BulletType;
    public float Angle;
    public short Damage;
    public byte NumShots;
    public float AngleInc;
    public Position[] StartingPositions;
    public bool LargeStorage;
    public float OrbitLine;

   public override PacketId PacketId => PacketId.EnemyShoot;

    public override void Reset() {
        BulletId = 0;
        OwnerId = 0;
        ContainerType = 0;
        BulletType = 0;
        Angle = 0;
        Damage = 0;
        NumShots = 0;
        AngleInc = 0;
        StartingPositions = null;
        LargeStorage = false;
        OrbitLine = 0;
    }

    public override void Read(NetworkReader reader) {
        BulletId = reader.ReadInt32();
        OwnerId = reader.ReadInt32();
        ContainerType = reader.ReadInt32();
        BulletType = reader.ReadByte();
        Angle = reader.ReadSingle();
        Damage = reader.ReadInt16();
        NumShots = reader.ReadByte();
        AngleInc = reader.ReadSingle();
        StartingPositions = new Position[NumShots];
        for (var i = 0; i < NumShots; i++) {
            StartingPositions[i] = new Position();
            StartingPositions[i].Read(reader);
        }
        LargeStorage = reader.ReadBoolean();
        OrbitLine = reader.ReadSingle();
    }

    public override void Handle() {
        if (!Map.Entities.TryGetValue(OwnerId, out var en))
            return;

        ushort type;
        ProjectileProperties projDesc;
        ObjectProperties objDesc;
        try {
            type = ObjectLibrary.IdToObjectType[en.Properties.Projectiles[BulletType].ObjectId];
            projDesc = en.Properties.Projectiles[BulletType];
            objDesc = ObjectLibrary.TypeToObjectProps[type];
        } catch {
            Logger.Error($"Projectile '{BulletType}' out of range on '{en.Properties.DisplayName}'", "EnemyShoot");
            type = ObjectLibrary.IdToObjectType[en.Properties.ProjectilesDict[BulletType].ObjectId];
            projDesc = en.Properties.ProjectilesDict[BulletType];
            objDesc = ObjectLibrary.TypeToObjectProps[type];
        }

        var bulletIdMod = LargeStorage ? 4096 : 256;

        for (var i = 0; i < NumShots; i++) {
            var proj = ObjectPools.Projectiles.Pop();
            var startPos = StartingPositions[i];
            proj.Reset((BulletId + i) % bulletIdMod, Damage, Angle + AngleInc * i, en, objDesc, projDesc);
            proj.SetStartPosition(new Vector2(startPos.X, startPos.Y));
            Map.AddProjectile(proj, projDesc.ProjDelayMs);
        }

        en.SetAttack(OwnerId, Angle + AngleInc * (NumShots - 1) / 2f);
    }

    public override string ToString() {
        return $"BulletId: {BulletId}, OwnerId: {OwnerId}, BulletType: {BulletType}, Angle: {Angle}, Damage: {Damage}, StartingPositions: {StartingPositions}, NumShots: {NumShots}, AngleInc: {AngleInc}";
    }
}