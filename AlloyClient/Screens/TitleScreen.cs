using System;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.Screens.Components;
using AlloyClient.Screens.Components.Containers;
using AlloyClient.Ui.Components.Buttons;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Screens;

public class TitleScreen : TitleScreenBase {
    public const int PlayFontSize = 57;
    public const int FontSize = 35;

    private readonly Container _container = new();
    private readonly SimpleText _developerText;

    // References for our corner text so we can resize them
    private readonly SimpleText _versionText;
    private Vector2 _menuAnchor;
    private Vector2 _menuExactPos;

    public TitleScreen() : base(true) {
        var currentY = 50;
        var gap = 15;

        var play = new MenuBarButton("PLAY", PlayFontSize, OnPlay);
        play.X = -play.Width / 2;
        play.Y = currentY;
        _container.AddChild(play);
        currentY += FontSize + gap + 15;

        var servers = new MenuBarButton("SERVERS", FontSize, () => ScreenManager.FadeTo(new ServersTitleScreen()));
        servers.X = -servers.Width / 2;
        servers.Y = currentY;
        _container.AddChild(servers);
        currentY += servers.Height + gap;

        var legends = new MenuBarButton("LEGENDS", FontSize, () => ScreenManager.FadeTo(new LegendsTitleScreen()));
        legends.X = -legends.Width / 2;
        legends.Y = currentY;
        _container.AddChild(legends);
        currentY += legends.Height + gap;

        var credits = new MenuBarButton("CREDITS", FontSize, () => { });
        credits.X = -credits.Width / 2;
        credits.Y = currentY;
        _container.AddChild(credits);
        currentY += credits.Height + gap;

        var editor = new MenuBarButton("EDITOR", FontSize, () => { });
        editor.X = -editor.Width / 2;
        editor.Y = currentY;
        _container.AddChild(editor);
        currentY += editor.Height + gap;

        var exit = new MenuBarButton("EXIT", FontSize, () => { Main.GameInstance.Exit(); });
        exit.X = -exit.Width / 2;
        exit.Y = currentY;
        _container.AddChild(exit);
        AddChild(_container);

        _versionText = new SimpleText(new TextConfig {
            Text = "Ica's Realm: 1.0.0",
            FontSize = 16,
            FontType = FontType.Bold,
            OutlineThickness = 0.0f,
            Glow = 0.33f,
            OutlineColor = 0xFFFFFF,
            Color = 0xFFFFFF
        });
        AddChild(_versionText);
        _developerText = new SimpleText(new TextConfig {
            Text = "Developed by @HaloCelsius",
            FontSize = 16,
            FontType = FontType.Bold,
            OutlineThickness = 0.0f,
            Glow = 0.33f,
            OutlineColor = 0xFFFFFF,
            Color = 0xFFFFFF
        });
        AddChild(_developerText);
    }

    public override void Draw(GameTime gameTime) {
        var screenCenter = new Vector2(Stage.StageWidth / 2f, Stage.StageHeight / 2f);

        float mouseX = Stage.GetRelativeMousePosition().X;
        float mouseY = Stage.GetRelativeMousePosition().Y;

        var mouseOffsetX = screenCenter.X - mouseX;
        var mouseOffsetY = screenCenter.Y - mouseY;

        var targetX = _menuAnchor.X + mouseOffsetX * 0.012f;
        var targetY = _menuAnchor.Y + mouseOffsetY * 0.012f;

        _menuExactPos.X += (targetX - _menuExactPos.X) * 0.05f;
        _menuExactPos.Y += (targetY - _menuExactPos.Y) * 0.05f;

        _container.X = (int)_menuExactPos.X;
        _container.Y = (int)_menuExactPos.Y;

        base.Draw(gameTime);
    }

    protected override void OnResize(ResizeEvent args) {
        var uniformScale = Math.Min(Stage.ScreenScale.X, Stage.ScreenScale.Y);
        _container.Scale = new Vector2(uniformScale);

        _menuAnchor.X = args.Width / 2f;
        _menuAnchor.Y = args.Height * 0.45f;

        _menuExactPos = _menuAnchor;
        _container.X = (int)_menuExactPos.X;
        _container.Y = (int)_menuExactPos.Y;

        _versionText.Scale = new Vector2(uniformScale);
        _developerText.Scale = new Vector2(uniformScale);
        var padding = (int)(5 * uniformScale);

        _versionText.X = padding;
        _versionText.Y = args.Height - _versionText.Height - padding;

        _developerText.X = args.Width - _developerText.Width - padding;
        _developerText.Y = args.Height - _developerText.Height - padding;

        base.OnResize(args);
    }

    private void OnPlay() {
        if (GlobalData.Contains<LoginData>()) {
            ScreenManager.FadeTo(new CharacterListScreen());
        } else {
            var login = new LoginContainer();
            login.AddEventListener("loginSuccess", Overlay.OnLogin);
            OverlayManager.Set(login);
        }
    }
}