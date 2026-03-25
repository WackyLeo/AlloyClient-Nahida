using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;

namespace AlloyClient.Ui.Components.Tooltips;

public abstract class Tooltip : Sprite {
    private SdfRect _background;
    protected CutEdgeRect Outline;
    private readonly Container _contain;

    public int ToolWidth;
    public int ToolHeight;
    private readonly uint _colour;
    private readonly uint _outlineColour;

    protected Tooltip(int width, int height, uint colour = 0xFF363636u, uint outlineColour = 0xFF676767u) {
        TooltipMode = true;

        ToolWidth = width;
        ToolHeight = height;
        _outlineColour = outlineColour;
        _colour = colour;

        _contain = new Container();
        AddChild(_contain);
    }

    public virtual void DrawSprite() {
        if (_background != null) _contain.RemoveChild(_background);

        _background = new SdfRect(new SdfRectConfig {
            Width = ToolWidth,
            Height = ToolHeight,
            Cut = 4,
            Color = _colour, 
            OutlineColor = _outlineColour,
            OutlineSize = 2f,
            ShadowRadius = 3f, 
            ShadowAlpha = 0.3f
        });

        _contain.AddChildAt(_background, 0); 
    }
}