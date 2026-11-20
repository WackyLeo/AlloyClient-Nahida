using System;
using AlloyClient.AppEngine;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.Screens.Components.Containers;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Extra;
using AlloyClient.Models;
using AlloyClient.UiLib.Signals;
using Common;

namespace AlloyClient.Screens.Components;

public class AccountOverlay : Sprite
{
    private readonly bool _isTitle;

    private readonly Container _container = new();
    private readonly TitleScreen _titleScreen;

    public static Signal OnVerifyAsyncSuccess = new();
    public static Signal OnVerifyAsyncSuccessLoggedOut = new();
    public static bool Connected = true;

    private double _startTime = Stage.GameTime.TotalMs;
    private int _counter = 10;
    private double _coolDown = 1;
    private bool _loadingStarted = false;

    public AccountOverlay(TitleScreen titleScreen, bool title)
    {
        _titleScreen = titleScreen;
        _isTitle = title;

        AddChild(_container);

        if (GlobalData.Contains<LoginData>())
        {
            CreateLogout();
        }
        else if (Connected)
        {
            CreateLogin();
        }

        AddEventListener(Event.AddedToStage, AddHandlers);
        AddEventListener(Event.RemovedFromStage, RemoveHandlers);
    }

    private void AddHandlers()
    {
        OnVerifyAsyncSuccess.Add(CreateLogout);
        OnVerifyAsyncSuccessLoggedOut.Add(CreateLogin);
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void RemoveHandlers()
    {
        OnVerifyAsyncSuccess.Remove(CreateLogout);
        OnVerifyAsyncSuccessLoggedOut.Remove(CreateLogin);
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
    }

    public void OnLogin()
    {
        GTween.Add(Tween.New(_container, Easing.SineInOut, 150, 0f, EaseType.Alpha, onFinish: () =>
        {
            CreateLogout();
            GTween.Add(Tween.New(_container, Easing.SineInOut, 150, 1f, EaseType.Alpha));
        }));
    }

    private void OnLogout()
    {
        GlobalData.Logout();

        if (_isTitle)
        {
            GTween.Add(Tween.New(_container, Easing.SineInOut, 150, 0f, EaseType.Alpha, onFinish: () =>
            {
                CreateLogin();
                GTween.Add(Tween.New(_container, Easing.SineInOut, 150, 1f, EaseType.Alpha));
            }));
        }
        else
        {
            ScreenManager.FadeToScreen(new TitleScreen(), Easing.SineInOut, 500, 0x0);
        }
    }

    public async void OnRegister()
    {
        var result = await AppRequests.GetCharList();
        await Assets.RemoteAssetLoader.LoadAllAsync();

        if (!result.Success)
        {
            ScreenManager.FadeToScreen(new TitleScreen(), Easing.SineInOut, 1000, 0x0);
        }
        else
            GTween.Add(Tween.New(_container, Easing.SineInOut, 150, 0f, EaseType.Alpha, onFinish: () =>
            {
                CreateLogout();
                GTween.Add(Tween.New(_container, Easing.SineInOut, 150, 1f, EaseType.Alpha));
            }));
    }

    private void CreateLogin()
    {
        /*if (_titleScreen._playButton.Visible)
        {
            GTween.Add(Tween.New(_titleScreen._playButton, Easing.SineInOut, 150, 1f, EaseType.Alpha, onFinish: () =>
            {
                _titleScreen._playButton.Visible = false;
                GTween.Add(Tween.New(_titleScreen._playButton, Easing.SineInOut, 150, 0f, EaseType.Alpha));
            }));
        }*/

        _container.RemoveChildren();

        var newConfig = new TextConfig
        {
            Text = "new account - ", FontSize = 24, FontType = FontType.Normal, Color = 0xB3B3B3
        };
        var newText = new SimpleText(newConfig);
        _container.AddChild(newText);

        var registerConfig = new TextButtonConfig
        {
            Text = "register",
            FontSize = 24,
            OnClicked = () =>
            {
                var register = new RegisterContainer();
                register.AddEventListener("registerSuccess", OnRegister);
                OverlayManager.Set(register);
            },
            FontType = FontType.Bold,
            X = newText.Width
        };
        var registerButton = new TextButton(registerConfig);
        _container.AddChild(registerButton);

        var dashConfig = new TextConfig
        {
            Text = " - ", FontSize = 24, X = registerButton.X + registerButton.Width, Color = 0xB3B3B3
        };
        var dashText = new SimpleText(dashConfig);
        _container.AddChild(dashText);

        var loginConfig = new TextButtonConfig
        {
            Text = "login",
            FontSize = 24,
            OnClicked = () =>
            {
                var login = new LoginContainer();
                login.AddEventListener("loginSuccess", OnLogin);
                OverlayManager.Set(login);
            },
            FontType = FontType.Bold,
            X = dashText.X + dashText.Width
        };
        var loginButton = new TextButton(loginConfig);
        _container.AddChild(loginButton);
    }

    private void CreateLogout()
    {
        /*GTween.Add(Tween.New(_titleScreen._playButton, Easing.SineInOut, 150, 0f, EaseType.Alpha, onFinish: () =>
        {
            _titleScreen._playButton.Visible = true;
            GTween.Add(Tween.New(_titleScreen._playButton, Easing.SineInOut, 150, 1f, EaseType.Alpha));
        }));*/

        _container.RemoveChildren();

        var account = GlobalData.Get<AccountData>();

        if (account == null) // We're not properly logged in
        {
            if (!Connected)
            {
                CreateLogin();
            }

            return;
        }

        var nameConfig = new TextConfig { Text = $"logged in as {account.Name} - ", FontSize = 24 };
        var nameText = new SimpleText(nameConfig);
        _container.AddChild(nameText);

        var logoutConfig = new TextButtonConfig
        {
            Text = "logout", FontSize = 24, OnClicked = OnLogout, X = nameText.Width
        };
        var logoutButton = new TextButton(logoutConfig);
        _container.AddChild(logoutButton);
    }

    private void CreateLoading()
    {
        if ((int)((Stage.GameTime.TotalMs - _startTime) / 1000) >= _coolDown)
        {
            _container.RemoveChildren();

            var connectingMessage = new TextConfig { Text = $"Attempting to connect in {_counter} ", FontSize = 24 };
            var nameText = new SimpleText(connectingMessage);
            _container.AddChild(nameText);
            _counter = _counter <= 0 ? 10 : _counter - 1;
            _startTime = Stage.GameTime.TotalMs;
        }
    }

    private void OnFrameEnter()
    {
        if (Connected) return;

        CreateLoading();
    }
}