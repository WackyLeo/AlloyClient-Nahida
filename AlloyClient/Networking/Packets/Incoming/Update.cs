using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Networking.Packets.Incoming;

public class Update : IncomingPacket<Update> {

    private static TileData[] _tilesBuffer = new TileData[256];
    private static ObjectDef[] _newObjsBuffer = new ObjectDef[256];
    private static int[] _dropsBuffer = new int[256];
    public int DropCount;
    public int NewObjCount;

    public int TileCount;

    public override PacketId PacketId => PacketId.Update;

    public TileData[] Tiles => _tilesBuffer;
    public ObjectDef[] NewObjs => _newObjsBuffer;
    public int[] Drops => _dropsBuffer;

    public override void Reset() {
        TileCount = NewObjCount = DropCount = 0;
    }

    public override void Read(NetworkReader reader) {
        ObjectDef.StatsPoolIndex = 0;

        TileCount = reader.ReadInt16();
        EnsureCapacity(ref _tilesBuffer, TileCount);
        for (var i = 0; i < TileCount; i++)
            _tilesBuffer[i].Read(reader);

        //Logger.Debug($"[Update] After tiles: tileCount={TileCount} pos={reader.BaseStream.Position}");

        NewObjCount = reader.ReadInt16();
        EnsureCapacity(ref _newObjsBuffer, NewObjCount);
        for (var i = 0; i < NewObjCount; i++)
            _newObjsBuffer[i].Read(reader);

        DropCount = reader.ReadInt16();
        EnsureCapacity(ref _dropsBuffer, DropCount);
        for (var i = 0; i < DropCount; i++)
            _dropsBuffer[i] = reader.ReadInt32();
    }

    private static void EnsureCapacity<T>(ref T[] array, int needed) {
        if (array.Length < needed)
            array = new T[needed * 2]; // double to avoid frequent resizes
    }

    public override void Handle() {
        Client.QueuePacket(UpdateAck.CreatePacket());

        for (var i = 0; i < TileCount; i++)
            Map.SetTileData(Tiles[i].X, Tiles[i].Y, Tiles[i].Type);

        for (var i = 0; i < DropCount; i++)
            Map.RemoveEntity(Drops[i]);

        for (var i = 0; i < NewObjCount; i++) {
            var newObj = NewObjs[i];
            Entity entity;

            if (!ObjectLibrary.TypeToObjectProps.TryGetValue(newObj.ObjectType, out var props)) {
                Logger.Warn($"[Update] Unable to parse props for id: {newObj.ObjectType:x4}");
                props = ObjectLibrary.TypeToObjectProps[0x600]; //Pirate Placeholder
            }

            /*if (true || props.Class == "Wall" || props.Class == "ConnectedWall" || props.Class == "CaveWall") {
                var tile = Map.GetTile((int)newObj.Position.X, (int)newObj.Position.Y);
                if (true || tile == null || tile.Type == Const.DefaultTile) {
                    Map.SetTileData((int)newObj.Position.X, (int)newObj.Position.Y, 0xfa);
                }
            } */

            if (props.IsPlayer) {
                entity = new Player();
            }
            else if (props.Class == "Merchant" || props.ObjectType == 0x01ca) {
                entity = new Merchant();
            }
            else {
                entity = new Entity();
            }

            entity.Properties = props;
            entity.SetObjectId(newObj.Id);
            entity.SetType(newObj.ObjectType);

            if (!Map.AddEntity(entity, newObj.Position)) {
                if (Map.TryGetEntity(newObj.Id, out var existing)) {
                    existing.SetPos(newObj.Position.X, newObj.Position.Y);
                    existing.UpdateStats(ObjectDef.StatsPool, newObj.StatOffset, newObj.StatCount);
                    existing.TickPosition = new Vector2(newObj.Position.X, newObj.Position.Y);
                    existing.PositionAtTick = new Vector2(newObj.Position.X, newObj.Position.Y);
                    existing.LastTickId = Map.LastTickId;
                    existing.LastTickUpdateTime = Map.LastGameTime.TotalMs;
                    existing.MovementVector = Vector2.Zero;
                }

                continue;
            }

            entity.SetPos(newObj.Position.X, newObj.Position.Y);
            entity.UpdateStats(ObjectDef.StatsPool, newObj.StatOffset, newObj.StatCount);

            entity.TickPosition = new Vector2(newObj.Position.X, newObj.Position.Y);
            entity.PositionAtTick = new Vector2(newObj.Position.X, newObj.Position.Y);
            entity.LastTickId = Map.LastTickId;
            entity.LastTickUpdateTime = Map.LastGameTime.TotalMs;
            entity.MovementVector = Vector2.Zero;

            if (newObj.Id == Map.LocalPlayerId) {
                Map.OnLocalPlayerCreated(entity);
            }
        }
    }

    public override string ToString() {
        return $"Tiles: {Tiles}, NewObjs: {NewObjs}, Drops: {Drops}";
    }
}
