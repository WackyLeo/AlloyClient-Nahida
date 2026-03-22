using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;

namespace AlloyClient.Ui.Components.Tooltips;

public abstract class Tooltip : Sprite {

    private CutEdgeRect _background;
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
        if (_outline != null) _contain.RemoveChild(_outline);

        int outlineSize = 2;
        int cut = 4;

        _background = new CutEdgeRect(new CutEdgeConfig {
            Width = ToolWidth,
            Height = ToolHeight,
            CutX = cut,
            CutY = cut,
            Color = 0xFF363636u, 
        });

        _outline = new CutEdgeRect(new CutEdgeConfig {
            Width = ToolWidth + (outlineSize * 2),
            Height = ToolHeight + (outlineSize * 2),
            CutX = cut + (outlineSize / 2),
            CutY = cut + (outlineSize / 2),
            Color = 0x676767
        }) {
            X = -outlineSize,
            Y = -outlineSize
        };

        _contain.AddChildAt(_background, 0); 
        _contain.AddChildAt(_outline, 0);
    }
}