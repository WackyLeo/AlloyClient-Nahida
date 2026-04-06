using System;
using System.Collections.Generic;
using AlloyClient.Game.Components;
using AlloyClient.Game.Components.Hud;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Storage;

public class StorageWindow : Sprite {
    private const int BgWidth = 560;
    private const int BgHeight = 560;
    private const int Pad = 20;
    private const int TabIslandH = 35;
    private const int TabIslandGap = 18;
    private const int Inner = 14;
    private const int RowH = 56;
    private const int BottomBarH = 64;
    private const int ScrollbarW = 8;
    private const int TabW = 110;
    private const int TabSpacing = 6;
    private const int TabHeightPad = 6;

    private const uint ColBg = 0xFF363636u;
    private const uint ColBgOutline = 0xFF676767u;
    private const uint ColListBg = 0xFF252525u;
    private const uint ColListLine = 0xFF484848u;
    private const uint ColTabActive = 0xFF4A6A9Au;
    private const uint ColTabInactive = 0xFF2E2E2Eu;
    private const uint ColTabOutline = 0xFF606060u;
    private const uint ColTabActLine = 0xFF7090C0u;
    private const uint ColBtnGreen = 0xFF3A7A3Au;
    private const uint ColBtnGreenOut = 0xFF66BB66u;
    private const uint ColScrollTrack = 0xFF1A1A1Au;
    private const uint ColScrollThumb = 0xFF676767u;
    private readonly SdfRect _background;

    private readonly Dictionary<ushort, StorageLine> _lineDict = new();
    private readonly Container _listClip;
    private readonly Container _listContent;
    private readonly SdfRect _marksTab;
    private readonly SdfRect _potionsTab;
    private readonly Container _screenContainer;
    private readonly SdfRect _scrollThumb;
    private readonly SdfRect _scrollTrack;
    private readonly SdfRect _tabIsland;
    private readonly SdfRect _tokensTab;

    private Tab _currentTab = Tab.Potions;
    private bool _isDraggingThumb;
    private float _maxScrollOffset;
    private float _scrollOffset;
    private float _thumbDragStartOffset;
    private float _thumbDragStartY;

    public StorageWindow() {
        Instance = this;

        _screenContainer = new Container();
        AddChild(_screenContainer);

        _tabIsland = new SdfRect(new SdfRectConfig {
            Width = BgWidth,
            Height = TabIslandH,
            Cut = 8,
            Color = 0xFF363636u,
            OutlineColor = 0xFF676767u,
            OutlineSize = 5f,
            ShadowRadius = 3f,
            ShadowAlpha = 0.3f
        });
        _tabIsland.SetAnchor(UiAnchor.LeftTop);
        _tabIsland.X = Pad;
        _tabIsland.Y = Pad;
        _screenContainer.AddChildAt(_tabIsland, 0);

        _potionsTab = CreateTabButton("Potions", 0);
        _marksTab = CreateTabButton("Marks", 1);
        _tokensTab = CreateTabButton("Tokens", 2);

        _potionsTab.AddEventListener(MouseEvent.LeftClick, () => SwitchTab(Tab.Potions));
        _marksTab.AddEventListener(MouseEvent.LeftClick, () => SwitchTab(Tab.Marks));
        _tokensTab.AddEventListener(MouseEvent.LeftClick, () => SwitchTab(Tab.Tokens));

        _tabIsland.AddChild(_potionsTab);
        _tabIsland.AddChild(_marksTab);
        _tabIsland.AddChild(_tokensTab);

        var lastTabEdge = TabHeightPad + 3 * (TabW + TabSpacing);
        var closeButtonEdge = _tabIsland.Width - TabHeightPad - 21;

        var titleLabel = new SimpleText(new TextConfig {
            FontSize = 13, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        titleLabel.SetText("Storage");
        titleLabel.X = lastTabEdge + (closeButtonEdge - lastTabEdge) / 2;
        titleLabel.Y = TabIslandH / 2;
        _tabIsland.AddChild(titleLabel);

        var closeBtn = new SdfRect(new SdfRectConfig {
            Width = 21, Height = 21, Cut = 4,
            Color = 0xFF555555u, OutlineColor = 0xFF888888u, OutlineSize = 2f, ShadowRadius = 0f
        });
        closeBtn.SetAnchor(UiAnchor.RightTop);
        closeBtn.X = _tabIsland.Width - TabHeightPad;
        closeBtn.Y = (_tabIsland.Height - 21) / 2;
        closeBtn.MouseEnabled = true;
        closeBtn.AddEventListener(MouseEvent.LeftClick, OnCloseClicked);
        _tabIsland.AddChild(closeBtn);

        var closeX = new SimpleText(new TextConfig {
            FontSize = 11, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        closeX.SetText("X");
        closeX.X = 21 / 2;
        closeX.Y = 21 / 2;
        closeBtn.AddChild(closeX);

        _background = new SdfRect(new SdfRectConfig {
            Width = BgWidth,
            Height = BgHeight,
            Cut = 8,
            Color = ColBg,
            OutlineColor = ColBgOutline,
            OutlineSize = 5f,
            ShadowRadius = 3f,
            ShadowAlpha = 0.3f
        });
        _background.SetAnchor(UiAnchor.LeftTop);
        _background.X = Pad;
        _background.Y = Pad + TabIslandH + TabIslandGap;
        _screenContainer.AddChildAt(_background, 1);

        var listH = BgHeight - Inner * 2 - BottomBarH - Inner;

        var listBg = new SdfRect(new SdfRectConfig {
            Width = BgWidth - Inner * 2, Height = listH, Cut = 6,
            Color = ColListBg, OutlineColor = ColListLine, OutlineSize = 2f, ShadowRadius = 0f
        });
        listBg.X = Inner;
        listBg.Y = Inner;
        listBg.MouseEnabled = true;
        _background.AddChild(listBg);

        var clipW = (int)listBg.SelfContentWidth - ScrollbarW - Inner * 2;
        var clipH = (int)listBg.SelfContentHeight - Inner * 2;

        _listClip = new Container(new ContainerConfig {
            Width = clipW, Height = clipH, EnableClip = true
        });
        _listClip.X = Inner;
        _listClip.Y = Inner;
        listBg.AddChild(_listClip);

        _listContent = new Container();
        _listClip.AddChild(_listContent);

        _scrollTrack = new SdfRect(new SdfRectConfig {
            Width = ScrollbarW, Height = clipH, Cut = 4,
            Color = ColScrollTrack, OutlineSize = 0f, ShadowRadius = 0f
        });
        _scrollTrack.X = (int)listBg.SelfContentWidth - ScrollbarW - Inner / 2;
        _scrollTrack.Y = Inner;
        listBg.AddChild(_scrollTrack);

        _scrollThumb = new SdfRect(new SdfRectConfig {
            Width = ScrollbarW, Height = 40, Cut = 4,
            Color = ColScrollThumb, OutlineSize = 0f, ShadowRadius = 0f
        });
        _scrollThumb.X = _scrollTrack.X;
        _scrollThumb.Y = Inner;
        _scrollThumb.MouseEnabled = true;
        listBg.AddChild(_scrollThumb);

        listBg.AddEventListener(MouseEvent.ScrollVertical, new Action<MouseEvent>(OnMouseWheel));
        _scrollThumb.AddEventListener(MouseEvent.LeftDown, new Action<MouseEvent>(OnThumbMouseDown));

        var bottomBg = new SdfRect(new SdfRectConfig {
            Width = BgWidth - Inner * 2, Height = BottomBarH, Cut = 6,
            Color = 0xFF2E2E2Eu, OutlineColor = 0xFF555555u, OutlineSize = 1.5f,
            ShadowRadius = 0f, ShadowAlpha = 0.2f
        });
        bottomBg.X = Inner;
        bottomBg.Y = BgHeight - Inner - BottomBarH;
        _background.AddChild(bottomBg);

        var upgradeBtn = CreateBottomButton("Upgrade!", 0, bottomBg);
        var storeBtn = CreateBottomButton("Store!", 1, bottomBg);
        upgradeBtn.AddEventListener(MouseEvent.LeftClick, OnUpgradeClicked);
        storeBtn.AddEventListener(MouseEvent.LeftClick, OnStoreClicked);

        StorageItems.Load();
        SwitchTab(Tab.Potions);

        SetPosition(Settings.ScreenWidth, Settings.ScreenHeight);
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);

        Client.QueuePacket(StorageRequest.CreatePacket());
    }

    public static StorageWindow Instance { get; private set; }

    private void SwitchTab(Tab tab) {
        _currentTab = tab;
        SetTabActive(_potionsTab, tab == Tab.Potions);
        SetTabActive(_marksTab, tab == Tab.Marks);
        SetTabActive(_tokensTab, tab == Tab.Tokens);

        var ids = tab switch {
            Tab.Potions => StorageItems.Potions,
            Tab.Marks => StorageItems.Marks,
            Tab.Tokens => StorageItems.Tokens,
            _ => StorageItems.Potions
        };
        LoadCategory(ids);
    }

    private static void SetTabActive(SdfRect tab, bool active) {
        tab.SetColor(active ? ColTabActive : ColTabInactive);
        tab.SetColorSecondary(active ? ColTabActLine : ColTabOutline);
    }

    private void LoadCategory(ushort[] ids) {
        _lineDict.Clear();
        while (_listContent.NumChildren > 0)
            _listContent.RemoveChildAt(0);

        var rowW = (int)_listClip.SelfContentWidth;
        var currentY = 0;

        for (var i = 0; i < ids.Length; i++) {
            var id = ids[i];
            var item = StorageItems.ItemDict.GetValueOrDefault(id);
            if (item == null) continue;

            var line = new StorageLine(item, rowW, RowH, i % 2 == 0, OnPull, OnMax, OnCombine);
            line.X = 0;
            line.Y = currentY;
            _listContent.AddChild(line);
            _lineDict[id] = line;
            currentY += RowH;
        }

        _scrollOffset = 0f;
        _listContent.Y = 0;

        var viewH = (int)_listClip.SelfContentHeight;
        _maxScrollOffset = Math.Max(0, currentY - viewH);

        _listClip.SelfContentWidth = rowW;
        _listClip.SelfContentHeight = viewH;
        _listClip.UpdateBounds();

        RefreshScrollbar(viewH, currentY);
    }

    public void FullRefresh() {
        foreach (var line in _lineDict.Values)
            line.Refresh();
    }

    private void OnPull(ushort id) {
        SendStorageCommand(0, id);
    }

    private void OnMax(ushort id) {
        SendStorageCommand(1, id);
    }

    private void OnCombine(ushort id) {
        SendStorageCommand(2, id);
    }

    private static void SendStorageCommand(int cmd, ushort id) {
        var pkt = StorageCommand.CreatePacket();
        pkt.Cmd = cmd;
        pkt.ObjType = id;
        Client.QueuePacket(pkt);
    }

    private void OnUpgradeClicked() {
        var pkt = StorageBuy.CreatePacket();
        pkt.ObjType = 1;
        Client.QueuePacket(pkt);
    }

    private void OnStoreClicked() {
        Client.QueuePacket(StorageGrab.CreatePacket());
    }

    private void OnCloseClicked() {
        UserInput.SetInputDisabled(false);
        Instance = null;
        Parent?.RemoveChild(this);
    }

    private void RefreshScrollbar(float viewH, float contentH) {
        _scrollThumb.Visible = contentH > viewH;
        if (!_scrollThumb.Visible) return;

        var thumbH = (int)Math.Max(20, viewH * (viewH / contentH));
        _scrollThumb.Resize(ScrollbarW, thumbH);
        var trackRange = viewH - thumbH;
        _scrollThumb.Y = (int)(_scrollTrack.Y +
                               (_maxScrollOffset > 0 ? _scrollOffset / _maxScrollOffset * trackRange : 0));
    }

    private void ApplyScroll() {
        _listContent.Y = -(int)_scrollOffset;
        RefreshScrollbar(_listClip.SelfContentHeight, _maxScrollOffset + _listClip.SelfContentHeight);
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

    private void OnGlobalMouseUp(MouseEvent e) {
        _isDraggingThumb = false;
    }

    private void OnGlobalMouseMove(MouseEvent e) {
        if (!_isDraggingThumb) return;
        var trackRange = _listClip.SelfContentHeight - _scrollThumb.Height;
        if (trackRange <= 0) return;
        var delta = (e.Coords.Y - _thumbDragStartY) / Stage.ScreenScale.Y;
        _scrollOffset = Math.Clamp(
            _thumbDragStartOffset + delta / trackRange * _maxScrollOffset, 0, _maxScrollOffset);
        ApplyScroll();
    }

    private void OnStageEnter() {
        Stage.AddEventListener(MouseEvent.LeftUp, new Action<MouseEvent>(OnGlobalMouseUp));
        Stage.AddEventListener(MouseEvent.MouseMove, new Action<MouseEvent>(OnGlobalMouseMove));
        Stage.AddEventListener(ResizeEvent.Resize, new Action<ResizeEvent>(OnResize));
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(MouseEvent.LeftUp, new Action<MouseEvent>(OnGlobalMouseUp));
        Stage.RemoveEventListener(MouseEvent.MouseMove, new Action<MouseEvent>(OnGlobalMouseMove));
        Stage.RemoveEventListener(ResizeEvent.Resize, new Action<ResizeEvent>(OnResize));
        Instance = null;
    }

    private void OnResize(ResizeEvent e) {
        SetPosition(e.Width, e.Height);
    }

    private void SetPosition(int width, int height) {
        _screenContainer.SetAnchor(UiAnchor.LeftTop);
        _screenContainer.Scale = Stage.ScreenScale;
        var screenW = width - HudView.Width * Stage.ScreenScale.X;

        var totalH = (TabIslandH + TabIslandGap + BgHeight + Pad * 2) * Stage.ScreenScale.Y;
        var knownW = (BgWidth + Pad * 2) * Stage.ScreenScale.X;

        _screenContainer.X = (int)(screenW / 2f - knownW / 2f);
        _screenContainer.Y = (int)(height / 2f - totalH / 2f);
    }

    private SdfRect CreateTabButton(string label, int index) {
        var tab = new SdfRect(new SdfRectConfig {
            Width = TabW,
            Height = TabIslandH - TabHeightPad * 2,
            Cut = 5,
            Color = ColTabInactive,
            OutlineColor = ColTabOutline,
            OutlineSize = 2f,
            ShadowRadius = 0f
        });
        tab.X = TabHeightPad + index * (TabW + TabSpacing);
        tab.Y = TabHeightPad;
        tab.MouseEnabled = true;

        var text = new SimpleText(new TextConfig {
            FontSize = 10, Color = 0xFFDDDDDDu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        text.SetText(label);
        text.X = TabW / 2;
        text.Y = tab.Height / 2;
        tab.AddChild(text);

        return tab;
    }

    private SdfRect CreateBottomButton(string label, int slot, SdfRect parent) {
        const int btnW = 130;
        const int btnH = 30;
        const int btnGap = 20;
        var totalW = btnW * 2 + btnGap;
        var startX = ((int)parent.SelfContentWidth - totalW) / 2;

        var btn = new SdfRect(new SdfRectConfig {
            Width = btnW,
            Height = btnH,
            Cut = 5,
            Color = ColBtnGreen,
            OutlineColor = ColBtnGreenOut,
            OutlineSize = 1.5f,
            ShadowRadius = 0f
        });
        btn.X = startX + slot * (btnW + btnGap);
        btn.Y = (BottomBarH - btnH) / 2;
        btn.MouseEnabled = true;
        parent.AddChild(btn);

        var text = new SimpleText(new TextConfig {
            FontSize = 13, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        text.SetText(label);
        text.X = btnW / 2;
        text.Y = btnH / 2;
        btn.AddChild(text);

        return btn;
    }

    private enum Tab {
        Potions,
        Marks,
        Tokens
    }
}