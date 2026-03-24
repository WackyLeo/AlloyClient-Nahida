using System;
using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using Common;

namespace AlloyClient.Networking.Packets.Incoming;

public class NewTick : IncomingPacket<NewTick> {
    private static ObjectStats[] _statsBuffer = new ObjectStats[256];
    public int ObjectStatsCount;
    public int TickId;
    public int TickTime;

    public override PacketId PacketId => PacketId.NewTick;
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
        for (var i = 0; i < ObjectStatsCount; i++)
            _statsBuffer[i].Read(reader);
    }

    private static void EnsureCapacity<T>(ref T[] array, int needed) {
        if (array.Length < needed)
            array = new T[needed * 2];
    }

    public override void Handle() {
        Map.LastTickId = TickId;
        var now = Main.GameTime.TotalMs;

        if (Map.LocalPlayer != null) {
            var move = Move.CreatePacket();
            move.ObjectId = Map.LocalPlayer.ObjectId;
            move.TickId = TickId;
            move.Time = (int)now;
            move.NewPosition = new Position {
                X = Map.LocalPlayer.Position.X,
                Y = Map.LocalPlayer.Position.Y
            };
            move.Records = Array.Empty<TimedPosition>();
            Client.QueuePacket(move);

            Map.FlushEnemyHits();
            Map.FlushPlayerHits();
        }

        for (var i = 0; i < ObjectStatsCount; i++) {
            var stats = _statsBuffer[i];
            var isMe = Map.LocalPlayer != null && stats.Id == Map.LocalPlayer.ObjectId;
            ProcessObjectStats(stats, isMe, now);
        }
    }

    private void ProcessObjectStats(ObjectStats stats, bool isPlayer, double now) {
        if (!Map.Entities.TryGetValue(stats.Id, out var en)) {
            Logger.Warn($"[NewTick] Unable to lookup id pos {stats.Position}: {stats.Id}");
            return;
        }

        en.UpdateStats(Structs.DataObjects.ObjectStats.StatsPool, stats.StatOffset, stats.StatCount);
        en.OnTickPosition(stats.Position.X, stats.Position.Y, TickTime, TickId, isPlayer, now);
    }

    public override string ToString() {
        return $"ObjectStats: {ObjectStats}";
    }
}