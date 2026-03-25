using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud.Panels;

public class TradeRequestPanel : Panel {
    private readonly string _playerName;
    private readonly double _startTime;
    private const double WaitTime = 20000;

    public TradeRequestPanel(string playerName) {
        _playerName = playerName;
        _startTime = Stage.GameTime.TotalMs;

        var title = new SimpleText(new TextConfig {
            X = PanelWidth / 2,
            Y = 16,
            Text = $"{playerName} wants to trade",
            FontSize = 16,
            FontType = FontType.Bold,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.MiddleTop,
            Glow = 0
        });
        AddChild(title);

        var rejectBtn = new TextButton(new TextButtonConfig {
            Text = "Reject",
            FontSize = 16,
            OnClicked = Reject,
            X = PanelWidth / 4,
            Y = PanelHeight - 20,
            Anchor = UiAnchor.MiddleBottom
        });
        AddChild(rejectBtn);

        var acceptBtn = new TextButton(new TextButtonConfig {
            Text = "Accept",
            FontSize = 16,
            OnClicked = Accept,
            X = 3 * PanelWidth / 4,
            Y = PanelHeight - 20,
            Anchor = UiAnchor.MiddleBottom
        });
        AddChild(acceptBtn);

        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void Accept() {
        var pkt = RequestTrade.CreatePacket();
        pkt.Name = _playerName;
        Client.QueuePacket(pkt);
        Close();
    }

    private void Reject() {
        Close();
    }

    private void Close() {
        InteractPanel.AddOverride.Dispatch(null);
    }

    private void OnFrameEnter() {
        if (Stage.GameTime.TotalMs - _startTime > WaitTime) {
            Close();
        }
    }
}