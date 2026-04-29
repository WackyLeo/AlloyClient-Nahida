using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.State;

namespace AlloyClient.Networking.Packets.Incoming;

public class AllyShoot : IncomingPacket<AllyShoot> {
    public float[] Angles;
    public int[] BulletIds;
    public ushort[] ContainerTypes;
    public int OwnerId;
    public int[] ProjIds;

    public int Size;
    public int[] TimeDiff;

    public override PacketId PacketId => PacketId.AllyShoot;

    public override void Reset() {
        Size = 0;
        OwnerId = 0;
        TimeDiff = null;
        BulletIds = null;
        ContainerTypes = null;
        Angles = null;
        ProjIds = null;
    }

    public override void Read(NetworkReader reader) {
        Size = reader.ReadInt32();
        OwnerId = reader.ReadInt32();
        TimeDiff = new int[Size];
        BulletIds = new int[Size];
        ContainerTypes = new ushort[Size];
        Angles = new float[Size];
        ProjIds = new int[Size];

        for (var i = 0; i < Size; i++) {
            TimeDiff[i] = reader.ReadInt32();
            BulletIds[i] = reader.ReadInt32();
            ContainerTypes[i] = reader.ReadUInt16();
            Angles[i] = reader.ReadSingle();
            ProjIds[i] = reader.ReadInt32();
        }
    }

    public override void Handle() {
        if (!Map.Entities.TryGetValue(OwnerId, out var owner) || owner is not Player player)
            return;

        for (var i = 0; i < Size; i++) {
            var containerType = ContainerTypes[i];
            var bulletType = (byte)(ProjIds[i] & 0xFF);

            if (!ObjectLibrary.TypeToObjectProps.TryGetValue(containerType, out var objProps))
                continue;

            if (!objProps.ProjectilesDict.TryGetValue(bulletType, out var projProps))
                if (objProps.Projectiles == null || objProps.Projectiles.Count == 0) continue;
                else projProps = objProps.Projectiles[0];

            if (!ObjectLibrary.IdToObjectType.TryGetValue(projProps.ObjectId, out var projObjType) ||
                !ObjectLibrary.TypeToObjectProps.TryGetValue(projObjType, out var projObjDesc))
                continue;

            var proj = ObjectPools.Projectiles.Pop();
            proj.Reset(BulletIds[i], 0, Angles[i], player, projObjDesc, projProps);
            proj.SetStartPosition(player.Position);
            proj.AddProjDelay(TimeDiff[i]);
            if (proj.RenderBaseType != null)
                proj.RenderBaseType.SetAlpha(Settings.AllyProjOpacity);
            Map.AddProjectile(proj);

            player.SetAttack(containerType, Angles[i]);
        }
    }

    public override string ToString() {
        return
            $"Size: {Size}, OwnerId: {OwnerId}, TimeDiff: {TimeDiff}, BulletIds: {BulletIds}, ContainerTypes: {ContainerTypes}, Angles: {Angles}, ProjIds: {ProjIds}";
    }
}