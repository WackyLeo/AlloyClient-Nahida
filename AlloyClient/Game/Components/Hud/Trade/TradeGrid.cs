using System;
using System.Collections.Generic;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Trade;

public class TradeGrid : Sprite {
    private const int Cols = 4;
    private const int SlotSpacing = 4;
    private const int SlotSize = 48;
    private const int SectionGap = 6;

    private readonly List<TradeTile> _tiles = new();
    private readonly bool _clickable;
    private readonly Action<bool[]> _onOfferChanged;

    public TradeGrid(TradeItem[] items, bool clickable, Action<bool[]> onOfferChanged = null) {
        _clickable = clickable;
        _onOfferChanged = onOfferChanged;

        for (int i = 0; i < items.Length; i++) {
            var tile = new TradeTile(GetCutsForIndex(i));

            int col = i % Cols;
            int row = i / Cols;

            tile.X = col * (SlotSize + SlotSpacing);
            tile.Y = row == 0
                ? 0
                : SlotSize + SectionGap + (row - 1) * (SlotSize + SlotSpacing);

            tile.SetItem((ushort)items[i].Item, items[i].Tradeable, items[i].Included);

            if (_clickable && items[i].Tradeable && items[i].Item > 0) {
                int index = i;
                tile.AddEventListener(MouseEvent.LeftDown, () => ToggleTileSelection(index));
            }

            AddChild(tile);
            _tiles.Add(tile);
        }
    }
    
    private static CutEdges GetCutsForIndex(int i) {
        int col = i % Cols;
        int row = i / Cols;
        bool left = col == 0;
        bool right = col == Cols - 1;

        return row switch {
            0 when left  => CutEdges.TopLeft | CutEdges.BottomLeft,
            0 when right => CutEdges.TopRight | CutEdges.BottomRight,
            0 => CutEdges.None,
            1 when left  => CutEdges.TopLeft,
            1 when right => CutEdges.TopRight,
            2 when left  => CutEdges.BottomLeft,
            2 when right => CutEdges.BottomRight,
            _ => CutEdges.None
        };
    }

    private void ToggleTileSelection(int index) {
        _tiles[index].SetSelected(!_tiles[index].Selected);
        _onOfferChanged?.Invoke(GetOfferState());
    }

    public void SetOfferState(bool[] offers) {
        for (int i = 0; i < Math.Min(_tiles.Count, offers.Length); i++)
            _tiles[i].SetSelected(offers[i]);
    }

    public bool[] GetOfferState() {
        var offers = new bool[_tiles.Count];
        for (int i = 0; i < _tiles.Count; i++)
            offers[i] = _tiles[i].Selected;
        return offers;
    }
}
