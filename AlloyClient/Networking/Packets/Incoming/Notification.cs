using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Ui.Character;

namespace AlloyClient.Networking.Packets.Incoming;

public class Notification : IncomingPacket<Notification>
{
    public int ObjectId;
    public string Message;
    public ARGB Color;

    public override PacketId PacketId => PacketId.Notification;

    public override void Reset()
    {
        ObjectId = 0;
        Message = null;
        Color.Reset();
    }

    public override void Read(NetworkReader reader)
    {
        ObjectId = reader.ReadInt32();
        Message = reader.ReadUTF();
        Color.Read(reader);
    }

    public override void Handle()
    {
        if (string.IsNullOrEmpty(Message))
            return;

        if (!Map.Entities.TryGetValue(ObjectId, out var en))
            return;

        // AddStatusText takes 0xRRGGBB, so drop the alpha byte
        var rgb = ((uint)Color.R << 16) | ((uint)Color.G << 8) | Color.B;
        
        NotificationLayer.AddStatusText(en, Message, rgb, 2000, 0);
    }

    public override string ToString()
    {
        return $"ObjectId: {ObjectId}, Message: {Message}, Color: {Color}";
    }
}