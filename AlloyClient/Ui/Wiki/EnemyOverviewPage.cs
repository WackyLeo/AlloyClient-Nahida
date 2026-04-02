using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Display;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Wiki;

public class EnemyOverviewPage : Container {
    private const int ListPanelWidth = 240;
    private const int EntryHeight = 36;
    private const int EntrySpacing = 3;
    private const int SearchBarHeight = 28;
    private const int ScrollbarWidth = 8;
    private const int Padding = 8;

    private const int EnemySpriteSize = 80;
    private const int ItemIconSize = 44;
    private const int ItemSpacing = 8;
    private const int SectionSpacing = 18;
    private const int RightPadding = 16;

    private const uint ColTealBag = 0xFF00D287u;
    private const uint ColRedBag = 0xFFB2001Du;
    private const uint ColPurpleBag = 0xFF8A3FC2u;
    private const uint ColPotion = 0xFF50A0E0u;
    private const uint ColTiered = 0xFF999999u;

    private const string SearchDefault = "search...";
    private readonly List<EnemyListEntry> _allEntries = new();

    private readonly Container _detailPanel;
    private readonly SimpleText _enemyNameText;
    private readonly List<EnemyListEntry> _filteredEntries = new();
    private readonly Container _listClip;
    private readonly Container _listContent;
    private readonly Container _lootContent;
    private readonly int _lootContentWidth;

    private readonly Container _lootScrollClip;
    private readonly SdfRect _lootScrollThumb;
    private readonly SdfRect _lootScrollTrack;
    private readonly SdfRect _scrollbarThumb;
    private readonly SdfRect _scrollbarTrack;
    private readonly TextInput _searchInput;
    private ObjectRect _enemySprite;
    private bool _isDraggingThumb;
    private bool _isLootDragging;
    private float _lootMaxScroll;
    private float _lootScroll;
    private float _lootThumbDragStartScroll;
    private float _lootThumbDragStartY;
    private float _maxScrollOffset;
    private float _scrollOffset;

    private EnemyListEntry _selectedEntry;
    private float _thumbDragStartOffset;
    private float _thumbDragStartY;

    public EnemyOverviewPage(int width, int height) {
        var listPanelH = height - Padding * 2;
        var listInnerH = listPanelH - SearchBarHeight - Padding * 3;
        var entryW = ListPanelWidth - ScrollbarWidth - Padding * 2 - 4;

        var listBg = new SdfRect(new SdfRectConfig {
            Width = ListPanelWidth,
            Height = listPanelH,
            Cut = 6,
            Color = 0xFF252525u,
            OutlineColor = 0xFF484848u,
            OutlineSize = 2f,
            ShadowRadius = 0f
        });
        listBg.X = Padding;
        listBg.Y = Padding;
        listBg.MouseEnabled = true;
        AddChild(listBg);

        _searchInput = new TextInput(new InputConfig {
            Width = entryW,
            FontSize = 10,
            DefaultText = SearchDefault,
            OnChange = OnSearchChanged
        });
        _searchInput.X = Padding;
        _searchInput.Y = Padding;
        listBg.AddChild(_searchInput);

        _listClip = new Container(new ContainerConfig {
            Width = ListPanelWidth - Padding * 2,
            Height = listInnerH,
            EnableClip = true
        });
        _listClip.X = Padding;
        _listClip.Y = SearchBarHeight + Padding * 2;
        listBg.AddChild(_listClip);

        _listContent = new Container();
        _listClip.AddChild(_listContent);

        _scrollbarTrack = new SdfRect(new SdfRectConfig {
            Width = ScrollbarWidth,
            Height = listInnerH,
            Cut = 4,
            Color = 0xFF1A1A1Au,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _scrollbarTrack.X = ListPanelWidth - ScrollbarWidth - Padding;
        _scrollbarTrack.Y = SearchBarHeight + Padding * 2;
        listBg.AddChild(_scrollbarTrack);

        _scrollbarThumb = new SdfRect(new SdfRectConfig {
            Width = ScrollbarWidth,
            Height = 40,
            Cut = 4,
            Color = 0xFF676767u,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _scrollbarThumb.X = ListPanelWidth - ScrollbarWidth - Padding;
        _scrollbarThumb.Y = SearchBarHeight + Padding * 2;
        _scrollbarThumb.MouseEnabled = true;
        listBg.AddChild(_scrollbarThumb);

        listBg.AddEventListener(MouseEvent.ScrollVertical, OnMouseWheel);
        _scrollbarThumb.AddEventListener(MouseEvent.LeftDown, OnThumbMouseDown);

        var detailX = Padding + ListPanelWidth + Padding;
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

        _enemyNameText = new SimpleText(new TextConfig {
            FontSize = 20,
            Color = 0xFFFFFFFFu,
            FontType = FontType.Bold,
            Anchor = UiAnchor.LeftTop
        });
        _enemyNameText.X = RightPadding;
        _enemyNameText.Y = RightPadding;
        _detailPanel.AddChild(_enemyNameText);

        var lootY = RightPadding + EnemySpriteSize + RightPadding + 28;
        var lootH = detailH - lootY - Padding;
        var lootScrollbarX = detailW - ScrollbarWidth - Padding;
        _lootContentWidth = lootScrollbarX;

        _lootScrollClip = new Container(new ContainerConfig {
            Width = _lootContentWidth,
            Height = lootH,
            EnableClip = true
        });
        _lootScrollClip.X = 0;
        _lootScrollClip.Y = lootY;
        _detailPanel.AddChild(_lootScrollClip);

        _lootContent = new Container();
        _lootScrollClip.AddChild(_lootContent);

        _lootScrollTrack = new SdfRect(new SdfRectConfig {
            Width = ScrollbarWidth,
            Height = lootH,
            Cut = 4,
            Color = 0xFF1A1A1Au,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _lootScrollTrack.X = lootScrollbarX + Padding / 2;
        _lootScrollTrack.Y = lootY;
        _lootScrollTrack.Visible = false;
        _detailPanel.AddChild(_lootScrollTrack);

        _lootScrollThumb = new SdfRect(new SdfRectConfig {
            Width = ScrollbarWidth,
            Height = 40,
            Cut = 4,
            Color = 0xFF676767u,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _lootScrollThumb.X = lootScrollbarX + Padding / 2;
        _lootScrollThumb.Y = lootY;
        _lootScrollThumb.MouseEnabled = true;
        _lootScrollThumb.Visible = false;
        _detailPanel.AddChild(_lootScrollThumb);

        _lootScrollClip.AddEventListener(MouseEvent.ScrollVertical, OnLootMouseWheel);
        _lootScrollThumb.AddEventListener(MouseEvent.LeftDown, OnLootThumbMouseDown);

        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);

        PopulateFromJson();
    }

    private void OnAddedToStage() {
        Stage.AddEventListener(MouseEvent.LeftUp, OnGlobalMouseUp);
        Stage.AddEventListener(MouseEvent.MouseMove, OnGlobalMouseMove);
    }

    private void OnRemovedFromStage() {
        Stage.RemoveEventListener(MouseEvent.LeftUp, OnGlobalMouseUp);
        Stage.RemoveEventListener(MouseEvent.MouseMove, OnGlobalMouseMove);
    }

    private void PopulateFromJson() {
        var data = WikiLibrary.LootDropData;
        if (data == null) return;

        var raw = new List<(string displayId, JsonNode node)>();
        foreach (var kvp in data.AsObject()) {
            var displayId = kvp.Value?["DisplayID"]?.GetValue<string>() ?? kvp.Key;
            raw.Add((displayId, kvp.Value));
        }

        raw.Sort((a, b) => string.Compare(a.displayId, b.displayId, StringComparison.OrdinalIgnoreCase));

        var entryW = ListPanelWidth - ScrollbarWidth - Padding * 2 - 4;
        foreach (var (displayId, node) in raw)
            _allEntries.Add(new EnemyListEntry(displayId, node, entryW, OnEnemySelected));

        ApplyFilter("");
        if (_allEntries.Count > 0) OnEnemySelected(_allEntries[0]);
    }

    private void OnSearchChanged() {
        var text = _searchInput.Text;
        ApplyFilter(text == SearchDefault ? "" : text);
    }

    private void ApplyFilter(string search) {
        _filteredEntries.Clear();
        foreach (var e in _allEntries)
            if (search.Length == 0 || e.DisplayId.Contains(search, StringComparison.OrdinalIgnoreCase))
                _filteredEntries.Add(e);
        RebuildList();
    }

    private void RebuildList(bool preserveScroll = false) {
        while (_listContent.NumChildren > 0)
            _listContent.RemoveChildAt(0);

        var y = 0;
        for (var i = 0; i < _filteredEntries.Count; i++) {
            var entry = _filteredEntries[i];
            entry.SetAlternate(i % 2 == 0);
            entry.X = 0;
            entry.Y = y;
            _listContent.AddChild(entry);
            y += EntryHeight + EntrySpacing;
        }

        var viewH = (int)_listClip.SelfContentHeight;
        _maxScrollOffset = Math.Max(0, y - viewH);
        _listClip.SelfContentWidth = ListPanelWidth - Padding * 2;
        _listClip.SelfContentHeight = viewH;
        _listClip.UpdateBounds();

        if (!preserveScroll) {
            _scrollOffset = 0f;
            _listContent.Y = 0;
        } else {
            _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScrollOffset);
            _listContent.Y = -(int)_scrollOffset;
        }

        RefreshScrollbar(viewH, y);
    }

    private void OnEnemySelected(EnemyListEntry entry) {
        _selectedEntry?.SetSelected(false);
        _selectedEntry = entry;
        _selectedEntry.SetSelected(true);
        RebuildList(true);
        ShowEnemyDetail(entry.DisplayId, entry.Data, entry.EnemyObjectType);
    }

    private void ShowEnemyDetail(string name, JsonNode data, ushort objectType) {
        _enemyNameText.SetText(name);

        if (_enemySprite != null) {
            _detailPanel.RemoveChild(_enemySprite);
            _enemySprite = null;
        }

        if (objectType != 0) {
            _enemySprite = new ObjectRect(new ObjectRectConfig {
                Texture = WikiTextureHelper.ResolveStaticFrame(objectType),
                Width = EnemySpriteSize,
                Height = EnemySpriteSize,
                OutlineEnabled = true,
                GlowEnabled = false
            });
            _enemySprite.X = (_lootContentWidth - EnemySpriteSize) / 2;
            _enemySprite.Y = RightPadding + 28;
            _detailPanel.AddChild(_enemySprite);
        }

        while (_lootContent.NumChildren > 0)
            _lootContent.RemoveChildAt(0);
        _lootScroll = 0f;
        _lootContent.Y = 0;

        var drops = data?["Drops"]?.AsArray();
        if (drops == null || drops.Count == 0) {
            _lootScrollTrack.Visible = false;
            _lootScrollThumb.Visible = false;
            return;
        }

        var tealBag = new List<JsonNode>();
        var redBag = new List<JsonNode>();
        var purpleBag = new List<JsonNode>();
        var potions = new List<JsonNode>();
        var tiered = new List<JsonNode>();

        foreach (var drop in drops) {
            var itemName = drop?["Item"]?.GetValue<string>();
            if (itemName == null) continue;

            ItemDesc item = null;
            if (ObjectLibrary.IdToObjectType.TryGetValue(itemName, out var itemType))
                item = ObjectLibrary.GetItem(itemType);

            if (item == null) tiered.Add(drop);
            else if (item.TributePoints >= 750000) tealBag.Add(drop);
            else if (item.TributePoints >= 100000) redBag.Add(drop);
            else if (item.TributePoints > 0) purpleBag.Add(drop);
            else if (item.Consumable || item.SlotType == 10) potions.Add(drop);
            else tiered.Add(drop);
        }

        var currentY = 0;
        currentY = AddLootSection(ColTealBag, tealBag, currentY, _lootContentWidth);
        currentY = AddLootSection(ColRedBag, redBag, currentY, _lootContentWidth);
        currentY = AddLootSection(ColPurpleBag, purpleBag, currentY, _lootContentWidth);
        currentY = AddLootSection(ColPotion, potions, currentY, _lootContentWidth);
        currentY = AddLootSection(ColTiered, tiered, currentY, _lootContentWidth);

        var lootViewH = (int)_lootScrollClip.SelfContentHeight;
        _lootMaxScroll = Math.Max(0, currentY - lootViewH);
        RefreshLootScrollbar(lootViewH, currentY);
    }

    private int AddLootSection(uint accentColor, List<JsonNode> drops, int startY, int panelW) {
        if (drops.Count == 0) return startY;

        var y = startY;

        var line = new SdfRect(new SdfRectConfig {
            Width = panelW - RightPadding * 2,
            Height = 1,
            Cut = 0,
            Color = accentColor & 0x55FFFFFFu,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        line.X = RightPadding;
        line.Y = y;
        _lootContent.AddChild(line);
        y += 6;

        var x = RightPadding;
        var maxW = panelW - RightPadding * 2;
        var rowMaxH = ItemIconSize + 14;

        foreach (var drop in drops) {
            if (x + ItemIconSize > maxW) {
                x = RightPadding;
                y += rowMaxH + ItemSpacing;
            }

            BuildDropIcon(drop, x, y, accentColor);
            x += ItemIconSize + ItemSpacing;
        }

        y += rowMaxH + SectionSpacing;
        return y;
    }

    private void BuildDropIcon(JsonNode drop, int x, int y, uint accentColor) {
        var itemName = drop?["Item"]?.GetValue<string>() ?? "?";
        var num = drop?["Probability"]?.GetValue<float>() ?? 1;

        ItemDesc itemDesc = null;
        ushort itemObjType = 0;
        if (ObjectLibrary.IdToObjectType.TryGetValue(itemName, out itemObjType))
            itemDesc = ObjectLibrary.GetItem(itemObjType);

        var bg = new SdfRect(new SdfRectConfig {
            Width = ItemIconSize,
            Height = ItemIconSize,
            Cut = 5,
            Color = 0xFF2D2D2Du,
            OutlineColor = accentColor & 0x66FFFFFFu,
            OutlineSize = 2f,
            ShadowRadius = 0f
        });
        bg.X = x;
        bg.Y = y;
        bg.MouseEnabled = true;
        _lootContent.AddChild(bg);

        if (itemObjType != 0) {
            var icon = new ObjectRect(new ObjectRectConfig {
                Texture = TextureHelper.FromGameAtlas(itemObjType),
                Width = ItemIconSize - 8,
                Height = ItemIconSize - 8,
                OutlineEnabled = false,
                GlowEnabled = false
            });
            icon.X = x + 4;
            icon.Y = y + 4;
            _lootContent.AddChild(icon);
        }

        var rateText = new SimpleText(new TextConfig {
            FontSize = 10,
            Color = 0xFFCCCCCC,
            FontType = FontType.Bold,
            OutlineThickness = 0.0f,
            Glow = 1.0f,
            Anchor = UiAnchor.LeftTop
        });
        string displayText;
        if (num > 0f) {
            var reciprocal = 1f / num;
            var denominator = (int)reciprocal;

            if (denominator >= 10000 && denominator % 1000 == 0)
                displayText = $"1/{denominator / 1000}k";
            else
                displayText = $"1/{denominator}";
        } else {
            displayText = "N/A";
        }

        rateText.SetText(displayText);
        rateText.X = x;
        rateText.Y = y + ItemIconSize + 4;
        _lootContent.AddChild(rateText);

        if (itemDesc != null) {
            var capturedDesc = itemDesc;
            EquipmentToolTip tooltip = null;
            bg.AddEventListener(MouseEvent.MouseOver, () => {
                tooltip = new EquipmentToolTip(capturedDesc);
                TooltipManager.AddTooltip(tooltip);
            });
            bg.AddEventListener(MouseEvent.MouseOut, () => {
                if (tooltip == null) return;
                TooltipManager.RemoveTooltip(tooltip);
                tooltip = null;
            });
        }
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
        _listContent.Y = -(int)_scrollOffset;
        var viewH = _listClip.SelfContentHeight;
        var contentH = _filteredEntries.Count * (float)(EntryHeight + EntrySpacing);
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

    private void RefreshLootScrollbar(float viewH, float contentH) {
        var needsScroll = contentH > viewH && viewH > 0;
        _lootScrollTrack.Visible = needsScroll;
        _lootScrollThumb.Visible = needsScroll;
        if (!needsScroll) return;

        var thumbH = (int)Math.Max(20, viewH * (viewH / contentH));
        _lootScrollThumb.Resize(ScrollbarWidth, thumbH);

        var trackRange = viewH - thumbH;
        _lootScrollThumb.Y = (int)(_lootScrollTrack.Y + (_lootMaxScroll > 0
            ? _lootScroll / _lootMaxScroll * trackRange
            : 0));
    }

    private void ApplyLootScroll() {
        _lootContent.Y = -(int)_lootScroll;
        var viewH = _lootScrollClip.SelfContentHeight;
        RefreshLootScrollbar(viewH, _lootMaxScroll + viewH);
    }

    private void OnLootMouseWheel(MouseEvent e) {
        _lootScroll = Math.Clamp(_lootScroll - e.Delta * 20f, 0, _lootMaxScroll);
        ApplyLootScroll();
    }

    private void OnLootThumbMouseDown(MouseEvent e) {
        _isLootDragging = true;
        _lootThumbDragStartY = e.Coords.Y;
        _lootThumbDragStartScroll = _lootScroll;
    }

    private void OnGlobalMouseUp() {
        _isDraggingThumb = false;
        _isLootDragging = false;
    }

    private void OnGlobalMouseMove(MouseEvent e) {
        if (_isDraggingThumb) {
            var trackRange = _listClip.SelfContentHeight - _scrollbarThumb.Height;
            if (trackRange > 0) {
                _scrollOffset = Math.Clamp(
                    _thumbDragStartOffset + (e.Coords.Y - _thumbDragStartY) / trackRange * _maxScrollOffset,
                    0, _maxScrollOffset);
                ApplyScroll();
            }
        }

        if (_isLootDragging) {
            var trackRange = _lootScrollClip.SelfContentHeight - _lootScrollThumb.Height;
            if (trackRange > 0) {
                _lootScroll = Math.Clamp(
                    _lootThumbDragStartScroll + (e.Coords.Y - _lootThumbDragStartY) / trackRange * _lootMaxScroll,
                    0, _lootMaxScroll);
                ApplyLootScroll();
            }
        }
    }
}