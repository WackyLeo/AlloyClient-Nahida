using System;
using System.Security.Cryptography;
using System.Text;

namespace AlloyClient.Networking.Packets.Outgoing;

public class Hello : OutgoingPacket<Hello>
{
    public string BuildVersion;
    public int GameId;
    public string Guid;
    public string Password;
    public string MapJSON;
    public string Secret;
    public int KeyTime;
    public byte[] Key;
    public int Key2;

    public override PacketId PacketId => PacketId.Hello;

    public override void Reset()
    {
        BuildVersion = string.Empty;
        GameId = 0;
        Guid = string.Empty;
        Password = string.Empty;
        MapJSON = string.Empty;
        Secret = string.Empty;
        KeyTime = -1;
        Key = Array.Empty<byte>();
        Key2 = 0;
    }

    public override void Write(NetworkWriter writer)
    {
        writer.WriteUTF(BuildVersion);
        writer.Write(GameId);
        writer.WriteUTF(Rsa.EncryptPublic(Guid));
        writer.WriteUTF(Rsa.EncryptPublic(Password));
        writer.WriteUTF(Rsa.EncryptPublic(Secret));
        writer.Write(KeyTime);
        writer.Write((short)Key.Length);
        writer.BaseStream.Write(Key, 0, Key.Length);
        writer.WriteUTF(Key2.ToString());
        writer.Write32UTF(MapJSON);
    }

    public override string ToString()
    {
        return $"BuildVersion: {BuildVersion}, GameId: {GameId}, GUID: {Guid}, Password: {Password}, MapJSON: {MapJSON}, KeyTime: {KeyTime}, Key2: {Key2}";
    }
}