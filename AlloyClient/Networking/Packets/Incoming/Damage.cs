using System;
using System.Numerics;
using AlloyClient.Game;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Ui.Character;
using AlloyClient.Game.Objects;

namespace AlloyClient.Networking.Packets.Incoming;

public class Damage : IncomingPacket<Damage> {
    public int TargetId;
    public ConditionEffects Effects;
    public ushort DamageAmount;
    public bool Kill;
    public byte BulletId;
    public int ObjectId;

    public override PacketId PacketId => PacketId.Damage;

    public override void Reset() {
        TargetId = 0;
        Effects = 0;
        DamageAmount = 0;
        Kill = false;
        BulletId = 0;
        ObjectId = 0;
    }

    public override void Read(NetworkReader reader) {
        TargetId = reader.ReadInt32();

        byte c = reader.ReadByte();
        Effects = 0;
        for (int i = 0; i < c; i++)
            Effects |= (ConditionEffects)(1 << reader.ReadByte());

        DamageAmount = reader.ReadUInt16();
        Kill = reader.ReadBoolean();
        BulletId = reader.ReadByte();
        ObjectId = reader.ReadInt32();
    }

    public override void Handle() {
        if (!Map.Entities.TryGetValue(TargetId, out var target))
            return;

        // Need to add damage text colors including dynamic color and its toggle in options
        // Entity.GetHitColour(Effects, ArmorPiercing/ArmorBroken);
        
        String notification = "";
        uint clr;
        
        if (Kill) {
            target.PredictedHp = Math.Min(target.PredictedHp, 0);
            notification = $"-{DamageAmount} [{target.PredictedHp}]";
            clr = 0xFF0000;
            NotificationLayer.AddStatusText(target, notification, clr, 1000, 0);
        } else if (target.Hp > 0) {
            target.Hp -= DamageAmount;
            notification = $"-{DamageAmount} [{target.Hp}]";
            clr = 0xFF0000;
            NotificationLayer.AddStatusText(target, notification, clr, 1000, 0);
        }
    }

    public override string ToString() {
        return $"TargetId: {TargetId}, Effects: {Effects}, DamageAmount: {DamageAmount}, Kill: {Kill}, BulletId: {BulletId}, ObjectId: {ObjectId}";
    }
}