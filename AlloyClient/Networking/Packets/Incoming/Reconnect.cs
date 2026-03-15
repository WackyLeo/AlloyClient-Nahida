using System;
using System.Threading.Tasks;
using AlloyClient.Data;
using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.State;
using Common;

namespace AlloyClient.Networking.Packets.Incoming;

public class Reconnect : IncomingPacket<Reconnect> {
    public string Name;
    public string Host;
    public int Port;
    public int GameId;
    public bool IsFromArena;
    public int KeyTime;
    public byte[] Key;

    public override PacketId PacketId => PacketId.Reconnect;

    public override void Reset() {
        Name = null;
        Host = null;
        Port = 0;
        GameId = 0;
        IsFromArena = false;
        KeyTime = -1;
        Key = [];
    }

    public override void Read(NetworkReader reader) {
        Name = reader.ReadUTF();
        Host = reader.ReadUTF();
        Port = reader.ReadInt32();
        GameId = reader.ReadInt32();
        KeyTime = reader.ReadInt32();
        IsFromArena = reader.ReadBoolean();
        var keyLen = reader.ReadInt16();
        Key = keyLen > 0 ? reader.ReadBytes(keyLen) : [];
    }

    public override void Handle() {
        Logger.Info($"[Reconnect] GameId={GameId} KeyTime={KeyTime} KeyLen={Key?.Length ?? 0}");
        var gameId = GameId;
        var keyTime = KeyTime;
        var key = Key;
        Client.ReconnectToGame(gameId, keyTime, key);
    }

    public override string ToString() =>
        $"Name={Name} Host={Host} Port={Port} GameId={GameId} KeyTime={KeyTime} KeyLen={Key?.Length ?? 0}";
}