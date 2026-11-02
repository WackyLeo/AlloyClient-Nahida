using AlloyClient.Game.Components.Hud.Panels;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Signals;

namespace AlloyClient.Game.Components.Hud;

public sealed class InteractPanel : Sprite {
    public static readonly SingleSignal<Panel> AddOverride = new();

    private readonly PartyPanel _partyPanel = new();

    private Entity _currentObject;

    private Panel _currentPanel;

    private Panel _overridePanel;

    public InteractPanel() {
        AddOverride.Set(SetOverride);
    }

    private void SetOverride(Panel panel) {
        if (_overridePanel != null)
            RemoveChild(_overridePanel);

        _overridePanel = panel;

        if (_overridePanel != null) {
            if (_currentPanel != null)
                _currentPanel.Visible = false;

            AddChild(_overridePanel);
        } else {
            if (_currentPanel != null)
                _currentPanel.Visible = true;
        }
    }


    public void Update() {
        if (_overridePanel != null) return;

        var obj = EntityUtils.FindClosestEntityInRadius(Map.LocalPlayer, Map.InteractiveObjects.Values, 1f);

        if (obj == null) {
            _currentObject = null;
            Map.CurrentObject  = null;
            Map.CurrentInvPanel = null;
            SetPanel(_partyPanel);
        } else if (obj != _currentObject || _currentPanel == null) {
            _currentObject = obj;
            var panel = GetInteractPanel(_currentObject);
            Map.CurrentObject  = _currentObject;
            Map.CurrentInvPanel = panel as ContainerPanel;
            SetPanel(panel);
        }

        _partyPanel.Refresh();
    }

    private void SetPanel(Panel panel) {
        RemoveChild(_currentPanel);
        _currentPanel = panel;

        if (_currentPanel == null)
            return;

        AddChild(_currentPanel);
    }

    public static bool IsInteractiveObject(Entity entity) {
        return entity.Properties.Class switch {
            "Container" => true,
            "OneWayContainer" => true,
            "Portal" => true,
            "Button" => true,
            "TributeButton" => true,
            "CurseButton" => true,
            "CurseObject" => true,
            "TributeObjectOption" => true,
            "MarketNPC" => true,
            "StorageNPC" => true,
            "StorageBuyNPC" => true,
            "WikiAlter" => true,
            "Merchant" => true,
            "QuestRewards" => true,
            _ => false
        };
    }

    private static Panel GetInteractPanel(Entity entity) {
        if (entity == null)
            return null;

        return entity.Properties.Class switch {
            "Container" => new ContainerPanel(entity, false),
            "OneWayContainer" => new ContainerPanel(entity, true),
            "Portal" => new PortalPanel(entity),
            "Button" => null,
            "TributeButton" => new TributeButtonPanel(entity),
            "CurseButton" => null,
            "CurseObject" => null,
            "QuestRewards" => new QuestPanel(),
            "TributeObjectOption" => new TributeOptionPanel(entity),
            "MarketNPC" => null,
            "StorageNPC" => new StoragePanel(),
            "StorageBuyNPC" => new StoragePanel(),
            "WikiAlter" => new WikiPanel(),
            "Merchant" => null,
            _ => null
        };
    }
}