using System;
using AlloyClient.Display;
using AlloyClient.State;
using AlloyClient.UiLib.Core;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Components.Panels;

public class Overlay : Sprite {
    protected Overlay() {
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
    }

    public virtual bool InputBlocker => true;

    public virtual void CloseOverlay() {
        OverlayManager.Clear();
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent(ResizeEvent.Resize, Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }

    private void OnResize(ResizeEvent args) {
        var uniformScale = Math.Min(
            (float)args.Width / Settings.DefaultScreenWidth,
            (float)args.Height / Settings.DefaultScreenHeight
        );

        Scale = new Vector2(uniformScale);
        X = (int)((args.Width - Settings.DefaultScreenWidth * uniformScale) / 2f);
        Y = (int)((args.Height - Settings.DefaultScreenHeight * uniformScale) / 2f);
    }
}