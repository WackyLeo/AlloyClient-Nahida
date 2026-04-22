using System;
using System.Collections.Generic;
using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Utils;

namespace AlloyClient.Game;

public static class PartyData {

    public const int MaxVisibleMembers = 10;

    private const int MaxDistance = 50 * 50;

    public static readonly HashSet<int> LockedPlayers = [];
    
    public static readonly HashSet<int> IgnoredPlayers = [];

    private static double _lastUpdateTime;

    private static readonly PartyInfo[] Members = new PartyInfo[250];

    public static readonly ArraySegment<PartyInfo> PartyMembers = new(Members, 0, MaxVisibleMembers);

    private static readonly PartyComparison PartyComparer = new();

    public static void Clear() {
        LockedPlayers.Clear();
        IgnoredPlayers.Clear();
        Array.Clear(Members);
    }

    public static void Update(double time) {
        if (time < _lastUpdateTime + 500)
            return;
        
        if (Map.LocalPlayer == null)
            return;
        
        _lastUpdateTime = time;
        
        Array.Clear(Members);

        var localPosition = Map.LocalPlayer.Position;
        var i = 0;
        
        foreach (var player in Map.Players.Values) {
            player.Locked = LockedPlayers.Contains(player.AccountId);
            player.Ignored = IgnoredPlayers.Contains(player.AccountId);

            var dist = MathUtils.GetDistanceSquared(localPosition, player.Position);
            if (dist < MaxDistance || player.Locked) {
                Members[i] = new PartyInfo(player, player.Locked, dist, player.ObjectId);
                i++;
                
                if (i >= Members.Length)
                    break;
            }
        }
        
        Array.Sort(Members, 0, i, PartyComparer);
    }

    public static void SetData(int id, int[] list) {
        var set = id == 0 ? LockedPlayers : IgnoredPlayers;
        set.UnionWith(list);
    }

    public static void LockPlayer(Player player) {
        player.Locked = true;
        _lastUpdateTime = int.MinValue;
        LockedPlayers.Add(player.AccountId);

        var pkt = EditAccountList.CreatePacket();
        pkt.AccountListId = 0;
        pkt.Add = true;
        pkt.ObjectId = player.ObjectId;
        
        Client.QueuePacket(pkt);
    }
    
    public static void UnlockPlayer(Player player) {
        player.Locked = false;
        _lastUpdateTime = int.MinValue;
        LockedPlayers.Remove(player.AccountId);

        var pkt = EditAccountList.CreatePacket();
        pkt.AccountListId = 0;
        pkt.Add = false;
        pkt.ObjectId = player.ObjectId;
        
        Client.QueuePacket(pkt);
    }
    
    public static void IgnorePlayer(Player player) {
        player.Ignored = true;
        _lastUpdateTime = int.MinValue;
        IgnoredPlayers.Add(player.AccountId);

        var pkt = EditAccountList.CreatePacket();
        pkt.AccountListId = 1;
        pkt.Add = true;
        pkt.ObjectId = player.ObjectId;
        
        Client.QueuePacket(pkt);
    }
    
    public static void UnignorePlayer(Player player) {
        player.Ignored = false;
        _lastUpdateTime = int.MinValue;
        IgnoredPlayers.Remove(player.AccountId);

        var pkt = EditAccountList.CreatePacket();
        pkt.AccountListId = 1;
        pkt.Add = false;
        pkt.ObjectId = player.ObjectId;
        
        Client.QueuePacket(pkt);
    }

    public sealed record PartyInfo(Player Player, bool Locked = false, float Dist = float.MaxValue, int ObjectId = int.MaxValue);
    
    private class PartyComparison : IComparer<PartyInfo> {
        public int Compare(PartyInfo self, PartyInfo other) {
            if (ReferenceEquals(self, other)) return 0;
            if (self is null) return 1;
            if (other is null) return -1;

            return (
                !self.Locked,
                self.Dist,
                self.ObjectId
            ).CompareTo((
                !other.Locked,
                other.Dist,
                other.ObjectId
            ));
        }
    }   
}