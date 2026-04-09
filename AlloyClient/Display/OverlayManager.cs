using AlloyClient.State;
using AlloyClient.Ui.Components.Panels;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.Utils;

namespace AlloyClient.Display;

public sealed class OverlayManager : Sprite {
    private static readonly ColorRect Overlay = new(new ColorRectConfig
        { Width = Settings.DefaultScreenWidth, Height = Settings.DefaultScreenHeight, Color = 0x2B2B2B, Alpha = 0.8f });

    private static OverlayManager Instance;

    private Sprite _current;
    private int _generation;

    public OverlayManager() {
        Instance = this;
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
    }

    public static void Set(Overlay sprite) {
        Instance.PrivateSet(sprite);
    }

    public static void Clear() {
        Instance.PrivateClear();
    }

    private void PrivateSet(Overlay sprite) {
        var gen = ++_generation;

        if (_current is null) {
            AddChild(Overlay);
            Overlay.AddAlphaTween(Overlay.Alpha, 0.8f, 250);

            _current = sprite;
            AddChild(_current);
            _current.AddAlphaTween(0f, 1f, 250);
        } else {
            var outgoing = _current;
            _current = sprite;

            outgoing.AddAlphaTween(outgoing.Alpha, 0f, 150, onFinish: () => {
                if (gen != _generation) return;
                RemoveChild(outgoing);
                AddChild(_current);
                _current.AddAlphaTween(0f, 1f, 150);
            });
        }
    }

    private void PrivateClear() {
        var gen = ++_generation;

        Overlay.AddAlphaTween(Overlay.Alpha, 0f, 250, onFinish: () => {
            if (gen != _generation) return;
            RemoveChild(Overlay);
        });

        if (_current is null) return;

        var outgoing = _current;
        _current = null;

        outgoing.AddAlphaTween(outgoing.Alpha, 0f, 175, onFinish: () => {
            if (gen != _generation) return;
            RemoveChild(outgoing);
        });
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }

    private void OnResize(ResizeEvent args) {
        if (_current is not null) {
            _current.X = Stage.StageWidth / 2;
            _current.Y = Stage.StageHeight / 2;
            _current.Scale = Stage.ScreenScale;
        }

        Overlay.Resize(args.Width, args.Height);
    }
}