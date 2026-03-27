using AlloyClient.AppEngine;
using AlloyClient.Assets;
using AlloyClient.Assets.Libraries;
using AlloyClient.Display;
using AlloyClient.Models;
using AlloyClient.State;
using AlloyClient.Ui.Components.Graphics;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Extra;
using AlloyClient.Utils;
using System.Threading.Tasks;

namespace AlloyClient.Screens;

public class LoadingScreen : Screen {

    private readonly SimpleText _text;
    
    public LoadingScreen() {
        _text = new SimpleText(new TextConfig {
            Text = "Loading...",
            FontSize = 40,
            FontType = FontType.Bold,
            OutlineThickness = 4,
            X = Settings.DefaultScreenWidth / 2,
            Y = Settings.DefaultScreenHeight - 90,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.Middle
        });
        AddChild(_text);
        
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
        ObjectLibrary.SetFallback();

        AddEventListener(Task.WhenAll(
            AppRequests.Startup(),
            //Task.Run(SoundManager.PreLoadSounds),
            //Task.Run(Music.PreLoadSongs),
            AssetParser.LoadAssetsAsync(),
            Task.Delay(2000)
        ), () => { ScreenManager.FadeToScreen(new TitleScreen(), Easing.SineInOut, 1000, 0x0); });
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }
    
    protected override void OnResize(ResizeEvent args) {
        _text.Scale = Stage.ScreenScale;
        _text.X = Stage.StageWidth / 2;
        _text.Y = Stage.StageHeight - (int)(90 * Stage.ScreenScale.Y);
    }
}