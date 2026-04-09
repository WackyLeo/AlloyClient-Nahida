using AlloyClient.Game;
using AlloyClient.Sound;

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

        if (!string.IsNullOrEmpty(entity.Properties.Sound))
            SoundManager.PlaySoundEffect(entity.Properties.Sound);
        else if (!string.IsNullOrEmpty(entity.Properties.OldSound))
            SoundManager.PlaySoundEffect(entity.Properties.OldSound);
    }

    public override string ToString() {
        return $"OwnerId: {OwnerId}, SoundId: {SoundId}";
    }
}