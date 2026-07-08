using AlloyClient.Game;
using AlloyClient.Networking.Enums;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.ParticleEffects;
using OpenTK.Mathematics;

namespace AlloyClient.Networking.Packets.Incoming;

public class ShowEffect : IncomingPacket<ShowEffect> {
    public uint Color;
    public float Duration;
    public EffectType EffectType;
    public Position Pos1;
    public Position Pos2;
    public float Strength;
    public int TargetObjectId;

    public override PacketId PacketId => PacketId.ShowEffect;

    public override void Reset() {
        EffectType = default;
        TargetObjectId = 0;
        Color = 0;
        Pos1.Reset();
        Pos2.Reset();
        Duration = 0;
        Strength = 0;
    }

    public override void Read(NetworkReader reader) {
        EffectType = (EffectType)reader.ReadByte();
        TargetObjectId = reader.ReadInt32();
        Pos1.Read(reader);
        Pos2.Read(reader);
        Color = reader.ReadUInt32();
        Duration = reader.ReadSingle();
        Strength = reader.ReadSingle();
    }

    public override void Handle() {
        ParticleEffect effect = null;
        Map.Entities.TryGetValue(TargetObjectId, out var entity);

        var pos1 = new Vector2(Pos1.X, Pos1.Y);
        var pos2 = new Vector2(Pos2.X, Pos2.Y);

        switch (EffectType) {
            case EffectType.Heal:
                if (entity == null) return;
                effect = new HealEffect(entity, Color);
                break;
            case EffectType.Teleport:
                effect = new TeleportEffect(pos1);
                break;
            case EffectType.Stream:
                effect = new StreamEffect(pos1, pos2, Color);
                break;
            case EffectType.Throw:
                var start = entity != null ? entity.Position : pos2;
                var duration = Duration != 0 ? Duration : 1.5f;
                effect = new ThrowEffect(start, pos1, Color, (int)(duration * 1000));
                break;
            case EffectType.Nova:
                if (entity == null) return;
                effect = new NovaEffect(entity, Pos1.X, Color);
                break;
            case EffectType.Poison:
                if (entity == null) return;
                effect = new PoisonEffect(entity, Color);
                break;
            case EffectType.Trail:
                if (entity == null) return;
                effect = new LineEffect(entity, pos1, Color);
                break;
            case EffectType.Burst:
                if (entity == null) return;
                effect = new BurstEffect(pos1, pos2, Color);
                break;
            case EffectType.Flow:
                if (entity == null) return;
                effect = new FlowEffect(pos1, entity, Color);
                break;
            case EffectType.Ring:
                if (entity == null) return;
                effect = new RingEffect(entity, Pos1.X, Color);
                break;
            case EffectType.Lightning:
                if (entity == null && Map.LocalPlayer == null) return;
                var lightningStart = entity?.Position ?? Map.LocalPlayer.Position;
                effect = new LightningEffect(lightningStart, pos1, Color, (int)Pos2.X);
                break;
            case EffectType.Collapse:
                if (entity == null) return;
                effect = new CollapseEffect(pos1, pos2, Color);
                break;
            case EffectType.ConeBlast:
                if (entity == null) return;
                effect = new ConeBlastEffect(entity, pos1, Pos2.X, Color);
                break;
            case EffectType.Jitter:
                // Todo: Jitta
                return;
            case EffectType.Flashing:
                // Todo: Flashin
                return;
            case EffectType.ThrowProjectile:
                // Todo: Throwa
                effect = new ThrowEffect(pos2, pos1, Color, Duration > 0 ? (int)(Duration * 1000) : 1500);
                break;
            case EffectType.Shocker:
                if (entity == null) return;
                effect = new ShockerEffect(entity);
                break;
            case EffectType.Shockee:
                if (entity == null) return;
                effect = new ShockeeEffect(entity);
                break;
            case EffectType.RisingFury:
                if (entity == null) return;
                effect = new RisingFuryEffect(entity, Pos1.X * 1000.0);
                break;
            case EffectType.NecroLine:
                if (entity == null || Map.LocalPlayer == null) return;
                effect = new NecroLineEffect(Map.LocalPlayer.Position, entity, Color, Strength);
                break;
            case EffectType.NecroRadius:
                if (Map.LocalPlayer == null) return;
                effect = new NecroRadiusEffect(Map.LocalPlayer, Color, Duration);
                break;
            case EffectType.NecroDamage:
                effect = new NecroDamageEffect(pos1, Color, Duration);
                break;
            default:
                return;
        }

        if (effect == null) return;

        Map.AddParticleEffect(effect);
    }

    public override string ToString() {
        return
            $"EffectType: {EffectType}, TargetObjectId: {TargetObjectId}, Pos1: {Pos1}, Pos2: {Pos2}, Color: {Color}";
    }
}
