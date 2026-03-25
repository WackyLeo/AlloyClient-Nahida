using AlloyClient.Game.Components.Hud;
using AlloyClient.State;
using AlloyClient.Ui.Character;
using AlloyClient.Ui.Chat;
using AlloyClient.UiLib.Core;
using AlloyClient.Game.Components.Hud.Chat;
using AlloyClient.Game.Components.Hud.Trade;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Ui.Components.Elements;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components;

public sealed class GameSprite : Sprite {

    public readonly UserInput UserInput;
    public readonly HudView Hud;
    private readonly CurrencyView _currencyView;
    private readonly ChatBox _chat;
    private TradeView _tradeView; 
    
    public GameSprite() {
        AddChild(UserInput = new UserInput());
        AddChild(new ChatLayer());
        AddChild(new NotificationLayer());
        AddChild(Hud = new HudView());
        AddChild(_currencyView = new CurrencyView());
        AddChild(_chat = new ChatBox());
        AddChild(new DebugStats());

        Map.GameSprite = this;
        
        SetPosition(Settings.ScreenWidth, Settings.ScreenHeight);
        
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }
    
    private void OnResize(ResizeEvent args) {
        SetPosition(args.Width, args.Height);
    }

    private void SetPosition(int width, int height) {
        /* FIXME 
         * this fixes bug: first item move drops regardless of target
         * bounds are just wrong until 2nd end drag for some reason
         * would like to fix the root cause
         */
        //todo:SetBaseDimensions(width, height);
        
        Hud.X = width;
        Hud.Y = height / 2;
        Hud.Scale = Stage.ScreenScale;

        _currencyView.X = (int)(width - HudView.Width * Stage.ScreenScale.X - 25);
        _currencyView.Y = 15;
        _currencyView.Scale = Stage.ScreenScale;
        
        _chat.X = 0;
        _chat.Y = height;
        _chat.Scale = Stage.ScreenScale;
        
        PositionTradeView(width, height);
    }
    
    // Trading
    
    public void UpdatePartnerOffer(bool[] offer) {
        _tradeView?.UpdatePartnerOffer(offer);
    }
    
    public void OpenTrade(TradeItem[] myItems, string yourName, TradeItem[] yourItems) {
        if (_tradeView != null) CloseTradeView();

        _tradeView = new TradeView(myItems, yourName, yourItems);
        AddChild(_tradeView);

        PositionTradeView(Stage.StageWidth, Stage.StageHeight);
    }

    public void CloseTradeView() {
        if (_tradeView == null) return;
        RemoveChild(_tradeView);
        _tradeView = null;
    }
    
    private void PositionTradeView(int width, int height) {
        if (_tradeView == null) return;

        var scale = Stage.ScreenScale;
        var hudLeft = width - HudView.Width * scale.X;

        _tradeView.SetAnchor(UiAnchor.RightBottom);
        _tradeView.X = (int)hudLeft - 20;
        _tradeView.Y = (int)(height - 20);
        _tradeView.Scale = scale;
    }
}