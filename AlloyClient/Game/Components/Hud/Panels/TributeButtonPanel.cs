using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Panels;

public class TributeButtonPanel : Panel {
    private readonly Entity _owner;

    public TributeButtonPanel(Entity entity) {
        _owner = entity;

        var fullText = new SimpleText(new TextConfig {
            X = PanelWidth / 2,
            Y = 18,
            Text = "Tribute",
            FontSize = 20,
            FontType = FontType.Bold,
            OutlineColor = 0x000000,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.MiddleTop,
            Glow = 0
        });
        AddChild(fullText);

        var enterButton = new TextButton(new TextButtonConfig {
            Text = "Offer Tribute",
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
        var pkt = ButtonClick.CreatePacket();
        pkt.Position.X = (int)_owner.Position.X;
        pkt.Position.Y = (int)_owner.Position.Y;
        Client.QueuePacket(pkt);
    }
}