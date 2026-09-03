using AlloyClient.Game;

namespace AlloyClient.Networking.Packets.Incoming;

public class PlaySound : IncomingPacket<PlaySound> {
    public int OwnerId;
    public int SoundId;

    public override PacketId PacketId => PacketId.PlaySound;

    public override void Reset() {
        OwnerId = 0;
        SoundId = 0;
    }

    public override void Read(NetworkReader reader) {
        OwnerId = reader.ReadInt32();
        SoundId = reader.ReadInt32();
    }

    public override void Handle() {
        Map.Entities.TryGetValue(OwnerId, out var entity);

        if (entity == null) return;

        if (!string.IsNullOrEmpty(entity.Properties.OldSound))
            Audio.SfxChannel.Play(entity.Properties.OldSound);
        else if (!string.IsNullOrEmpty(entity.Properties.Sound))
            Audio.SfxChannel.Play(entity.Properties.Sound);
    }

    public override string ToString() {
        return $"OwnerId: {OwnerId}, SoundId: {SoundId}";
    }
}