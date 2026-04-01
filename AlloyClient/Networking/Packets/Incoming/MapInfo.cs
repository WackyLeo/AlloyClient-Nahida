using System;
using AlloyClient.Data;
using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;

namespace AlloyClient.Networking.Packets.Incoming;

public class MapInfo : IncomingPacket<MapInfo> {
    public bool AllowPlayerTeleport;
    public int Background;
    public int Difficulty;
    public string DisplayName;
    public int Height;
    public string Name;
    public uint Seed;
    public bool ShowDisplays;
    public int Width;

    public override PacketId PacketId => PacketId.MapInfo;

    public override void Reset() {
        Width = 0;
        Height = 0;
        Name = null;
        DisplayName = null;
        Difficulty = 0;
        Seed = 0;
        Background = 0;
        AllowPlayerTeleport = false;
        ShowDisplays = false;
    }

    public override void Read(NetworkReader reader) {
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        Name = reader.ReadUTF();
        DisplayName = reader.ReadUTF();
        Difficulty = reader.ReadInt32();
        Seed = reader.ReadUInt32();
        Background = reader.ReadInt32();
        AllowPlayerTeleport = reader.ReadBoolean();
        ShowDisplays = reader.ReadBoolean();
    }

    public override void Handle() {
        Map.Reset();

        Map.InitMap(Width, Height, Name, DisplayName, Difficulty, Seed, Background,
            AllowPlayerTeleport, ShowDisplays);

        LoadOrCreate();

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();

        Client.IsReconnecting = false;
    }

    private static void LoadOrCreate() {
        if (GlobalData.CharacterType > 0) {
            var create = Create.CreatePacket();
            create.ClassType = GlobalData.CharacterType;
            create.SkinType = 0;
            Client.QueuePacket(create);

            GlobalData.CharacterType = 0;
            return;
        }

        var load = Load.CreatePacket();
        load.CharId = GlobalData.SelectedCharacterId;
        Client.QueuePacket(load);
    }

    public override string ToString() {
        return
            $"Width: {Width}, Height: {Height}, Name: {Name}, DisplayName: {DisplayName}, Difficulty: {Difficulty}, Seed: {Seed}, Background: {Background}, AllowPlayerTeleport: {AllowPlayerTeleport}, ShowDisplays: {ShowDisplays}";
    }
}