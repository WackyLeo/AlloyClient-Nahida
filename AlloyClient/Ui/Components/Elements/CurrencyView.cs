using AlloyClient.Assets;
using AlloyClient.Game;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;
using Common.Structs;

namespace AlloyClient.Ui.Components.Elements;

public sealed class CurrencyView : Sprite {
    
    private readonly SimpleText _creditsText;
    private readonly SimpleText _fameText;
    private readonly SimpleText _tributeText;

    private const int RowHeight = 30;
    private const int IconSize = 24;
    private const int Padding = 20;
    private const float Outline = 0.5f;
    private const int TextWidth = 70;

    public CurrencyView() {
        SetAnchor(UiAnchor.RightTop);
        
        _fameText = MakeText(TextWidth + Padding - 4, 4);
        var fameIcon = MakeIcon("lofiObj3", 224, TextWidth + Padding, 2);
        AddChild(_fameText);
        AddChild(fameIcon);

        _tributeText = MakeText(TextWidth + Padding - 4, RowHeight + 4);
        var tributeIcon = MakeIcon("SR8x8items2", 228, TextWidth + Padding, RowHeight + 2);
        AddChild(_tributeText);
        AddChild(tributeIcon);

        _creditsText = MakeText(TextWidth + Padding - 4, RowHeight * 2 + 4);
        var creditsIcon = MakeIcon("lofiObj3", 225, TextWidth + Padding, RowHeight * 2 + 2);
        AddChild(_creditsText);
        AddChild(creditsIcon);

        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    private static ObjectRect MakeIcon(string sheet, int index, int x, int y) {
        return new ObjectRect(new ObjectRectConfig {
            X = x,
            Y = y,
            Width = IconSize,
            Height = IconSize,
            Texture = TextureHelper.FromGameAtlas(sheet, index),
            Anchor = UiAnchor.LeftTop
        });
    }

    private static SimpleText MakeText(int x, int y) {
        return new SimpleText(new TextConfig {
            X = x,
            Y = y,
            FontSize = 22,
            FontType = FontType.Bold,
            OutlineThickness = Outline,
            Anchor = UiAnchor.RightTop
        });
    }

    
    private void OnAddedToStage() {
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnRemovedFromStage() {
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnFrameEnter() {
        var player = Map.LocalPlayer;
        if (player == null) return;

        _creditsText.SetText(player.Credits.ToString());
        _fameText.SetText(player.Fame.ToString());
        _tributeText.SetText(player.Tribute.ToString());
    }
}

