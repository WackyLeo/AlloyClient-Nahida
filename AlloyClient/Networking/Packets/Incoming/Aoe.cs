using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.ParticleEffects;
using OpenTK.Mathematics;

namespace AlloyClient.Networking.Packets.Incoming;

public class Aoe : IncomingPacket<Aoe> {
    public ushort Damage;
    public float Duration;
    public ConditionEffectIndex Effect;
    public ushort OrigType;
    public Position Pos;
    public float Radius;

    public override PacketId PacketId => PacketId.Aoe;

    public override void Reset() {
        Pos.Reset();
        Radius = 0;
        Damage = 0;
        Effect = 0;
        Duration = 0;
        OrigType = 0;
    }

    public override void Read(NetworkReader reader) {
        Pos.Read(reader);
        Radius = reader.ReadSingle();
        Damage = reader.ReadUInt16();
        Effect = (ConditionEffectIndex)reader.ReadByte();
        Duration = reader.ReadSingle();
        OrigType = reader.ReadUInt16();
    }

    public override void Handle() {
        var aoeAck = AoeAck.CreatePacket();
        aoeAck.Time = (int)Main.GameTime.TotalMs;

        if (Map.LocalPlayer == null) {
            aoeAck.Pos = new Position(0, 0);
            Client.QueuePacket(aoeAck);
            return;
        }

        aoeAck.Pos = new Position(Map.LocalPlayer.Position.X, Map.LocalPlayer.Position.Y);

        ParticleEffect aoeEff = new AoeEffect(new Vector2(Pos.X, Pos.Y), Radius, 0xFF0000);
        Map.AddParticleEffect(aoeEff);

        if (Map.LocalPlayer.HasConditionEffect(ConditionEffects.Invincible) ||
            Map.LocalPlayer.HasConditionEffect(ConditionEffects.Paused)) {
            Client.QueuePacket(aoeAck);
            return;
        }

        if (EntityUtils.CalculateDistance(Map.LocalPlayer.Position, new Vector2(Pos.X, Pos.Y)) <= Radius) {
            var actualDamage = Entity.DamageWithDefense(Damage, Map.LocalPlayer.Defense, false, Map.LocalPlayer);
            if (Effect != null && Effect != ConditionEffectIndex.None)
                Map.LocalPlayer.Damage(actualDamage, Effect);
            else
                Map.LocalPlayer.Damage(actualDamage);
        }

        Client.QueuePacket(aoeAck);
    }


    public override string ToString() {
        return
            $"Pos: {Pos}, Radius: {Radius}, Damage: {Damage}, Effect: {Effect}, Duration: {Duration}, OwnerId: {OrigType}";
    }
}