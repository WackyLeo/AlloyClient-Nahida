using AlloyClient.Game.Components.Hud.Inventory;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using Common;

namespace AlloyClient.Game.Components.Hud;

public sealed class HudView : Sprite {
    public const int Width = 240;

    private Minimap _minimap;
    private CharacterDetails _details;
    private CharacterBars _bars;

    private EquippedGrid _equippedGrid;
    private InventoryGrid _inventoryGrid;
    private TabStrip _tabStrip;

    private InteractPanel _interactPanel;

    public HudView() {
        SetAnchor(UiAnchor.MiddleRight);

        var bg = new SdfRect(new SdfRectConfig {
            Width = Width,
            Height = Settings.DefaultScreenHeight,
            Cut = 0,
            Color = 0xFF363636u, 
            OutlineSize = 0f,
            ShadowRadius = 3f, 
            ShadowAlpha = 0.3f
        });
        AddChild(bg);

        _minimap = new Minimap();
        _minimap.X = 5;
        _minimap.Y = 5;
        AddChild(_minimap);

        _details = new CharacterDetails();
        _details.X = 5;
        _details.Y = _minimap.Y + _minimap.Height;
        AddChild(_details);

        _bars = new CharacterBars();
        _bars.X = 10;
        _bars.Y = _details.Y + _details.Height;
        AddChild(_bars);
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    public void CreatePlayerDependentAssets() {
        RemoveChild(_equippedGrid);
        RemoveChild(_inventoryGrid);
        RemoveChild(_interactPanel);
        RemoveChild(_tabStrip);

        _equippedGrid = new EquippedGrid(Map.LocalPlayer);
        _equippedGrid.X = 12;
        _equippedGrid.Y = _bars.Y + CharacterBars.TotalHeight;
        AddChild(_equippedGrid);

        _tabStrip = new TabStrip(Map.LocalPlayer); //Inv + Backpack + StatView is handled under TabStrip.
        _tabStrip.X = 8;
        _tabStrip.Y = _equippedGrid.Y + _equippedGrid.Height + 32;
        AddChild(_tabStrip);

        _interactPanel = new InteractPanel();
        _interactPanel.X = 8;
        _interactPanel.Y = Settings.DefaultScreenHeight - 115;
        AddChild(_interactPanel);
    }

    private void OnFrameEnter() {
        if (Map.LocalPlayer == null) return;

        _bars.Update();
        if (_equippedGrid != null) {
            _equippedGrid.UpdateAbilitySlot();
        }

        if (_interactPanel != null) {
            _interactPanel.Update();
        }
    }
}