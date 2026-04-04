using System.Collections.Generic;
using AlloyClient.Networking;

public class QuestData {
    public int Category;
    public bool Completed;
    public string Description;
    public string Id;
    public bool ItemOfChoice;
    public string Name;
    public int Priority;
    public List<int> Requirements = new();
    public List<int> Rewards = new();

    public QuestData(NetworkReader reader) {
        Reset();

        Id = reader.ReadUTF();
        Name = reader.ReadUTF();
        Description = reader.ReadUTF();
        Category = reader.ReadInt32();

        var amountOfReq = reader.ReadInt16();
        for (var i = 0; i < amountOfReq; i++) {
            var requirment = reader.ReadInt32();
            Requirements.Add(requirment);
        }

        var amountOfRewards = reader.ReadInt16();
        for (var i = 0; i < amountOfRewards; i++) {
            var reward = reader.ReadInt32();
            Rewards.Add(reward);
        }

        Completed = reader.ReadBoolean();
        ItemOfChoice = reader.ReadBoolean();
        Priority = reader.ReadInt32();
    }

    public void Reset() {
        Category = 0;
        Completed = false;
        Description = "";
        Id = "";
        ItemOfChoice = false;
        Name = "";
        Priority = 0;
        Requirements.Clear();
        Rewards.Clear();
    }
}