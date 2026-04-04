using AlloyClient.Game.Objects;
using AlloyClient.Ui.DailyQuest;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Panels;

public class QuestPanel : Panel {

    public QuestPanel() {
        var fullText = new SimpleText(new TextConfig {
            X = PanelWidth / 2,
            Y = 18,
            Text = "Quests",
            FontSize = 20,
            FontType = FontType.Bold,
            OutlineColor = 0x000000,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.MiddleTop,
            Glow = 0
        });
        AddChild(fullText);

        var enterButton = new TextButton(new TextButtonConfig {
            Text = "Redeem",
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
        GameSprite.DailyQuestWindow = new DailyQuestWindow();
        GameSprite.DailyQuestWindow.LoadQuests(Map.DailyQuests);
        Stage.AddChild(GameSprite.DailyQuestWindow);
        UserInput.SetInputDisabled(true);
    }
}