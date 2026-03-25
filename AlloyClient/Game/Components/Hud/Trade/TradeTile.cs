using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Display;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Game.Components.Hud.Trade;

public class TradeTile : Sprite {
    private const int Size = 48;

    private readonly ObjectRect _sprite;
    private readonly CutEdgeRect _bg;
    private readonly CutEdgeRect _selectedHighlight;
    private readonly CutEdgeRect _darkOverlay;
    private readonly CutEdgeRect _darkBorder;

    private EquipmentToolTip _tooltip;
    private ItemDesc _itemDesc;

    public bool Selected;
    public bool Tradeable;

    public TradeTile(CutEdges cuts) {
        MouseEnabled = true;

        _bg = new CutEdgeRect(new CutEdgeConfig {
            Width = Size, Height = Size, CutX = 4, CutY = 4, Color = 0x3A3A3Au, Cuts = cuts
        });
        AddChild(_bg);

        _selectedHighlight = new CutEdgeRect(new CutEdgeConfig {
            Width = Size, Height = Size, CutX = 4, CutY = 4, Color = 0xFFCD57u, Cuts = cuts
        });
        _selectedHighlight.Alpha = 1f;
        _selectedHighlight.Visible = false;
        _selectedHighlight.MouseEnabled = false;
        AddChild(_selectedHighlight);

        _sprite = new ObjectRect(new ObjectRectConfig { Width = Size, Height = Size });
        _sprite.MouseEnabled = true;
        AddChild(_sprite);

        _darkOverlay = new CutEdgeRect(new CutEdgeConfig {
            Width = Size, Height = Size, CutX = 4, CutY = 4, Color = 0xFEFE8Eu, Cuts = cuts
        });
        _darkOverlay.Alpha = 0.55f;
        _darkOverlay.Visible = false;
        _darkOverlay.MouseEnabled = false;
        AddChild(_darkOverlay);

        _darkBorder = new CutEdgeRect(new CutEdgeConfig {
            Width = Size, Height = Size, CutX = 4, CutY = 4, Color = 0x000000u, Cuts = cuts
        });
        _darkBorder.Alpha = 0.65f;
        _darkBorder.Visible = false;
        _darkBorder.MouseEnabled = false;
        AddChild(_darkBorder);

        _sprite.AddEventListener(MouseEvent.MouseOver, OnMouseOver);
        _sprite.AddEventListener(MouseEvent.MouseOut, OnMouseOut);
    }

    public void SetItem(ushort objectType, bool tradeable, bool included) {
        Tradeable = tradeable;
        _itemDesc = objectType > 0 ? ObjectLibrary.GetItem(objectType) : null;

        SetSelected(included);

        if (objectType > 0) {
            _sprite.ChangeTexture(TextureHelper.FromGameAtlas(objectType));
            _sprite.Visible = true;
        } else {
            _sprite.Visible = false;
        }

        _darkBorder.Visible = !Tradeable;

        if (_tooltip != null) {
            TooltipManager.RemoveTooltip(_tooltip);
            _tooltip = null;
        }
    }

    public void SetSelected(bool selected) {
        Selected = selected;
        _bg.SetColor(selected ? 0xFFCD57u : 0x545454);
        _selectedHighlight.Visible = selected; 
        _darkOverlay.Visible = false; 
    }
    
    private void OnMouseOver() {
        if (_itemDesc == null) return;
        _tooltip = new EquipmentToolTip(_itemDesc);
        TooltipManager.AddTooltip(_tooltip);
    }

    private void OnMouseOut() {
        if (_tooltip == null) return;
        TooltipManager.RemoveTooltip(_tooltip);
        _tooltip = null;
    }
}