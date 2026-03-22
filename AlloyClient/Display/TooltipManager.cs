using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.Core;

namespace AlloyClient.Display;

public sealed class TooltipManager : Sprite {

    private static TooltipManager _instance;
    private static Tooltip _current;

    public TooltipManager() {
        _instance = this;
        AddEventListener(Event.AddedToStage, () => {
            Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        });
        AddEventListener(Event.RemovedFromStage, () => {
            Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
        });
    }

    private static void OnResize(ResizeEvent args) {
        if (_current != null)
            _current.Scale = Stage.ScreenScale;
    }

    public static void AddTooltip(Tooltip tooltip) {
        if (_current != null)
            _instance.RemoveChild(_current);

        _current = tooltip;
        if (_current != null) {
            _current.Scale = Stage.ScreenScale;
            _instance.AddChild(_current);
        }
    }
    
    public static void RemoveTooltip(Tooltip tooltip) {
        if (_current != tooltip) return;
        _instance.RemoveChild(_current);
        _current = null;
    }
}