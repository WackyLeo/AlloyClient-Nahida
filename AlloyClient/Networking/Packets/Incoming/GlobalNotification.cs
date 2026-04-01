namespace AlloyClient.Networking.Packets.Incoming;

public class GlobalNotification : IncomingPacket<GlobalNotification> {
    public string Text;
    public int Type;

    public override PacketId PacketId => PacketId.GlobalNotification;

    public override void Reset() {
        Type = 0;
        Text = null;
    }

    public override void Read(NetworkReader reader) {
        Type = reader.ReadInt32();
        Text = reader.ReadUTF();
    }

    public override void Handle() {
        switch (Text) {
            case "yellow": break;
            case "red": break;
            case "green": break;
            case "purple": break;
            case "showKeyUI": break;
            case "giftChestOccupied": break;
            case "giftChestEmpty": break;
            case "beginnersPackage": break;
        }
    }

    public override string ToString() {
        return $"Type: {Type}, Text: {Text}";
    }
}