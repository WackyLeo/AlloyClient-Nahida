using AlloyClient.AppEngine;
using AlloyClient.Display;
using AlloyClient.State;
using AlloyClient.Ui.Components.Dialogs;
using AlloyClient.Ui.Components.Panels;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Extra;
using AlloyClient.Models;

namespace AlloyClient.Screens.Components.Containers;

public class RegisterContainer : Overlay {
    
    private readonly TextInput _emailInput;
    private readonly TextInput _passwordInput;
    
    public RegisterContainer() {
        var cx = Settings.DefaultScreenWidth / 2;
        var cy = Settings.DefaultScreenHeight / 2;

        var background = new ColorRect(new ColorRectConfig
            { Width = 475, Height = 350, Color = 0x363636, X = cx, Y = cy, Anchor = UiAnchor.Middle });
        AddChild(background);

        var titleBackground = new ColorRect(new ColorRectConfig
            { Width = 475, Height = 50, Color = 0x4d4d4d, X = cx, Y = cy - 150, Anchor = UiAnchor.Middle });
        AddChild(titleBackground);

        var title = new SimpleText(new TextConfig {
            Text = "Register", FontSize = 22, FontType = FontType.Bold, X = cx, Y = cy - 150, Color = 0xFFFFFF,
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

        var loginButton = new TextButton(new TextButtonConfig {
            Text = "Existing user? Click here to login!", FontSize = 16,
            OnClicked = () => OverlayManager.Set(new LoginContainer()), FontType = FontType.Bold, X = cx,
            Y = cy + 50, Anchor = UiAnchor.Middle
        });
        AddChild(loginButton);

        var registerButton = new TextButton(new TextButtonConfig {
            Text = "Create", FontSize = 28, OnClicked = OnRegister, FontType = FontType.Normal, X = cx + 200, Y = cy + 150,
            Anchor = UiAnchor.Middle
        });
        AddChild(registerButton);

        var cancelButton = new TextButton(new TextButtonConfig {
            Text = "Cancel", FontSize = 28, OnClicked = CloseOverlay, FontType = FontType.Normal, X = cx - 200,
            Y = cy + 150, Anchor = UiAnchor.Middle
        });
        AddChild(cancelButton);
    }
    
    private void OnRegister() {
        AddEventListener(AppRequests.Register(_emailInput.Text, _passwordInput.Text), OnLoginResponse);
    }
    
    private void OnLoginResponse(AppResponse response) {
        if (!response.Success) {
            var dialog = new Dialog("Register Error", response.Message, new DialogOption("Ok"));
            DialogManager.Enqueue(dialog);
            return;
        }
        
        
        CloseOverlay();
        ScreenManager.FadeToScreen(new TitleScreen(), Easing.SineInOut, 500, 0x0);
    }
}