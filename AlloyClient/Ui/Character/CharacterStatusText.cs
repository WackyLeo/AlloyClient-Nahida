using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Character;

public class CharacterStatusText : Sprite {
    private const int MaxDrift = 20;
    private readonly uint _color;
    private readonly double _lifetime;
    private readonly int _offsetTime;

    private readonly Entity _owner;
    private double _startTime;
    private readonly string _text;

    public CharacterStatusText(Entity en, string text, uint color, int lifetime, int offsetTime) {
        _owner = en;
        _text = text;
        _color = color;
        _lifetime = lifetime;
        _offsetTime = offsetTime;

        var txtConfig = new TextConfig {
            Text = _text,
            Color = _color,
            MaxWidth = 300,
            FontSize = 25,
            OutlineColor = 0x000000,
            OutlineThickness = 0.5f,
            FontType = FontType.Bold,
            Glow = 0.5f
        };
        var txt = new SimpleText(txtConfig);
        AddChild(txt);

        SetAnchor(UiAnchor.MiddleBottom);
        AddEventListener(Event.AddedToStage, () => {
            AddEventListener(Event.EnterFrame, OnFrameEnter);
        });
        AddEventListener(Event.RemovedFromStage, () => {
            RemoveEventListener(Event.EnterFrame, OnFrameEnter);
        });
    }

    private void OnFrameEnter() {
        if (_owner == null || !Map.Entities.ContainsKey(_owner.ObjectId)) {
            Parent?.RemoveChild(this);
            return;
        }

        var gameTime = Stage.GameTime;
        var currentTime = gameTime.TotalMs;

        if (_startTime == 0)
            _startTime = currentTime + _offsetTime;
        if (currentTime < _startTime)
            return;

        var elapsedTime = currentTime - _startTime;
        if (elapsedTime >= _lifetime) {
            Parent?.RemoveChild(this);
            return;
        }

        Scale = new Vector2(Settings.CameraZoom);

        var k = _owner.Size / 100f;
        var spriteTop = (_owner.RenderBaseType?.Scale.Y ?? 0.5f) * k;

        var matrix = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
        var worldPos = new Vector3(_owner.Position.X, _owner.Position.Y, _owner.Z + spriteTop);
        var ndc = Vector3.TransformPerspective(worldPos, matrix);

        var screenX = (ndc.X + 1.0f) / 2.0f * Settings.ScreenWidth;
        var screenY = (1.0f - ndc.Y) / 2.0f * Settings.ScreenHeight;

        var drift = elapsedTime / _lifetime * MaxDrift * Settings.CameraZoom;
        var startOffset = -4f * Settings.CameraZoom;

        X = (int)MathF.Round(screenX);
        Y = (int)MathF.Round((float)(screenY + startOffset - drift));

        var remainingLifetime = _lifetime - elapsedTime;
        Alpha = (float)(remainingLifetime / _lifetime);
    }
}