using AlloyClient.AppEngine;
using AlloyClient.Display;
using AlloyClient.State;
using AlloyClient.Ui.Components.Dialogs;
using AlloyClient.Ui.Components.Panels;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Screens.Components.Containers;

public class LoginContainer : Overlay {
    private readonly TextInput _emailInput;
    private readonly TextInput _passwordInput;

    public LoginContainer() {
        var cx = Settings.DefaultScreenWidth / 2;
        var cy = Settings.DefaultScreenHeight / 2;

        var background = new ColorRect(new ColorRectConfig
            { Width = 475, Height = 350, Color = 0x363636, X = cx, Y = cy, Anchor = UiAnchor.Middle });
        AddChild(background);

        var titleBackground = new ColorRect(new ColorRectConfig
            { Width = 475, Height = 50, Color = 0x4d4d4d, X = cx, Y = cy - 150, Anchor = UiAnchor.Middle });
        AddChild(titleBackground);

        var title = new SimpleText(new TextConfig {
            Text = "Log in", FontSize = 22, FontType = FontType.Bold, X = cx, Y = cy - 150, Color = 0xFFFFFF,
            Anchor = UiAnchor.Middle
        });
        AddChild(title);

        _emailInput = new TextInput(new InputConfig {
            X = cx, Y = cy - 50, FontSize = 24, FontType = FontType.Bold, Color = 0xFFFFFF, Width = 350,
            DefaultText = "Email", Anchor = UiAnchor.Middle
        });
        AddChild(_emailInput);

        _passwordInput = new TextInput(new InputConfig {
            X = cx, Y = cy + 10, FontSize = 24, FontType = FontType.Bold, Color = 0xFFFFFF, Width = 350,
            DefaultText = "Password", Password = true, Anchor = UiAnchor.Middle
        });
        AddChild(_passwordInput);

        var registerButton = new TextButton(new TextButtonConfig {
            Text = "New user? Click here to Register!", FontSize = 16,
            OnClicked = () => OverlayManager.Set(new RegisterContainer()), FontType = FontType.Bold, X = cx,
            Y = cy + 50, Anchor = UiAnchor.Middle
        });
        AddChild(registerButton);

        var loginButton = new TextButton(new TextButtonConfig {
            Text = "Log in", FontSize = 28, OnClicked = OnLogin, FontType = FontType.Normal, X = cx + 200, Y = cy + 150,
            Anchor = UiAnchor.Middle
        });
        AddChild(loginButton);

        var cancelButton = new TextButton(new TextButtonConfig {
            Text = "Cancel", FontSize = 28, OnClicked = CloseOverlay, FontType = FontType.Normal, X = cx - 200,
            Y = cy + 150, Anchor = UiAnchor.Middle
        });
        AddChild(cancelButton);
    }

    private void OnLogin() {
        AddEventListener(AppRequests.VerifyAsync(_emailInput.Text, _passwordInput.Text, true), OnLoginResponse);
    }

    private void OnLoginResponse(AppResponse response) {
        if (!response.Success) {
            var dialog = new Dialog("Login Error", response.Message, new DialogOption("Ok"));
            DialogManager.Enqueue(dialog);
            return;
        }

        CloseOverlay();
        DispatchEvent(new Event("loginSuccess"));
    }
}