using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.Core;

namespace AlloyClient.Display;

public sealed class TooltipManager : Sprite {
    private static TooltipManager _instance;
    private static Tooltip _current;

    public TooltipManager() {
        _instance = this;

        AddEventListener(Event.AddedToStage, () => {
            if (Stage != null)
                Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        });

        AddEventListener(Event.RemovedFromStage, () => {
            if (Stage != null)
                Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
        });
    }

    private static void OnResize(ResizeEvent args) {
        if (_current != null && _instance?.Stage != null)
            _current.Scale = Stage.ScreenScale;
    }

    public static void AddTooltip(Tooltip tooltip) {
        if (_instance == null || _instance.Stage == null)
            return;

        var stage = _instance.Stage;

        if (_instance.Parent != stage)
            stage.AddChild(_instance);
        else
            stage.SetChildIndex(_instance, stage.NumChildren - 1);

        if (_current != null)
            _instance.RemoveChild(_current);

        _current = tooltip;

        if (_current != null) {
            _instance.AddChild(_current);
            if (_instance.Stage != null)
                _current.Scale = Stage.ScreenScale;
        }
    }

    public static void RemoveTooltip(Tooltip tooltip) {
        if (_instance == null || _current != tooltip)
            return;

        _instance.RemoveChild(_current);
        _current = null;
    }
}