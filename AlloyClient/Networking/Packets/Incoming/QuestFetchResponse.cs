using System.Collections.Generic;
using AlloyClient.Game;
using AlloyClient.Game.Components;

namespace AlloyClient.Networking.Packets.Incoming;

public class QuestFetchResponse : IncomingPacket<QuestFetchResponse> {
    public List<QuestData> Quests { get; set; } = new();

    public override PacketId PacketId => PacketId.QuestFetchResponse;

    public override void Reset() {
        Quests.Clear();
    }

    public override void Read(NetworkReader reader) {
        var questLength = reader.ReadInt16();
        for (var i = 0; i < questLength; i++) Quests.Add(new QuestData(reader));
    }

    public override void Handle() {
        Map.DailyQuests = new List<QuestData>(Quests);
        if (GameSprite.DailyQuestWindow != null) GameSprite.DailyQuestWindow.LoadQuests(Map.DailyQuests);
    }

    public override string ToString() {
        return $"QuestFetchResponse: {Quests.Count} Quests";
    }
}