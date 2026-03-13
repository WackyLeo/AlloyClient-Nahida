using AlloyClient.Networking.Enums;
using Common;

namespace AlloyClient.Networking.Structs.DataObjects;

public struct StatData : IDataObject {
    public StatsType Type;
    public int Value;
    public float FloatValue;
    public string Text;

    public IDataObject DataObject;

    public void Reset() {
        Type = default;
        Value = 0;
        Text = null;
        DataObject = null;
    }

    public void Read(NetworkReader reader)
    {
        Type = (StatsType)reader.ReadByte();
        Logger.Debug($"[StatData] Reading stat type=0x{(byte)Type:X2} ({Type})");

        if (IsStringStat(Type))
        {
            Text = reader.ReadUTF();
            Value = 0;
        }
        else
        {
            Value = reader.ReadInt32();
            Text = null;
        }
    }

    public void Write(NetworkWriter writer) {
        writer.Write((byte)Type);

        if (IsStringStat(Type)) {
            writer.Write(Text);
        }
        else {
            writer.Write(Value);
        }
    }

    public static bool IsStringStat(StatsType type)
    {
        switch (type)
        {
            case StatsType.Name:
            case StatsType.Guild:
            case StatsType.AccountId:
            case StatsType.OwnerAccountId:
            /* case StatsType.Inventoory0:
            case StatsType.Inventory1:
            case StatsType.Inventory2:
            case StatsType.Inventory3:
            case StatsType.Inventory4:
            case StatsType.Inventory5:
            case StatsType.Inventory6:
            case StatsType.Inventory7:
            case StatsType.Inventory8:
            case StatsType.Inventory9:
            case StatsType.Inventory10:
            case StatsType.Inventory11:
            case StatsType.BackPack0:
            case StatsType.BackPack1:
            case StatsType.BackPack2:
            case StatsType.BackPack3:
            case StatsType.BackPack4:
            case StatsType.BackPack5:
            case StatsType.BackPack6:
            case StatsType.BackPack7: */
                return true;
            default:
                return false;
        }
    }

    public override string ToString() {
        return $"Type: {Type}, Value: {Value}, Text: {Text}, DataObject: {DataObject}";
    }
}