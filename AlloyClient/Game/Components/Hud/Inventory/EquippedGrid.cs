using System.Collections.Generic;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Objects;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Inventory;

public sealed class EquippedGrid : Sprite {
    private const byte NumSlots = 4;

    private static readonly CutEdges[] Cuts = [CutEdges.Left, CutEdges.None, CutEdges.None, CutEdges.Right];

    private readonly Player _owner;
    private readonly ItemTile[] _tileSlots = new ItemTile[4];

    public EquippedGrid(Player owner) {
        _owner = owner;

        var bg = new CutEdgeRect(new CutEdgeConfig
            { Width = 216, Height = 56, CutX = 6, CutY = 6, Cuts = CutEdges.All, Color = 0x676767 });
        AddChild(bg);

        _owner.InventoryUpdate.Add(OnInventoryChange);

        for (byte i = 0; i < NumSlots; i++) {
            var slot = new ItemTile(_owner, i, true, Cuts[i], false, (byte)_owner.Properties.SlotTypes[i], 49,
                0x454545);
            slot.X = i % 4 * (49 + 4) + 4;
            slot.Y = i / 4 * (49 + 4) + 4;
            AddChild(slot);
            _tileSlots[i] = slot;
        }

        var _cooldown = new CooldownOverlay(owner);
        _cooldown.X = 1 * (49 + 4) + 4;
        _cooldown.Y = 4;
        AddChild(_cooldown);
    }

    public EquippedGrid(ItemDesc[] items, List<int> slotTypes) {
        var bg = new CutEdgeRect(new CutEdgeConfig
            { Width = 216, Height = 56, CutX = 6, CutY = 6, Cuts = CutEdges.All, Color = 0x676767 });
        AddChild(bg);

        for (byte i = 0; i < NumSlots; i++) {
            var slot = new ItemTile(null, i, false, Cuts[i], false, (byte)slotTypes[i], 49, 0x454545);
            slot.X = i % 4 * (49 + 4) + 4;
            slot.Y = i / 4 * (49 + 4) + 4;
            slot.SetItem(items[i]);
            AddChild(slot);
            _tileSlots[i] = slot;
        }
    }

    public void Update() {
        if (Map.LocalPlayer == null || _owner == null)
            return;

        for (byte i = 0; i < NumSlots; i++) {
            var actualItem = _owner.Equipment[i];
            if (_tileSlots[i].ItemDesc != actualItem) _tileSlots[i].SetItem(actualItem);
        }

        var abilitySlot = _tileSlots[1];
        if (abilitySlot.ItemDesc != null && abilitySlot.ItemDesc.ObjectType != 0) {
            var noMana = Map.LocalPlayer.Mp < abilitySlot.ItemDesc.MpCost;
            //todo: silence check
            abilitySlot.SetDim(noMana);
        }
    }

    private void OnInventoryChange(int slot) {
        if (slot >= NumSlots) return;
        _tileSlots[slot].SetItem(_owner.Equipment[slot]);
    }
}