using AlloyClient.Game.Components;
using AlloyClient.Game.Components.Hud;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using Common;

namespace AlloyClient.Ui.Wiki;

public class WikiWindow : Sprite {
    private const int Padding = 20;
    private const int TabHeightPadding = 5;

    private const int TabButtonWidth = 120;
    private const int TabButtonHeight = 19;
    private const int TabButtonSpacing = 6;

    private readonly SdfRect _background;
    private readonly CutEdgeRect _closeButton;
    private readonly SimpleText _closeText;
    private readonly Container _craftingOverviewPage;
    private readonly CutEdgeRect _craftingOverviewTab;

    private readonly Container _enemyOverviewPage;

    private readonly CutEdgeRect _enemyOverviewTab;
    private readonly Container _itemOverviewPage;
    private readonly CutEdgeRect _itemOverviewTab;
    private readonly Container _playerOverviewPage;
    private readonly CutEdgeRect _playerOverviewTab;

    private readonly Container _screenContainer;
    private readonly SdfRect _tabBackground;

    private Container _activePage;


    public WikiWindow() {
        _screenContainer = new Container();
        AddChild(_screenContainer);

        var width = Settings.DefaultScreenWidth;
        var height = Settings.DefaultScreenHeight;

        _tabBackground = new SdfRect(new SdfRectConfig {
            Width = width - HudView.Width - Padding * 2,
            Height = 25,
            Cut = 8,
            Color = 0xFF363636u,
            OutlineColor = 0xFF676767u,
            OutlineSize = 5f,
            ShadowRadius = 3f,
            ShadowAlpha = 0.3f
        });
        _tabBackground.SetAnchor(UiAnchor.LeftTop);
        _tabBackground.X = Padding;
        _tabBackground.Y = Padding;
        _screenContainer.AddChildAt(_tabBackground, 0);

        _background = new SdfRect(new SdfRectConfig {
            Width = width - HudView.Width - Padding * 2,
            Height = height - Padding * 2 - _tabBackground.Height - TabHeightPadding * 2,
            Cut = 8,
            Color = 0xFF363636u,
            OutlineColor = 0xFF676767u,
            OutlineSize = 5f,
            ShadowRadius = 3f,
            ShadowAlpha = 0.3f
        });
        _background.SetAnchor(UiAnchor.LeftTop);
        _background.X = Padding;
        _background.Y = Padding + _tabBackground.Height + TabHeightPadding * 3;
        _screenContainer.AddChildAt(_background, 1);

        _enemyOverviewTab = CreateTabButton("Enemy Overview", 0);
        _itemOverviewTab = CreateTabButton("Item Overview", 1);
        _craftingOverviewTab = CreateTabButton("Crafting Overview", 2);
        _playerOverviewTab = CreateTabButton("Player Overview", 3);

        _enemyOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_enemyOverviewPage));
        _itemOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_itemOverviewPage));
        _craftingOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_craftingOverviewPage));
        _playerOverviewTab.AddEventListener(MouseEvent.LeftClick, () => SwitchPage(_playerOverviewPage));

        _tabBackground.AddChild(_enemyOverviewTab);
        _tabBackground.AddChild(_itemOverviewTab);
        _tabBackground.AddChild(_craftingOverviewTab);
        _tabBackground.AddChild(_playerOverviewTab);

        var lastTabEdge = TabHeightPadding + 4 * (TabButtonWidth + TabButtonSpacing);
        var closeButtonEdge = _tabBackground.Width - TabHeightPadding - 21;

        var wikiLabel = new SimpleText(new TextConfig {
            FontSize = 13,
            Color = 0xFFFFFFFFu,
            FontType = FontType.Bold,
            Anchor = UiAnchor.Middle
        });
        wikiLabel.SetText("Wiki");
        wikiLabel.X = lastTabEdge + (closeButtonEdge - lastTabEdge) / 2;
        wikiLabel.Y = _tabBackground.Height / 2;
        _tabBackground.AddChild(wikiLabel);

        _closeButton = new CutEdgeRect(new CutEdgeConfig {
            Width = 21,
            Height = 21,
            CutX = 4,
            CutY = 4,
            Cuts = CutEdges.All,
            Color = 0x555555
        });
        _closeButton.SetAnchor(UiAnchor.RightTop);
        _closeButton.X = _tabBackground.Width - TabHeightPadding;
        _closeButton.Y = 2;
        _closeButton.MouseEnabled = true;
        _closeButton.AddEventListener(MouseEvent.LeftClick, OnCloseClicked);
        _tabBackground.AddChild(_closeButton);

        _closeText = new SimpleText(new TextConfig {
            FontSize = 16,
            Color = 0xFFFFFFFFu,
            FontType = FontType.Bold,
            Anchor = UiAnchor.Middle
        });
        _closeText.SetText("X");
        _closeText.X = _closeButton.Width / 2;
        _closeText.Y = _closeButton.Height / 2;
        _closeButton.AddChild(_closeText);

        var bgWidth = (int)_background.SelfContentWidth;
        var bgHeight = (int)_background.SelfContentHeight;
        var pageClip = new Container(new ContainerConfig {
            Width = bgWidth,
            Height = bgHeight,
            EnableClip = true
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

    private CutEdgeRect CreateTabButton(string label, int index) {
        var button = new CutEdgeRect(new CutEdgeConfig {
            Width = TabButtonWidth,
            Height = TabButtonHeight,
            CutX = 4,
            CutY = 4,
            Cuts = CutEdges.All,
            Color = 0x444444
        });

        button.X = TabHeightPadding + index * (TabButtonWidth + TabButtonSpacing);
        button.Y = (_tabBackground.Height - TabButtonHeight) / 2;
        button.MouseEnabled = true;

        var text = new SimpleText(new TextConfig {
            FontSize = 10,
            Color = 0xFFFFFFFFu,
            FontType = FontType.Bold,
            Anchor = UiAnchor.Middle
        });
        text.SetText(label);
        text.X = TabButtonWidth / 2;
        text.Y = TabButtonHeight / 2;
        button.AddChild(text);

        return button;
    }

    private Container CreatePage() {
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

        page.Visible = true;
        _activePage = page;

        Logger.Info(_activePage.X);
        Logger.Info(_activePage.Y);
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
        var screenWidth = (int)(width - HudView.Width * Stage.ScreenScale.X);

        var knownW = (int)((_background.X + _background.SelfContentWidth + Padding) * Stage.ScreenScale.X);
        var knownH = (int)((_background.Y + _background.SelfContentHeight + Padding) * Stage.ScreenScale.Y);

        _screenContainer.X = (int)(screenWidth / 2f - knownW / 2f);
        _screenContainer.Y = (int)(height / 2f - knownH / 2f);
    }
}