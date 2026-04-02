// ItemOverviewPage.cs

using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Display;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Wiki;

public class ItemOverviewPage : Container {
    private const int GridPanelWidth = 240;
    private const int IconSize = 44;
    private const int IconSpacing = 6;
    private const int GridPadding = 6;
    private const int SearchBarHeight = 28;
    private const int ScrollbarWidth = 8;
    private const int Padding = 8;

    private const string SearchDefault = "search...";

    private readonly List<ItemDesc> _allItems = new();

    private readonly Container _detailPanel;
    private readonly List<ItemDesc> _filteredItems = new();
    private readonly Container _gridClip;
    private readonly Container _gridContent;
    private readonly SimpleText _itemDescText;
    private readonly SimpleText _itemNameText;
    private readonly SdfRect _scrollbarThumb;

    private readonly SdfRect _scrollbarTrack;
    private readonly TextInput _searchInput;
    private readonly WeaponSimPreview _simPreview;
    private bool _isDraggingThumb;
    private float _maxScrollOffset;

    private float _scrollOffset;
    private ItemDesc _selectedItem;
    private float _thumbDragStartOffset;
    private float _thumbDragStartY;

    public ItemOverviewPage(int width, int height) {
        var gridPanelH = height - Padding * 2;
        var gridInnerH = gridPanelH - SearchBarHeight - Padding * 3;

        var gridBg = new SdfRect(new SdfRectConfig {
            Width = GridPanelWidth,
            Height = gridPanelH,
            Cut = 6,
            Color = 0xFF252525u,
            OutlineColor = 0xFF484848u,
            OutlineSize = 2f,
            ShadowRadius = 0f
        });
        gridBg.X = Padding;
        gridBg.Y = Padding;
        gridBg.MouseEnabled = true;
        AddChild(gridBg);

        _searchInput = new TextInput(new InputConfig {
            Width = GridPanelWidth - Padding * 2,
            FontSize = 10,
            DefaultText = SearchDefault,
            OnChange = OnSearchChanged
        });
        _searchInput.X = Padding;
        _searchInput.Y = Padding;
        gridBg.AddChild(_searchInput);

        _gridClip = new Container(new ContainerConfig {
            Width = GridPanelWidth - Padding * 2,
            Height = gridInnerH,
            EnableClip = true
        });
        _gridClip.X = Padding;
        _gridClip.Y = SearchBarHeight + Padding * 2;
        gridBg.AddChild(_gridClip);

        _gridContent = new Container();
        _gridClip.AddChild(_gridContent);

        _scrollbarTrack = new SdfRect(new SdfRectConfig {
            Width = ScrollbarWidth,
            Height = gridInnerH,
            Cut = 4,
            Color = 0xFF1A1A1Au,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _scrollbarTrack.X = GridPanelWidth - ScrollbarWidth - Padding;
        _scrollbarTrack.Y = SearchBarHeight + Padding * 2;
        gridBg.AddChild(_scrollbarTrack);

        _scrollbarThumb = new SdfRect(new SdfRectConfig {
            Width = ScrollbarWidth,
            Height = 40,
            Cut = 4,
            Color = 0xFF676767u,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _scrollbarThumb.X = GridPanelWidth - ScrollbarWidth - Padding;
        _scrollbarThumb.Y = SearchBarHeight + Padding * 2;
        _scrollbarThumb.MouseEnabled = true;
        gridBg.AddChild(_scrollbarThumb);

        gridBg.AddEventListener(MouseEvent.ScrollVertical, OnMouseWheel);
        _scrollbarThumb.AddEventListener(MouseEvent.LeftDown, OnThumbMouseDown);

        var detailX = Padding + GridPanelWidth + Padding;
        var detailW = width - detailX - Padding;
        var detailH = height - Padding * 2;

        _detailPanel = new Container(new ContainerConfig {
            Width = detailW,
            Height = detailH,
            EnableClip = true
        });
        _detailPanel.X = detailX;
        _detailPanel.Y = Padding;
        AddChild(_detailPanel);

        var divider = new SdfRect(new SdfRectConfig {
            Width = 1,
            Height = detailH,
            Cut = 0,
            Color = 0xFF404040u,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        divider.X = -Padding / 2;
        _detailPanel.AddChild(divider);

        _itemNameText = new SimpleText(new TextConfig {
            FontSize = 18,
            Color = 0xFFFFFFFFu,
            FontType = FontType.Bold,
            Anchor = UiAnchor.LeftTop
        });
        _itemNameText.X = 16;
        _itemNameText.Y = 12;
        _detailPanel.AddChild(_itemNameText);

        _itemDescText = new SimpleText(new TextConfig {
            FontSize = 9,
            Color = 0xFFAAAAAAu,
            FontType = FontType.Normal,
            Anchor = UiAnchor.LeftTop,
            MaxWidth = detailW - 32
        });
        _itemDescText.X = 16;
        _itemDescText.Y = 38;
        _detailPanel.AddChild(_itemDescText);

        var previewY = 80;
        var previewW = detailW - 32;
        var previewH = detailH - previewY - 16;

        _simPreview = new WeaponSimPreview(previewW, previewH);
        _simPreview.X = 16;
        _simPreview.Y = previewY;
        _detailPanel.AddChild(_simPreview);

        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);

        PopulateItems();
    }


    private void OnAddedToStage() {
        Stage.AddEventListener(MouseEvent.LeftUp, OnGlobalMouseUp);
        Stage.AddEventListener(MouseEvent.MouseMove, OnGlobalMouseMove);
    }

    private void OnRemovedFromStage() {
        Stage.RemoveEventListener(MouseEvent.LeftUp, OnGlobalMouseUp);
        Stage.RemoveEventListener(MouseEvent.MouseMove, OnGlobalMouseMove);
    }


    private void PopulateItems() {
        foreach (var kvp in ObjectLibrary.TypeToItem) {
            var item = kvp.Value;

            if (item.Projectiles == null && (item.AllProjectiles == null || item.AllProjectiles.Length == 0))
                continue;

            if (item.Potion || item.Consumable || item.TypeOfConsumable || item.InvUse)
                continue;

            _allItems.Add(item);
        }

        _allItems.Sort((a, b) => {
            var slotCmp = a.SlotType.CompareTo(b.SlotType);
            if (slotCmp != 0) return slotCmp;

            var tierA = a.Tier > 0 ? a.Tier : 999;
            var tierB = b.Tier > 0 ? b.Tier : 999;
            var tierCmp = tierA.CompareTo(tierB);
            if (tierCmp != 0) return tierCmp;

            var tribCmp = a.TributePoints.CompareTo(b.TributePoints);
            if (tribCmp != 0) return tribCmp;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        ApplyFilter("");

        if (_allItems.Count > 0)
            SelectItem(_allItems[0]);
    }

    private void ApplyFilter(string search) {
        _filteredItems.Clear();
        foreach (var item in _allItems)
            if (search.Length == 0 || item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                _filteredItems.Add(item);
        RebuildGrid();
    }

    private void RebuildGrid(bool preserveScroll = false) {
        while (_gridContent.NumChildren > 0)
            _gridContent.RemoveChildAt(0);

        var gridInnerW = (int)_gridClip.SelfContentWidth - ScrollbarWidth - 4;
        var iconsPerRow = Math.Max(1, (gridInnerW - GridPadding * 2 + IconSpacing) / (IconSize + IconSpacing));
        var totalRowW = iconsPerRow * IconSize + (iconsPerRow - 1) * IconSpacing;
        var rowOffsetX = (gridInnerW - totalRowW) / 2;

        var x = rowOffsetX;
        var y = GridPadding;
        var col = 0;

        for (var i = 0; i < _filteredItems.Count; i++) {
            var item = _filteredItems[i];
            var captured = item;
            var isSelected = _selectedItem?.ObjectType == item.ObjectType;

            var cell = new SdfRect(new SdfRectConfig {
                Width = IconSize,
                Height = IconSize,
                Cut = 5,
                Color = isSelected ? 0xFF4A6A9Au : 0xFF2D2D2Du,
                OutlineColor = isSelected ? 0xFF7090C0u : 0xFF484848u,
                OutlineSize = 2f,
                ShadowRadius = 0f
            });
            cell.X = x;
            cell.Y = y;
            cell.MouseEnabled = true;
            _gridContent.AddChild(cell);

            var icon = new ObjectRect(new ObjectRectConfig {
                Texture = TextureHelper.FromGameAtlas(item.ObjectType),
                Width = IconSize - 8,
                Height = IconSize - 8,
                OutlineEnabled = false,
                GlowEnabled = false
            });
            icon.X = x + 4;
            icon.Y = y + 4;
            _gridContent.AddChild(icon);

            cell.AddEventListener(MouseEvent.LeftClick, () => SelectItem(captured));

            EquipmentToolTip tooltip = null;
            cell.AddEventListener(MouseEvent.MouseOver, () => {
                tooltip = new EquipmentToolTip(captured);
                TooltipManager.AddTooltip(tooltip);
            });
            cell.AddEventListener(MouseEvent.MouseOut, () => {
                if (tooltip == null) return;
                TooltipManager.RemoveTooltip(tooltip);
                tooltip = null;
            });

            col++;
            if (col >= iconsPerRow) {
                col = 0;
                x = rowOffsetX;
                y += IconSize + IconSpacing;
            } else {
                x += IconSize + IconSpacing;
            }
        }

        if (col > 0) y += IconSize + IconSpacing;
        y += GridPadding;

        var viewH = (int)_gridClip.SelfContentHeight;
        _maxScrollOffset = Math.Max(0, y - viewH);
        _gridClip.SelfContentWidth = (int)_gridClip.SelfContentWidth;
        _gridClip.SelfContentHeight = viewH;
        _gridClip.UpdateBounds();

        if (!preserveScroll) {
            _scrollOffset = 0f;
            _gridContent.Y = 0;
        } else {
            _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScrollOffset);
            _gridContent.Y = -(int)_scrollOffset;
        }

        RefreshScrollbar(viewH, y);
    }

    private void SelectItem(ItemDesc item) {
        _selectedItem = item;
        _itemNameText.SetText(item.DisplayName);
        _itemDescText.SetText(item.Description ?? "");
        _simPreview.LoadWeapon(item);
        RebuildGrid(true);
    }

    private void RefreshScrollbar(float viewH, float contentH) {
        _scrollbarThumb.Visible = contentH > viewH;
        if (!_scrollbarThumb.Visible) return;

        var thumbH = (int)Math.Max(20, viewH * (viewH / contentH));
        _scrollbarThumb.Resize(ScrollbarWidth, thumbH);

        var trackRange = viewH - thumbH;
        _scrollbarThumb.Y = (int)(_scrollbarTrack.Y + (_maxScrollOffset > 0
            ? _scrollOffset / _maxScrollOffset * trackRange
            : 0));
    }

    private void ApplyScroll() {
        _gridContent.Y = -(int)_scrollOffset;
        var viewH = _gridClip.SelfContentHeight;
        var contentH = _maxScrollOffset + viewH;
        RefreshScrollbar(viewH, contentH);
    }

    private void OnMouseWheel(MouseEvent e) {
        _scrollOffset = Math.Clamp(_scrollOffset - e.Delta * 20f, 0, _maxScrollOffset);
        ApplyScroll();
    }

    private void OnThumbMouseDown(MouseEvent e) {
        _isDraggingThumb = true;
        _thumbDragStartY = e.Coords.Y;
        _thumbDragStartOffset = _scrollOffset;
    }

    private void OnGlobalMouseUp() {
        _isDraggingThumb = false;
    }

    private void OnGlobalMouseMove(MouseEvent e) {
        if (!_isDraggingThumb) return;
        var trackRange = _gridClip.SelfContentHeight - _scrollbarThumb.Height;
        if (trackRange <= 0) return;
        _scrollOffset = Math.Clamp(
            _thumbDragStartOffset + (e.Coords.Y - _thumbDragStartY) / trackRange * _maxScrollOffset,
            0, _maxScrollOffset);
        ApplyScroll();
    }

    private void OnSearchChanged() {
        var text = _searchInput.Text;
        ApplyFilter(text == SearchDefault ? "" : text);
    }
}