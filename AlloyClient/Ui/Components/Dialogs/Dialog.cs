using System;
using System.Collections.Generic;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Components.Dialogs;

public sealed record DialogOption(string Text, Action Callback = null);

public enum DialogState {
    Active = 0,
    Closed = 1,
    Finished = 2
}

public class Dialog : Sprite 
{
    private List<Sprite> _sprites = new();
    
    private const int BoxWidth = 300;
    private const int BoxPadding = 10;

    public DialogState State = DialogState.Active;

    public Dialog(string title, string message, DialogOption confirm, DialogOption cancel = null) 
    {
        X = Settings.DefaultScreenWidth / 2;
        Y = Settings.DefaultScreenHeight / 2;

        var boxConfig = new ColorRectConfig 
            { 
                Width = BoxWidth, 
                Height = 75, 
                Color = 0x1C1C1C, 
                Alpha = 1f, 
                Anchor = UiAnchor.Middle
            };
        var box = new ColorRect(boxConfig);
        AddChild(box);

        var titleConfig = new TextConfig
        {
            Text = title, 
            FontSize = 24, 
            FontType = FontType.Bold, 
            Color = 0xFFFFFF, 
            Anchor = UiAnchor.MiddleTop
        };
        var titleText = new SimpleText(titleConfig);
        box.AddChild(titleText);
        _sprites.Add(titleText);

        var messageConfig = new TextConfig
        {
            Text = message, 
            FontSize = 20, 
            Color = 0xFFFFFF, 
            Anchor = UiAnchor.MiddleTop
        };
        var messageText = new SimpleText(messageConfig);
        box.AddChild(messageText);
        _sprites.Add(messageText);

        var confirmConfig = new TextButtonConfig
        {
            Text = confirm.Text, 
            FontSize = 22, 
            OnClicked = () => { confirm.Callback?.Invoke(); State = DialogState.Closed; },
            Anchor = UiAnchor.MiddleBottom
        };
        var confirmButton = new TextButton(confirmConfig);
        box.AddChild(confirmButton);
        _sprites.Add(confirmButton);

        var boxWidth = GetMaxWidth(_sprites);
        var boxHeight = titleText.Height + 10 + messageText.Height + 20 + confirmButton.Height + 20;
        box.Resize(boxWidth, boxHeight);
        box.X = X;
        box.Y = Y;

        titleText.X = box.Width / 2;
        titleText.Y = 10;

        messageText.X = box.Width / 2;
        messageText.Y = titleText.Height + 20;

        confirmButton.Y = box.Height - 10;
        if (cancel != null) 
        {
            confirmButton.X = 3 * box.Width / 4;

            var cancelConfig = new TextButtonConfig
            {
                Text = cancel.Text, 
                FontSize = 22, 
                OnClicked = () => 
                {
                    cancel.Callback?.Invoke(); 
                    State = DialogState.Closed;
                },
                Anchor = UiAnchor.MiddleBottom
            };
            var cancelButton = new TextButton(cancelConfig);
            box.AddChild(cancelButton);

            cancelButton.Y = box.Height - 10;
            cancelButton.X = box.Width / 4;
        } else 
        {
            confirmButton.X = box.Width / 2;
        }
    }

    private int GetMaxWidth(List<Sprite> sprites)
    {
        var maxWidth = BoxPadding * 2;
        var temp = 0;
        foreach (var sprite in sprites)
        {
            temp = sprite.Width > temp ? sprite.Width : temp;
        }
        return maxWidth + temp;
    }
}