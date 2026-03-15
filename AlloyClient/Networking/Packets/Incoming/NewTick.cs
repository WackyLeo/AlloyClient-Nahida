using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Utils;
using Common;
using System;

namespace AlloyClient.Networking.Packets.Incoming;

public class NewTick : IncomingPacket<NewTick> {
    
    public override PacketId PacketId => PacketId.NewTick;
    
    private static ObjectStats[] _statsBuffer = new ObjectStats[256];
    public int ObjectStatsCount;
    public int TickTime;
    public int TickId;
    public ObjectStats[] ObjectStats => _statsBuffer;

    public override void Reset() {
        ObjectStatsCount = 0;
    }

    public override void Read(NetworkReader reader) {

        TickId = reader.ReadInt32();
        TickTime = reader.ReadInt32();


        Structs.DataObjects.ObjectStats.StatsPoolIndex = 0;
      
        ObjectStatsCount = reader.ReadInt16();
        EnsureCapacity(ref _statsBuffer, ObjectStatsCount);
        for (int i = 0; i < ObjectStatsCount; i++)
            _statsBuffer[i].Read(reader);
    }

    private static void EnsureCapacity<T>(ref T[] array, int needed) {
        if (array.Length < needed)
            array = new T[needed * 2];
    }

    public override void Handle() {
        var isLocalPlayer = false;

        if (Map.LocalPlayer != null)
        {
            var move = Move.CreatePacket();
            move.ObjectId = Map.LocalPlayer.ObjectId;
            move.TickId = TickId;
            move.Time = (int)Main.GameTime.TotalMs;
            move.NewPosition = new Position
            {
                X = Map.LocalPlayer.Position.X,
                Y = Map.LocalPlayer.Position.Y
            };
            move.Records = Array.Empty<TimedPosition>();
            Client.QueuePacket(move);
            
            Map.FlushEnemyHits();
            Map.FlushPlayerHits();
        }

        for (int i = 0; i < ObjectStatsCount; i++)
            ProcessObjectStats(_statsBuffer[i], isLocalPlayer);
    }

    private void ProcessObjectStats(ObjectStats stats, bool isPlayer) {
        if (!Map.Entities.TryGetValue(stats.Id, out var en)) {
            Logger.Warn($"[NewTick] Unable to lookup id: {stats.Id}");
            return;
        }
        en.UpdateStats(Structs.DataObjects.ObjectStats.StatsPool, stats.StatOffset, stats.StatCount);

        en.OnTickPosition(stats.Position.X, stats.Position.Y, 0, 0, isPlayer);
    }

    public override string ToString() {
        return $"ObjectStats: {ObjectStats}";
    }
}