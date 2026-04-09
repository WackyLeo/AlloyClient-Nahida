namespace AlloyClient.Networking.Packets.Incoming;

public class SwitchMusic : IncomingPacket<SwitchMusic> {
    public string Music;

    public override PacketId PacketId => PacketId.SwitchMusic;

    public override void Reset() {
        Music = "";
    }

    public override void Read(NetworkReader reader) {
        Music = reader.ReadUTF();
    }

    public override void Handle() {
        Sound.Music.PlayMusic(Music);
    }

    public override string ToString() {
        return $"music: {Music}";
    }
}