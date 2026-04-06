using AlloyClient.Game.Components;
using AlloyClient.Game.Components.Hud;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Wiki;

public class WikiWindow : Sprite {
    private const int BgWidth = 860;
    private const int BgHeight = 560;
    private const int Pad = 20;
    private const int TabIslandH = 35;
    private const int TabIslandGap = 18;
    private const int TabW = 120;
    private const int TabSpacing = 6;
    private const int TabHeightPad = 6;

    private const uint ColTabActive = 0xFF4A6A9Au;
    private const uint ColTabInactive = 0xFF2E2E2Eu;
    private const uint ColTabOutline = 0xFF606060u;
    private const uint ColTabActLine = 0xFF7090C0u;

    private readonly SdfRect _background;
    private readonly Container _craftingOverviewPage;
    private readonly SdfRect _craftingOverviewTab;

    private readonly Container _enemyOverviewPage;

    private readonly SdfRect _enemyOverviewTab;
    private readonly Container _itemOverviewPage;
    private readonly SdfRect _itemOverviewTab;
    private readonly Container _playerOverviewPage;
    private readonly SdfRect _playerOverviewTab;
    private readonly Container _screenContainer;
    private readonly SdfRect _tabIsland;

    private Container _activePage;

    public WikiWindow() {
        _screenContainer = new Container();
        AddChild(_screenContainer);

        _tabIsland = new SdfRect(new SdfRectConfig {
            Width = BgWidth, Height = TabIslandH, Cut = 8,
            Color = 0xFF363636u, OutlineColor = 0xFF676767u, OutlineSize = 5f,
            ShadowRadius = 3f, ShadowAlpha = 0.3f
        });
        _tabIsland.SetAnchor(UiAnchor.LeftTop);
        _tabIsland.X = Pad;
        _tabIsland.Y = Pad;
        _screenContainer.AddChildAt(_tabIsland, 0);

        _enemyOverviewTab = CreateTabButton("Enemy Overview", 0);
        _itemOverviewTab = CreateTabButton("Item Overview", 1);
        _craftingOverviewTab = CreateTabButton("Crafting", 2);
        _playerOverviewTab = CreateTabButton("Player Overview", 3);

        _enemyOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_enemyOverviewPage));
        _itemOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_itemOverviewPage));
        _craftingOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_craftingOverviewPage));
        _playerOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_playerOverviewPage));

        _tabIsland.AddChild(_enemyOverviewTab);
        _tabIsland.AddChild(_itemOverviewTab);
        _tabIsland.AddChild(_craftingOverviewTab);
        _tabIsland.AddChild(_playerOverviewTab);

        var lastTabEdge = TabHeightPad + 4 * (TabW + TabSpacing);
        var closeButtonEdge = _tabIsland.Width - TabHeightPad - 21;

        var wikiLabel = new SimpleText(new TextConfig {
            FontSize = 13, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        wikiLabel.SetText("Wiki");
        wikiLabel.X = lastTabEdge + (closeButtonEdge - lastTabEdge) / 2;
        wikiLabel.Y = TabIslandH / 2;
        _tabIsland.AddChild(wikiLabel);

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
            Width = BgWidth, Height = BgHeight, Cut = 8,
            Color = 0xFF363636u, OutlineColor = 0xFF676767u, OutlineSize = 5f,
            ShadowRadius = 3f, ShadowAlpha = 0.3f
        });
        _background.SetAnchor(UiAnchor.LeftTop);
        _background.X = Pad;
        _background.Y = Pad + TabIslandH + TabIslandGap;
        _screenContainer.AddChildAt(_background, 1);

        var bgWidth = (int)_background.SelfContentWidth;
        var bgHeight = (int)_background.SelfContentHeight;

        var pageClip = new Container(new ContainerConfig {
            Width = bgWidth, Height = bgHeight, EnableClip = true
        });
        _background.AddChild(pageClip);

        _enemyOverviewPage = new EnemyOverviewPage(bgWidth, bgHeight);
        _itemOverviewPage = new ItemOverviewPage(bgWidth, bgHeight);
        _craftingOverviewPage = CreatePage();
        _playerOverviewPage = CreatePage();

        pageClip.AddChild(_enemyOverviewPage);
        pageClip.AddChild(_itemOverviewPage);
        pageClip.AddChild(_craftingOverviewPage);
        pageClip.AddChild(_playerOverviewPage);

        _activePage = _enemyOverviewPage;
        SwitchPage(_activePage);

        SetPosition(Settings.ScreenWidth, Settings.ScreenHeight);
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
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

    private static Container CreatePage() {
        var page = new Container();
        page.SetAnchor(UiAnchor.LeftTop);
        page.Visible = false;
        return page;
    }

    private void SwitchPage(Container page) {
        _enemyOverviewPage.Visible = false;
        _itemOverviewPage.Visible = false;
        _craftingOverviewPage.Visible = false;
        _playerOverviewPage.Visible = false;

        SetTabActive(_enemyOverviewTab, page == _enemyOverviewPage);
        SetTabActive(_itemOverviewTab, page == _itemOverviewPage);
        SetTabActive(_craftingOverviewTab, page == _craftingOverviewPage);
        SetTabActive(_playerOverviewTab, page == _playerOverviewPage);

        page.Visible = true;
        _activePage = page;
    }

    private static void SetTabActive(SdfRect tab, bool active) {
        tab.SetColor(active ? ColTabActive : ColTabInactive);
        tab.SetColorSecondary(active ? ColTabActLine : ColTabOutline);
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }

    private void OnCloseClicked() {
        Parent?.RemoveChild(this);
        UserInput.SetInputDisabled(false);
    }

    private void OnResize(ResizeEvent args) {
        SetPosition(args.Width, args.Height);
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
}