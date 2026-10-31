using System;
using AlloyClient.UiLib;
using AlloyClient.UiLib.Core;
using Common;

namespace AlloyClient.Display;

public static class DisplayManager {
    private static int _lastLog = -1;
    private static Stage _stage;

    public static void Init(Stage stage) {
        if (_stage != null)
            return;
        
        _stage = stage;
        _stage.AddChild(ScreenManager.FadeScreen);
        _stage.AddChild(new ScreenManager());
        _stage.AddChild(new OverlayManager());
        _stage.AddChild(new DialogManager());
        _stage.AddChild(new TooltipManager());
    }
    
    public static void Update(GameTime gameTime) {
        ScreenManager.Update(gameTime);
        _stage.Update(gameTime);
    }

    public static void Draw(GameTime gameTime) {
        Console.WriteLine("DisplayManager.Draw running");   // temporarily; it prints every frame, so stop the app quickly
        UiRender.LastRenderCount = 0;
        ScreenManager.Draw(gameTime);
        _stage.Draw(gameTime);
        var sec = (int)(gameTime.TotalMs / 1000.0);
        if (sec != _lastLog) {
            _lastLog = sec;
            Console.WriteLine($"[{sec}s] {ScreenManager.DebugState()} sprites={UiRender.LastRenderCount}");
        }
    }
    
}