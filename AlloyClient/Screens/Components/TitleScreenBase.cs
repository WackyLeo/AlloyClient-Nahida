using System;
using AlloyClient.Display;
using AlloyClient.State;
using AlloyClient.Ui.Components.Buttons;
using AlloyClient.Ui.Components.Graphics;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Screens.Components;

public abstract class TitleScreenBase : Screen {
    private readonly ColorRect _darken = new(new ColorRectConfig {
        Width = Settings.DefaultScreenWidth, Height = Settings.DefaultScreenHeight, Color = 0x2B2B2B, Alpha = 0.8f
    });

    private readonly MusicButton _music = new(new MusicButtonConfig { X = 7, Y = 7, Width = 32, Height = 32 });

    public readonly AccountOverlay Overlay;
    private Vector2 _textAnchor;
    private Vector2 _textExactPos;

    private readonly SimpleText? _titleText;

    protected TitleScreenBase(bool title = false) 
    {
        AddChild(new ScreenGraphic());
        if (title) {
            _titleText = new SimpleText(new TextConfig {
                Text = "meow",
                FontSize = 120,
                FontType = FontType.Bold,
                OutlineThickness = 1.0f,
                Color = 0xFFFFFF,
                Glow = 0.33f,
                Anchor = UiAnchor.Middle
            });
            AddChild(_titleText);
        } else {
            AddChild(_darken);
        }

        AddChild(_music);

        //Todo guild/stars
        
        Overlay = new AccountOverlay(title);
        Overlay.X = Settings.DefaultScreenWidth - 10;
        Overlay.Y = 10;
        Overlay.SetAnchor(UiAnchor.RightTop);
        AddChild(Overlay);

        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }

    public override void Draw(GameTime gameTime) {
        var screenCenter = new Vector2(Stage.StageWidth / 2f, Stage.StageHeight / 2f);

        if (_titleText != null) {
            float mouseX = Stage.GetRelativeMousePosition().X;
            float mouseY = Stage.GetRelativeMousePosition().Y;

            var mouseOffsetX = screenCenter.X - mouseX;
            var mouseOffsetY = screenCenter.Y - mouseY;

            var targetX = _textAnchor.X + mouseOffsetX * 0.008f;
            var targetY = _textAnchor.Y + mouseOffsetY * 0.008f;

            _textExactPos.X += (targetX - _textExactPos.X) * 0.05f;
            _textExactPos.Y += (targetY - _textExactPos.Y) * 0.05f;

            _titleText.X = (int)_textExactPos.X;
            _titleText.Y = (int)_textExactPos.Y;
        }

        base.Draw(gameTime);
    }

    protected override void OnResize(ResizeEvent args) {
        _darken.Resize(args.Width, args.Height);
        _music.Scale = Stage.ScreenScale;
        Overlay.Scale = Stage.ScreenScale;
        Overlay.X = args.Width - (int)(10 * Stage.ScreenScale.X);
        Overlay.Y = (int)(10 * Stage.ScreenScale.Y);

        if (_titleText != null) {
            var uniformScale = Math.Min(Stage.ScreenScale.X, Stage.ScreenScale.Y);
            _titleText.Scale = new Vector2(uniformScale);
            var basePlacementY = args.Height * 0.25f;

            _textAnchor.X = args.Width / 2f;
            _textAnchor.Y = basePlacementY;

            _textExactPos = _textAnchor;

            _titleText.X = (int)_textExactPos.X;
            _titleText.Y = (int)_textExactPos.Y;
        }
    }
}