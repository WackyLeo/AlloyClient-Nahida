using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using Common;

namespace AlloyClient.Game.Components.Hud.Trade;

public class TradeView : Sprite {
    public const int TradeWidth = 220;
    public const int TradeHeight = 420;

    private readonly TradeGrid _myGrid;
    private readonly TradeGrid _yourGrid;

    public TradeView(TradeItem[] myItems, string yourName, TradeItem[] yourItems) {
        var bg = new SdfRect(new SdfRectConfig {
            Width = TradeWidth, 
            Height = TradeHeight, 
            Color = 0xFF363636u, 
            OutlineColor = 0xFF676767u,
            Cut = 6, 
            Cuts = CutEdges.All,
            OutlineSize = 2f,
            ShadowRadius = 3f,
            ShadowAlpha = 0.3f
        });
        bg.MouseEnabled = false;
        AddChild(bg);
        
        var yourTitle = new SimpleText(new TextConfig {
            Text = $"{yourName}'s Offer:", FontSize = 16, FontType = FontType.Bold, X = 10, Y = 10
        });
        AddChild(yourTitle);

        _yourGrid = new TradeGrid(yourItems, false);
        _yourGrid.X = 10;
        _yourGrid.Y = 32;
        AddChild(_yourGrid);

        var myTitle = new SimpleText(new TextConfig {
            Text = "Your Offer:", FontSize = 16, FontType = FontType.Bold, X = 10, Y = 196
        });
        AddChild(myTitle);

        _myGrid = new TradeGrid(myItems, true, OnOfferChanged);
        _myGrid.X = 10;
        _myGrid.Y = 218;
        AddChild(_myGrid);

        var cancelBtn = new TextButton(new TextButtonConfig {
            Text = "Cancel", FontSize = 16, X = TradeWidth / 4, Y = TradeHeight - 20,
            Anchor = UiAnchor.MiddleBottom,
            OnClicked = () => { Client.QueuePacket(CancelTrade.CreatePacket()); Close(); }
        });
        AddChild(cancelBtn);

        var acceptBtn = new TextButton(new TextButtonConfig {
            Text = "Accept", FontSize = 16, X = 3 * TradeWidth / 4, Y = TradeHeight - 20,
            Anchor = UiAnchor.MiddleBottom,
            OnClicked = () => {
                var pkt = AcceptTrade.CreatePacket();
                pkt.MyOffer = _myGrid.GetOfferState();
                pkt.YourOffer = _yourGrid.GetOfferState();
                Client.QueuePacket(pkt);
            }
        });
        AddChild(acceptBtn);
    }

    private void OnOfferChanged(bool[] myOffer) {
        var pkt = ChangeTrade.CreatePacket();
        pkt.Offers = myOffer;
        Client.QueuePacket(pkt);
    }

    public void UpdatePartnerOffer(bool[] partnerOffer) {
        _yourGrid.SetOfferState(partnerOffer);
    }

    public void Close() {
        Parent?.RemoveChild(this);
    }
}