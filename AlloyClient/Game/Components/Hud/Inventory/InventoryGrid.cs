using System;
using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Ui.Components.Scrollbars;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Game.Components.Hud.Inventory;

public sealed class InventoryGrid : Container 
{

    private const int NumSlots = 8;
    private const int ItemTileSize = 48;
    private const int ConsumableSlotHeight = 26;
    private const int ConsumableSlotWidth = 26;
    private const int Padding = 8;
    private const int Spacing = 5;
    private const int IconSize = 28;

    private static readonly CutEdges[] Cuts = 
        [
            CutEdges.TopLeft, 
            CutEdges.None, 
            CutEdges.None, 
            CutEdges.TopRight, 
            CutEdges.BottomLeft, 
            CutEdges.None, 
            CutEdges.None, 
            CutEdges.BottomRight
        ];

    private Container _tileVisibleContainer;
    private Container _tileScrollContainer;
    
    private readonly ItemTile[] _tiles;
    private int _totalSlots = NumSlots;

    private readonly Entity _owner;

    private readonly int _offset;   // Offset for where the inventory grid starts from the entity's actual Equipment

    private readonly int _height;

    private readonly bool _interactive;

    private bool _backpack;
    
    private readonly SimpleText _hpText;
    private readonly SimpleText _mpText;
    private readonly CutEdgeRect _hpSlot;
    private readonly CutEdgeRect _hpSlotOutline;
    private readonly CutEdgeRect _mpSlot;
    private readonly CutEdgeRect _mpSlotOutline;
    
    private VerticalScrollBar _scrollBar;


    public InventoryGrid(Entity owner, int offset, bool oneWay = false, int extraSlots = 0)
    {
        _totalSlots = NumSlots + extraSlots;
        _tiles = new ItemTile[_totalSlots];
        _owner = owner;
        _offset = offset;
        _interactive = owner == Map.LocalPlayer || owner.Properties.Container;
        _height = (ItemTileSize * 2) + ConsumableSlotHeight + (Padding * 2) + (Spacing * 2);
        var visibleHeight = (ItemTileSize * 2) + Spacing;
        
        if (owner == Map.LocalPlayer)
        {
            var bg = new CutEdgeRect(new CutEdgeConfig
            {
                Width = 223, 
                Height = _height, 
                CutX = 6, 
                CutY = 6, 
                Cuts = CutEdges.All, 
                Color = 0x242222
            });
            AddChild(bg);
            
            _tileVisibleContainer = new Container(new ContainerConfig
            {
                Y = Padding,
                Width = (ItemTileSize * 4) + (Spacing * 3) + (Padding * 2), 
                Height = visibleHeight, 
                EnableClip = true
            });
            AddChild(_tileVisibleContainer);
            
            _hpSlotOutline = new CutEdgeRect(new CutEdgeConfig
            {
                Width = 101, 
                Height = 26, 
                CutX = 4, 
                CutY = 4, 
                Cuts = CutEdges.Left, 
                Color = 0x454545
            });
            _hpSlotOutline.Y += 152 - 38;
            _hpSlotOutline.X = 8;
            AddChild(_hpSlotOutline);

            _hpSlot  = new CutEdgeRect(new CutEdgeConfig
            {
                Width = 95, 
                Height = 20, 
                CutX = 4, 
                CutY = 4, 
                Cuts = CutEdges.Left, 
                Color = 0x242222
            });
            _hpSlot .Y += 152 - 38 + 3;
            _hpSlot .X = 8 + 3;
            AddChild(_hpSlot );

            _mpSlotOutline = new CutEdgeRect(new CutEdgeConfig {
                Width = 101,
                Height = 26,
                CutX = 4,
                CutY = 4,
                Cuts = CutEdges.Right,
                Color = 0x454545
            });
            _mpSlotOutline.Y += 152 - 38;
            _mpSlotOutline.X = _hpSlotOutline.X + 100 + 6;
            AddChild(_mpSlotOutline);

            _mpSlot  = new CutEdgeRect(new CutEdgeConfig
            {
                Width = 95, 
                Height = 20, 
                CutX = 4, 
                CutY = 4, 
                Cuts = CutEdges.Right, 
                Color = 0x242222
            });
            _mpSlot .Y += 152 - 38 + 3;
            _mpSlot .X = _hpSlotOutline.X + 100 + 6 + 3;
            AddChild(_mpSlot );
            
            var hpIcon =  new ObjectRect(new ObjectRectConfig 
            {
                X = _hpSlotOutline.X + (_hpSlotOutline.Width / 2) - (IconSize/2) - 10,
                Y = _hpSlotOutline.Y + (_hpSlotOutline.Height /2) - (IconSize/2),
                Width = IconSize,
                Height = IconSize,
                Texture = TextureHelper.FromGameAtlas(2594),
                Anchor = UiAnchor.LeftTop
            });
            AddChild(hpIcon);
            
            var mpIcon =  new ObjectRect(new ObjectRectConfig 
            {
                X = _mpSlotOutline.X + (_mpSlotOutline.Width / 2) - (IconSize/2) - 10,
                Y = _mpSlotOutline.Y + (_mpSlotOutline.Height /2) - (IconSize/2),
                Width = IconSize,
                Height = IconSize,
                Texture = TextureHelper.FromGameAtlas(2595),
                Anchor = UiAnchor.LeftTop
            });
            AddChild(mpIcon);
            
            _hpText = new SimpleText(new TextConfig 
            {
                FontSize = 20,
                Color = 0xFFFFFFFFu,
                OutlineColor = 0xFF000000u,
                OutlineThickness = 0.5f,
                FontType = FontType.Bold,
                Anchor = UiAnchor.LeftTop,
            });
            _hpText.X = hpIcon.X + 27;
            _hpText.Y = _hpSlotOutline.Y + (_hpSlotOutline.Height /2) - (9);
            AddChild(_hpText);

            _mpText = new SimpleText(new TextConfig 
            {
                FontSize = 20,
                Color = 0xFFFFFFFFu,
                OutlineColor = 0xFF000000u,
                OutlineThickness = 0.5f,
                FontType = FontType.Bold,
                Anchor = UiAnchor.LeftTop,
            });
            _mpText.X = mpIcon.X + 27;
            _mpText.Y = _mpSlotOutline.Y + (_mpSlotOutline.Height /2) - (9);
            AddChild(_mpText);
            
        }
        
        _owner.InventoryUpdate.Add(OnInventoryChange);
        
        _tileScrollContainer = new Container();
        _tileVisibleContainer.AddChild(_tileScrollContainer);
        
        for (var i = 0; i < _totalSlots; i++)
        {
            var slot = new ItemTile(owner, (byte)(i + offset), _interactive, Cuts[2], oneWay, tileSize: ItemTileSize);
            slot.SetTileNumber(i + 1);
            slot.X = i % 4 * (ItemTileSize + Spacing) + Padding;
            slot.Y = i / 4 * (ItemTileSize + Spacing);
            _tileScrollContainer.AddChild(slot);
            _tiles[i] = slot;
        }
        
        var numRows = _totalSlots / 4;
        var totalContentHeight = ((numRows - 1) * Spacing) + (numRows * ItemTileSize);
        
        if (_totalSlots > 8) 
        {
            _scrollBar = new VerticalScrollBar(_tileVisibleContainer, new VerticalScrollBarConfig 
            {
                X = _tileVisibleContainer.Width - 5,
                Y = Padding,
                Width = 6,
                Height = visibleHeight,
                TotalContentHeight = totalContentHeight,
                VisibleContentHeight = visibleHeight,
                OnValueChanged = value => {
                    _tileScrollContainer.Y = -value;
                },
                ScrollStep = 10
            });
            AddChild(_scrollBar);
        }
        
        if (owner == Map.LocalPlayer)
        {
            _hpSlot.MouseEnabled = true;
            _hpSlotOutline.MouseEnabled = true;

            _mpSlot.MouseEnabled = true;
            _mpSlotOutline.MouseEnabled = true;

            _hpSlot.AddEventListener(MouseEvent.LeftClick, UseHpPot);
            _hpSlotOutline.AddEventListener(MouseEvent.LeftClick, UseHpPot);

            _mpSlot.AddEventListener(MouseEvent.LeftClick, UseMpPot);
            _mpSlotOutline.AddEventListener(MouseEvent.LeftClick, UseMpPot);
        }
        
        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
        UpdatePotions();
    }
    
    
    private void OnAddedToStage() 
    {
        AddEventListener(Event.EnterFrame, OnEnterFrame);
    }

    private void OnRemovedFromStage() 
    {
        RemoveEventListener(Event.EnterFrame, OnEnterFrame);
        _owner.InventoryUpdate.Remove(OnInventoryChange);
    }

    private void OnInventoryChange(int slot) 
    {
        if (!Visible) //Unsure how reliable this is, its to stop issues with the Backpack & Inventory trying to update when hidden
        {
            return;
        }

        if (slot < _offset || slot >= _offset + _totalSlots) return;
        _tiles[slot - _offset].SetItem(_owner.Equipment[slot]);
    }
    
    private void UseHpPot() 
    {
        if (_owner is not Player player) return;
        if (player.HealthStackCount <= 0) return;
        if(player.Hp >= player.MaxHp) return;
        UsePotion(254, ItemConstants.HealthPotion);
    }

    private void UseMpPot() 
    {
        if (_owner is not Player player) return;
        if (player.MagicStackCount <= 0) return;
        if(player.Mp >= player.MaxMp) return;
        UsePotion(255, ItemConstants.MagicPotion);
    }
    
    public void UpdatePotions() 
    {
        if (_owner is not Player player) return;
        if (_hpText == null || _mpText == null) return;

        int hp = player.HealthStackCount;
        int mp = player.MagicStackCount;

        _hpText.SetText(hp.ToString());
        _mpText.SetText(mp.ToString());

        _hpText.SetColor(GetGradientColor(hp));
        _mpText.SetColor(GetGradientColor(mp));
        
        _hpText.Visible = true;
        _mpText.Visible = true;

        _hpSlot.Visible = hp <= 0;
        _mpSlot.Visible = mp <= 0;
    }
    
    uint GetGradientColor(int count) 
    {
        return count switch 
        {
            >= 6 => 0x00FF00,
            5 => 0x55FF00,
            4 => 0xAAFF00,
            3 => 0xFFFF00,
            2 => 0xFFA500,
            1 => 0xFF4500,
            _ => 0xFF0000
        };
    }
    
    private void UsePotion(byte slotId, ushort objectType) 
    {
        var player = Map.LocalPlayer;
        if (player == null) return;

        var packet = UseItem.CreatePacket();

        packet.Time = (int)Map.LastGameTime.TotalMs;

        packet.SlotObject.ObjectId = player.ObjectId;
        packet.SlotObject.SlotId = slotId;
        packet.SlotObject.ObjectType = objectType;

        packet.ItemUsePos.X = player.Position.X;
        packet.ItemUsePos.Y = player.Position.Y;

        packet.UseType = (byte)UseType.START_USE;
        packet.ProjId = player.ReadServerId();

        Client.QueuePacket(packet);
    }
    
    private void OnEnterFrame() 
    {
        UpdatePotions();
    }

    public ItemTile GetTileAtIndex(int index) 
    {
        return _tiles[index];
    }
}

