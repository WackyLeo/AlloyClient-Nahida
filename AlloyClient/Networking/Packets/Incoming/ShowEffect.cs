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

        switch (EffectType) {
            case EffectType.Heal:
                if (entity == null) return;
                effect = new HealEffect(entity, Color);
                break;
            case EffectType.Teleport:
                effect = new TeleportEffect(new Vector2(Pos1.X, Pos1.Y));
                break;
            case EffectType.Stream:
                effect = new StreamEffect(new Vector2(Pos1.X, Pos1.Y), new Vector2(Pos2.X, Pos2.Y), Color);
                break;
            case EffectType.Throw:
                var start = entity != null ? entity.Position : new Vector2(Pos2.X, Pos2.Y);
                var end = new Vector2(Pos1.X, Pos1.Y);
                var duration = Duration != 0 ? Duration : 1.5f;
                effect = new ThrowEffect(start, end, Color, (int)(duration * 1000));
                break;
            case EffectType.Nova:
                effect = new NovaEffect(entity, Pos1.X, Color);
                break;
            case EffectType.Poison:
                effect = new PoisonEffect(entity, Color);
                break;
            case EffectType.Trail:
                effect = new LineEffect(entity, new Vector2(Pos1.X, Pos1.Y), Color);
                break;
            case EffectType.Burst:

                break;
            case EffectType.Flow:

                break;
            case EffectType.Trap:

                break;
            case EffectType.Lightning:
                effect = new LightningEffect(new Vector2(Map.LocalPlayer.Position.X, Map.LocalPlayer.Position.Y),
                    new Vector2(Pos1.X, Pos1.Y), Color, (int)Pos2.X);
                break;
            case EffectType.Collapse:

                break;
            case EffectType.ConeBlast:

                break;
            case EffectType.Earthquake:

                break;
            case EffectType.Flashing:

                break;
            case EffectType.ObjectToss:

                break;
            case EffectType.Vortex:

                break;
            case EffectType.FadeToBlack:
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