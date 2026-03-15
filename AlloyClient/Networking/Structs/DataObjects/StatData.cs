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
        //Logger.Debug($"[StatData] Reading stat type=0x{(byte)Type:X2} ({Type})");

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
                return true;
            default:
                return false;
        }
    }

    public override string ToString() {
        return $"Type: {Type}, Value: {Value}, Text: {Text}, DataObject: {DataObject}";
    }
}