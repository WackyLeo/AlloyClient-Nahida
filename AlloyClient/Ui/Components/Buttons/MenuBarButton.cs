using System;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Components.Buttons;

public sealed class MenuBarButton : TextButton {
    
    private readonly bool _pulse;

    public MenuBarButton(string text, float size, Action callback, bool pulse = false) : base (new TextButtonConfig { Text = text, FontSize = size, OnClicked = callback, OutlineThickness = 1.0f }) {
        _pulse = pulse;
        HookFrameLifecycle();
    }

    public MenuBarButton(TextButtonConfig config, bool pulse = false) : base(config) {
        _pulse = pulse;
        HookFrameLifecycle();
    }

    private void HookFrameLifecycle() {
        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    private void OnAddedToStage() {
        if (_pulse) AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnRemovedFromStage() {
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnFrameEnter() {
        if (!_pulse) return;

        var gameTime = Stage.GameTime;
        var scale = 1.05f + 0.05f * (float)Math.Sin(gameTime.TotalMs / 200);
        Scale = new Vector2(scale);
    }
}

