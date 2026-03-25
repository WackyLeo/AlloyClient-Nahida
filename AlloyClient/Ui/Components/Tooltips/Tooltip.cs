using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;

namespace AlloyClient.Ui.Components.Tooltips;

public abstract class Tooltip : Sprite {

    private SdfRect _background;
    private CutEdgeRect _outline;
    private readonly Container _contain;

    public int ToolWidth;
    public int ToolHeight;

    protected Tooltip(int width, int height) {
        TooltipMode = true;

        ToolWidth = width;
        ToolHeight = height;

        _contain = new Container();
        AddChild(_contain);
    }

    public virtual void DrawSprite() {
        if (_background != null) _contain.RemoveChild(_background);

        _background = new SdfRect(new SdfRectConfig {
            Width = ToolWidth,
            Height = ToolHeight,
            Cut = 4,
            Color = 0x363636u, 
            OutlineColor = 0x676767u,
            OutlineSize = 2f,
            ShadowRadius = 3f, 
            ShadowAlpha = 0.3f
        });

        _contain.AddChildAt(_background, 0); 
    }
}