using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Panels;

public class TributeOptionPanel : Panel {
    private readonly TextButton _enterButton;

    private readonly SimpleText _fullText;

    private readonly Entity _owner;

    public TributeOptionPanel(Entity entity) {
        _owner = entity;
        if (_owner.AltItemID <= 0) return;

        _fullText = new SimpleText(new TextConfig {
            X = PanelWidth / 2,
            Y = 18,
            Text = _owner.AltItemID.ToString(),
            FontSize = 20,
            FontType = FontType.Bold,
            OutlineColor = 0x000000,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.MiddleTop,
            Glow = 0
        });
        AddChild(_fullText);

        _enterButton = new TextButton(new TextButtonConfig {
            Text = "Enter",
            FontSize = 20,
            OnClicked = OnInteractKey,
            FontType = FontType.Bold,
            X = PanelWidth / 2,
            Y = 56,
            Anchor = UiAnchor.MiddleTop
        });
        AddChild(_enterButton);

        AddEventListener(Event.AddedToStage, () => { AddEventListener(Event.EnterFrame, OnFrameEnter); });
        AddEventListener(Event.RemovedFromStage, () => { RemoveEventListener(Event.EnterFrame, OnFrameEnter); });
    }

    protected override void OnInteractKey() {
        var pkt = ButtonClick.CreatePacket();
        pkt.Position.X = (int)_owner.Position.X;
        pkt.Position.Y = (int)_owner.Position.Y;
        Client.QueuePacket(pkt);
    }

    private void OnFrameEnter() {
        if (_owner.AltItemID <= 0) {
            RemoveChild(_enterButton);
            AddChild(_fullText);
        }
    }
}