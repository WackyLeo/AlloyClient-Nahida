using AlloyClient.Game.Objects;
using AlloyClient.Ui.DailyQuest;
using AlloyClient.Ui.Storage;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Panels;

public class StoragePanel : Panel {

    public StoragePanel() {
        var fullText = new SimpleText(new TextConfig {
            X = PanelWidth / 2,
            Y = 18,
            Text = "Storage",
            FontSize = 20,
            FontType = FontType.Bold,
            OutlineColor = 0x000000,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.MiddleTop,
            Glow = 0
        });
        AddChild(fullText);

        var enterButton = new TextButton(new TextButtonConfig {
            Text = "Open",
            FontSize = 20,
            OnClicked = OnInteractKey,
            FontType = FontType.Bold,
            X = PanelWidth / 2,
            Y = 56,
            Anchor = UiAnchor.MiddleTop
        });
        AddChild(enterButton);
    }

    protected override void OnInteractKey() {
        var storage = new StorageWindow();
        Stage.AddChild(storage);
        UserInput.SetInputDisabled(true);
    }
}