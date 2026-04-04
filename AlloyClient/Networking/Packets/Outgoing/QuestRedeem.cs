using System.Collections.Generic;
using AlloyClient.Networking.Structs.DataObjects;

namespace AlloyClient.Networking.Packets.Outgoing;

public class QuestRedeem : OutgoingPacket<QuestRedeem> {
    public int Item;

    public string QuestId;

    public List<ObjectSlot> Slots = new();

    public override PacketId PacketId => PacketId.QuestRedeem;

    public override void Reset() {
        QuestId = "";
        Item = -1;
        Slots.Clear();
    }

    public override void Write(NetworkWriter writer) {
        writer.Write(QuestId);
        writer.Write(Item);
        writer.Write((short)Slots.Count);
        foreach (var slot in Slots) slot.Write(writer);
    }

    public override string ToString() {
        return $"QuestID: {QuestId},  Item: {Item},   Slots: {Slots.Count}";
    }
}