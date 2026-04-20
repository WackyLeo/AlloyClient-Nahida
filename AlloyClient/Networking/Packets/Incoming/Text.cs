using AlloyClient.Game;
using AlloyClient.Game.Components.Hud.Chat;
using AlloyClient.Ui.Chat;

namespace AlloyClient.Networking.Packets.Incoming;

public class Text : IncomingPacket<Text> {
    public string Name;
    public int ObjectId;
    public int NumStars;
    public int Admin;
    public byte BubbleTime;
    public string Recipient;
    public string Txt;
    public string CleanText;
    public int NameColor;
    public int TextColor;

    public override PacketId PacketId => PacketId.Text;

    public override void Reset() {
        Name = null;
        ObjectId = 0;
        NumStars = 0;
        Admin = 0;
        BubbleTime = 0;
        Recipient = null;
        Txt = null;
        CleanText = null;
        NameColor = 0;
        TextColor = 0;
    }

    public override void Read(NetworkReader reader) {
        Name = reader.ReadUTF();
        ObjectId = reader.ReadInt32();
        NumStars = reader.ReadInt32();
        Admin = reader.ReadInt32();
        BubbleTime = reader.ReadByte();
        Recipient = reader.ReadUTF();
        Txt = reader.ReadUTF();
        CleanText = reader.ReadUTF();
        NameColor = reader.ReadInt32();
        TextColor = reader.ReadInt32();
    }

    public override void Handle() {
        string finalText = string.IsNullOrEmpty(CleanText) ? Txt : CleanText;
        if (string.IsNullOrEmpty(finalText)) return;

        ChatBox.AddChatLine.Dispatch(new ChatBoxLineData(Main.GetTime(), Name, NumStars, Recipient, finalText, NameColor, TextColor));
            
        if (Map.Entities.TryGetValue(ObjectId, out var en)) {
            ChatLayer.QueueSpeech(new SpeechData(en, finalText, Recipient));
        }
    }

    public override string ToString() {
        return $"Name: {Name}, ObjectId: {ObjectId}, NumStars: {NumStars}, BubbleTime: {BubbleTime}, Recipient: {Recipient}, Txt: {Txt}";
    }
}