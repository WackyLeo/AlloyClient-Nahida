using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Display;
using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Sound;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Extra;
using AlloyClient.Utils;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Components.Hud.Inventory;

public sealed class ItemTile : Sprite {
    private readonly CutEdgeRect _background;
    private readonly uint _bgColor;
    private readonly ObjectRect _slotDetail;
    private readonly SimpleText _slotId;

    private readonly ObjectRect _sprite;
    private readonly SimpleText _tierText;

    public readonly bool ClassSensitive;

    public readonly bool Interactive;

    public readonly bool OneWay;

    public readonly Entity Owner;

    public readonly byte SlotId;

    public readonly byte SlotType;
    private bool _checkForDrag;
    private bool _dragging;

    private Vector2i _dragStart;

    private EquipmentToolTip _tooltip;

    public ItemDesc ItemDesc;

    public int Size = 50;

    public ItemTile(Entity owner, byte slotId, bool interactive, CutEdges cut, bool oneWay, byte slotType = 0,
                    int tileSize = 50, uint bgcolor = 0x545454, bool classSensitive = true) {
        Size = tileSize;
        Owner = owner;
        SlotId = slotId;
        SlotType = slotType;
        Interactive = interactive;
        OneWay = oneWay;
        _bgColor = bgcolor;
        ClassSensitive = classSensitive;

        _background = new CutEdgeRect(new CutEdgeConfig
            { Width = Size, Height = Size, CutX = 4, CutY = 4, Cuts = cut, Color = _bgColor });
        AddChild(_background);

        _slotDetail = new ObjectRect(new ObjectRectConfig {
            Texture = TextureHelper.FromGameAtlas(0x0096), Width = Size, Height = Size, OutlineEnabled = false,
            GlowEnabled = false
        });
        _slotDetail.Visible = false;
        _slotDetail.ColorTransformation = new ColorTransform(0, 0, 0, 1, 54, 54, 54, 0);
        _slotDetail.SetColorSecondary(0, 0);
        AddChild(_slotDetail);

        if (SlotType != 0) {
            _slotDetail.ChangeTexture(ItemConstants.GetSlot(SlotType));
            _slotDetail.Visible = true;
        }

        _slotId = new SimpleText(new TextConfig {
            Text = "", X = Size / 2, Y = Size / 2, FontSize = 32, FontType = FontType.Bold, Color = 0x363636,
            OutlineColor = 0x363636, Anchor = UiAnchor.Middle, Glow = 0
        });
        _slotId.Visible = false;
        AddChild(_slotId);

        if (Owner is Player && SlotType == 0) _slotId.Visible = true;

        _sprite = new ObjectRect(new ObjectRectConfig
            { Texture = TextureHelper.FromGameAtlas(0x0096), Width = Size, Height = Size });
        AddChild(_sprite);

        _tierText = new SimpleText(new TextConfig {
            FontSize = 16,
            FontType = FontType.Bold,
            Text = "",
            OutlineThickness = 0.5f,
            Glow = 1.5f,
            OutlineColor = 0x000000
        });

        _tierText.Visible = false;
        _tierText.SetAnchor(UiAnchor.RightBottom);
        _tierText.X = Size - 2;
        _tierText.Y = Size;
        AddChild(_tierText);

        if (Owner != null) SetItem(Owner.Equipment[SlotId]);

        _sprite.MouseEnabled = true;

        if (Interactive) {
            _sprite.AddEventListener(MouseEvent.LeftDown, OnMouseDown);
            _sprite.AddEventListener(MouseEvent.LeftUp, OnMouseUp);
            _sprite.AddEventListener(MouseEvent.DoubleClick, OnDoubleClick);
        }

        _sprite.AddEventListener(MouseEvent.MouseOver, OnMouseOver);
        _sprite.AddEventListener(MouseEvent.MouseOut, OnMouseOut);
        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    
    private void OnAddedToStage() {
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnRemovedFromStage() {
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
        Map.GameSprite.Stage?.RemoveEventListener(MouseEvent.LeftUp, CancelDragCheck);
        Map.GameSprite.Stage?.RemoveEventListener(MouseEvent.LeftUp, OnEndDrag);

        if (_tooltip != null) {
            TooltipManager.RemoveTooltip(_tooltip);
            _tooltip = null;
        }

        if (_dragging) {
            _sprite.EndDrag();
            _dragging = false;
        }

        _checkForDrag = false;
    }

    public void SetItem(ItemDesc itemDesc) {
        var isNewItem = itemDesc != null && itemDesc != ItemDesc;

        ItemDesc = itemDesc;
        if (ItemDesc != null && ItemDesc.ObjectType > 0) {
            _sprite.ChangeTexture(TextureHelper.FromGameAtlas(ItemDesc.ObjectType));
            if (ClassSensitive) _background.SetColor(IsUsableByPlayer(ItemDesc) ? _bgColor : 0x5C1D1Du);
            _slotDetail.Visible = false;
            _slotId.Visible = false;
            if (isNewItem && Owner is Player) {
                var soundToLoad = !string.IsNullOrEmpty(ItemDesc.OldSound) ? ItemDesc.OldSound : ItemDesc.Sound;
                if (!string.IsNullOrEmpty(soundToLoad)) _ = AudioCache.LoadSfxAsync(soundToLoad, CacheTier.Volatile);
            }
        } else {
            _sprite.ChangeTexture(TextureHelper.FromGameAtlas(0x0096));
            _background.SetColor(_bgColor);
            if (SlotType != 0) _slotDetail.Visible = true;
            if (Owner is Player && SlotType == 0) _slotId.Visible = true;
            if (_tooltip != null) TooltipManager.RemoveTooltip(_tooltip);
        }

        UpdateTierTag();
    }

    public void SetTileNumber(int slot) {
        _slotId.SetText($"{slot}");
    }

    public void SetDim(bool isDim) {
        _sprite.ColorTransformation = isDim ? Transforms.Dark : Transforms.Default;
    }

    private void UpdateTierTag() {
        if (ItemDesc == null || ItemDesc.Consumable || ItemDesc.SlotType == 10) {
            _tierText.Visible = false;
            return;
        }

        var color = 0xFFFFFFu;
        var effects = TextEffect.None;
        var tag = $"T{ItemDesc.Tier}";
        if (ItemDesc.TributePoints > 0) {
            if (ItemDesc.ST) {
                color = 0xFF9900;
                tag = "ST";
            } else if (ItemDesc.UnHoly) {
                color = 0x9E9859;
                tag = "UH";
            } else if (ItemDesc.Div) {
                color = 0xFCE53D;
                tag = "DIV";
            } else if (ItemDesc.Ferozium) {
                color = 0x52DCFF;
                tag = "FT" + ItemDesc.Tier;
                effects = TextEffect.Void;
            } else {
                tag = "UT";
                if (ItemDesc.TributePoints > 749999)
                    color = 0x00D287;
                else if (ItemDesc.TributePoints > 99999)
                    color = 0xB2001D;
                else
                    color = 9055202;
            }
        } else {
            if (ItemDesc.Tier == -1) {
                tag = "UT";
                color = 9055202;
            } else {
                tag = "T" + ItemDesc.Tier;
            }
        }

        _tierText.SetText(tag);
        _tierText.SetColor(color);
        _tierText.SetEffect(effects);
        _tierText.Visible = true;
    }

    private void OnMouseOver() {
        if (ItemDesc == null || _dragging) return;
        _tooltip = new EquipmentToolTip(ItemDesc);
        TooltipManager.AddTooltip(_tooltip);
    }

    private void OnMouseOut() {
        if (ItemDesc == null || _tooltip == null || _dragging) return;
        TooltipManager.RemoveTooltip(_tooltip);
        _tooltip = null;
    }

    private void OnMouseDown(MouseEvent args) {
        if (ItemDesc == null) return;

        _dragStart = GetRelativeMousePosition();
        _checkForDrag = true;

        Map.GameSprite.Stage?.AddEventListener(MouseEvent.LeftUp, CancelDragCheck);
    }

    private void OnMouseUp(MouseEvent args) {
        if (_dragging) return;

        if (args.ShiftKey) {
            if (ItemDesc == null) return;

            if (ItemDesc.SlotType == ItemConstants.PotionType || ItemDesc.Consumable)
                useItem((int)Map.LastGameTime.TotalMs, Owner.ObjectId, SlotId, ItemDesc.ObjectType, Owner.Position.X,
                    Owner.Position.Y, 1);
            return;
        }

        if (args.CtrlKey) {
            // todo: swap to backpack
        }
    }

    private void OnDoubleClick(MouseEvent args) {
        if (ItemDesc == null || ItemDesc.ObjectType == 0 || _dragging) return;

        if (ItemDesc.ObjectType == ItemConstants.HealthPotion || ItemDesc.ObjectType == ItemConstants.MagicPotion)
            if (AddToPotionStack())
                return;

        if (!(Owner is Player))
            EquipOrUseContainer();
        else
            EquipOrUseInventory();
    }

    private void EquipOrUseContainer() {
        var freeSlot = GetNextAvailableInventorySlot();
        if (freeSlot != -1)
            SwapItem(Map.LocalPlayer, (byte)freeSlot);
        else
            useItem((int)Map.LastGameTime.TotalMs, Owner.ObjectId, SlotId, ItemDesc.ObjectType,
                Map.LocalPlayer.Position.X, Map.LocalPlayer.Position.Y, 1);
    }

    private void EquipOrUseInventory() {
        if (ItemDesc.Consumable) {
            useItem((int)Map.LastGameTime.TotalMs, Owner.ObjectId, SlotId, ItemDesc.ObjectType,
                Map.LocalPlayer.Position.X, Map.LocalPlayer.Position.Y, 1);
            return;
        }

        if (SlotId < 4) {
            var freeSlot = GetNextAvailableInventorySlot();
            if (freeSlot != -1) SwapItem(Map.LocalPlayer, (byte)freeSlot);
        } else {
            var equipSlot = GetMatchingEquipmentSlot();
            if (equipSlot != -1)
                SwapItem(Map.LocalPlayer, (byte)equipSlot);
            else
                useItem((int)Map.LastGameTime.TotalMs, Owner.ObjectId, SlotId, ItemDesc.ObjectType,
                    Map.LocalPlayer.Position.X, Map.LocalPlayer.Position.Y, 1);
        }
    }

    private int GetNextAvailableInventorySlot() {
        if (Map.LocalPlayer == null) return -1;
        for (var i = 4; i < 12; i++)
            if (Map.LocalPlayer.Equipment.Length > i) {
                var item = Map.LocalPlayer.Equipment[i];
                if (item == null || item.ObjectType == 0) return i;
            }

        return -1;
    }

    private int GetMatchingEquipmentSlot() {
        if (Map.LocalPlayer == null || ItemDesc == null) return -1;
        if (ItemDesc.SlotType == 0) return 0;
        for (var i = 0; i < 4; i++)
            if (Map.LocalPlayer.Properties.SlotTypes.Count > i)
                if (Map.LocalPlayer.Properties.SlotTypes[i] == ItemDesc.SlotType)
                    return i;

        return -1;
    }

    private void SwapItem(Entity targetEntity, byte targetSlot) {
        var targetItem = targetEntity.Equipment[targetSlot];
        var targetObjType = targetItem?.ObjectType ?? 0;

        var swap = InvSwap.CreatePacket();
        swap.Time = (int)Map.LastGameTime.TotalMs;
        swap.Position = new Position { X = Map.LocalPlayer.Position.X, Y = Map.LocalPlayer.Position.Y };

        swap.SlotObj1 = new ObjectSlot {
            ObjectId = Owner.ObjectId,
            SlotId = SlotId,
            ObjectType = ItemDesc.ObjectType
        };
        swap.SlotObj2 = new ObjectSlot {
            ObjectId = targetEntity.ObjectId,
            SlotId = targetSlot,
            ObjectType = targetObjType
        };
        Client.QueuePacket(swap);

        var sourceItem = ItemDesc;
        Owner.Equipment[SlotId] = targetItem;
        targetEntity.Equipment[targetSlot] = sourceItem;

        SetItem(targetItem);
        if (targetEntity is Player tPlayer) tPlayer.InventoryUpdate.Dispatch(targetSlot);

        if (Owner is Player oPlayer) oPlayer.InventoryUpdate.Dispatch(SlotId);
    }

    private void CancelDragCheck() {
        _checkForDrag = false;
        Map.GameSprite.Stage?.RemoveEventListener(MouseEvent.LeftUp, CancelDragCheck);
    }

    private void OnFrameEnter() {
        if (!_checkForDrag) return;
        var delta = GetRelativeMousePosition() - _dragStart;
        var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

        if (dist > 3) {
            CancelDragCheck();
            OnBeginDrag();
        }
    }

    private void OnBeginDrag() {
        if (_dragging) return;
        _dragging = true;

        if (SlotType != 0)
            _slotDetail.Visible = true;

        if (Owner is Player && SlotType == 0)
            _slotId.Visible = true;

        TooltipManager.RemoveTooltip(_tooltip);

        RemoveChild(_sprite);
        RemoveChild(_tierText);

        _sprite.Scale = Stage.ScreenScale;
        _sprite.StartDrag();

        Map.GameSprite.AddChild(_sprite);
        Map.GameSprite.Stage?.AddEventListener(MouseEvent.LeftUp, OnEndDrag);
    }

    private void OnEndDrag(MouseEvent args) {
        Map.GameSprite.Stage?.RemoveEventListener(MouseEvent.LeftUp, OnEndDrag);

        if (!_dragging) return;
        _dragging = false;

        _sprite.EndDrag();
        _sprite.Scale = Vector2.One;
        Map.GameSprite.RemoveChild(_sprite);

        AddChild(_sprite);
        AddChild(_tierText);

        _slotDetail.Visible = false;
        _slotId.Visible = false;

        HandleDropTarget();
    }

    private void HandleDropTarget() {
        var list = new[] { typeof(ItemTile), typeof(InventoryGrid), typeof(HudView), typeof(GameScreen) };

        var target = _sprite.DropTarget.GetTypeFromList(list);

        if (target == null) {
            SetItem(ItemDesc);
            return;
        }

        if (ItemDesc != null && (ItemDesc.ObjectType == ItemConstants.HealthPotion ||
                                 ItemDesc.ObjectType == ItemConstants.MagicPotion))
            if (!(target is GameScreen))
                if (AddToPotionStack())
                    return;

        switch (target) {
            case ItemTile tile:
                if (!tile.Interactive) break;
                if (tile.OneWay) break;
                if (!CanSwapItems(this, tile)) break;

                var swap = InvSwap.CreatePacket();
                swap.Time = (int)Map.LastGameTime.TotalMs;
                swap.Position = new Position { X = Map.LocalPlayer.Position.X, Y = Map.LocalPlayer.Position.Y };

                swap.SlotObj1 = new ObjectSlot {
                    ObjectId = Owner.ObjectId,
                    SlotId = SlotId,
                    ObjectType = ItemDesc.ObjectType
                };
                swap.SlotObj2 = new ObjectSlot {
                    ObjectId = tile.Owner.ObjectId,
                    SlotId = tile.SlotId,
                    ObjectType = tile.ItemDesc?.ObjectType ?? 0
                };
                Client.QueuePacket(swap);
                var draggedItem = ItemDesc;

                Owner.Equipment[SlotId] = tile.ItemDesc;
                tile.Owner.Equipment[tile.SlotId] = draggedItem;

                SetItem(Owner.Equipment[SlotId]);
                tile.SetItem(tile.Owner.Equipment[tile.SlotId]);

                if (Owner is Player oPlayer) oPlayer.InventoryUpdate.Dispatch(SlotId);
                if (tile.Owner is Player tPlayer) tPlayer.InventoryUpdate.Dispatch(tile.SlotId);

                break;
            case InventoryGrid grid:
                break;
            case GameScreen:
                var drop = InvDrop.CreatePacket();
                drop.SlotObject = new ObjectSlot {
                    ObjectId = Owner.ObjectId,
                    SlotId = SlotId,
                    ObjectType = ItemDesc.ObjectType
                };

                Client.QueuePacket(drop);

                SetItem(null);
                break;
            default:
                SetItem(ItemDesc);
                break;
        }
    }

    private static bool CanSwapItems(ItemTile source, ItemTile target) {
        return source.CanHoldItem(target.ItemDesc) && target.CanHoldItem(source.ItemDesc);
    }

    private bool CanHoldItem(ItemDesc itemDesc) {
        return (itemDesc?.ObjectType ?? 0) == 0 || SlotType == 0 || SlotType == itemDesc.SlotType ||
               itemDesc.SlotType == 0;
    }

    private static bool IsUsableByPlayer(ItemDesc itemDesc) {
        if (Map.LocalPlayer == null || itemDesc == null) return true;
        if (itemDesc.ObjectType == 0) return false;
        if (itemDesc.SlotType == 0) return true;

        var slotType = itemDesc.SlotType;

        if (slotType == ItemConstants.PotionType)
            return true;

        var slots = Map.LocalPlayer.Properties.SlotTypes;
        for (var i = 0; i < slots.Count; i++)
            if (slots[i] == slotType)
                return true;

        return false;
    }

    private static void useItem(int time, int objectId, byte slotId, ushort objectType, float itemUsePosX,
                                float itemUsePosY, byte useType) {
        var packet = UseItem.CreatePacket();

        packet.Time = time;
        packet.SlotObject.ObjectId = objectId;
        packet.SlotObject.SlotId = slotId;
        packet.SlotObject.ObjectType = objectType;
        packet.ItemUsePos.X = itemUsePosX;
        packet.ItemUsePos.Y = itemUsePosY;
        packet.UseType = useType;
        packet.ProjId = Map.LocalPlayer.ReadServerId();

        Client.QueuePacket(packet);
    }

    private bool AddToPotionStack() {
        if (Map.LocalPlayer == null || ItemDesc == null) return false;

        var isHp = ItemDesc.ObjectType == ItemConstants.HealthPotion;
        var isMp = ItemDesc.ObjectType == ItemConstants.MagicPotion;

        if (!isHp && !isMp) return false;
        var currentCount = isHp ? Map.LocalPlayer.HealthStackCount : Map.LocalPlayer.MagicStackCount;
        if (currentCount >= 6) return false;

        var targetSlot = isHp ? ItemConstants.HpPotionSlot : ItemConstants.MpPotionSlot;
        var swap = InvSwap.CreatePacket();
        swap.Time = (int)Map.LastGameTime.TotalMs;
        swap.Position = new Position { X = Map.LocalPlayer.Position.X, Y = Map.LocalPlayer.Position.Y };

        swap.SlotObj1 = new ObjectSlot {
            ObjectId = Owner.ObjectId,
            SlotId = SlotId,
            ObjectType = ItemDesc.ObjectType
        };
        swap.SlotObj2 = new ObjectSlot {
            ObjectId = Map.LocalPlayer.ObjectId,
            SlotId = targetSlot,
            ObjectType = 0
        };
        Client.QueuePacket(swap);
        Owner.Equipment[SlotId] = null;
        SetItem(null);

        if (Owner is Player oPlayer) {
            oPlayer.InventoryUpdate.Dispatch(SlotId);
            if (isHp) oPlayer.HealthStackCount++;
            else oPlayer.MagicStackCount++;
        }

        return true;
    }
}

