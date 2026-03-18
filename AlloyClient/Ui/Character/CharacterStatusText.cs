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
    
    private Entity _owner;
    private string _text;
    private uint _color;
    private double _lifetime;
    private int _offsetTime;
    private double _startTime;

    public CharacterStatusText(Entity en, string text, uint color, int lifetime, int offsetTime) {
        _owner = en;
        _text = text;
        _color = color;
        _lifetime = lifetime;
        _offsetTime = offsetTime;

        var txtConfig = new TextConfig {
            Text = _text,
            Color = _color,
            MaxWidth = 120,
            FontSize = 20,
            OutlineColor = 0x000000,
            OutlineThickness = 0.5f 
        };
        var txt = new SimpleText(txtConfig);
        AddChild(txt);

        SetAnchor(UiAnchor.MiddleBottom);
        AddEventListener(Event.AddedToStage, () => { AddEventListener(Event.EnterFrame, OnFrameEnter); });
        AddEventListener(Event.RemovedFromStage, () => { RemoveEventListener(Event.EnterFrame, OnFrameEnter); });
    }
    
    private void OnFrameEnter() {
        if (_owner == null || !Map.Entities.ContainsKey(_owner.ObjectId)) 
        {
            Parent?.RemoveChild(this);
            return;
        }

        var gameTime = Stage.GameTime;
        var currentTime = gameTime.TotalMs;
        
        if (_startTime == 0) _startTime = currentTime + _offsetTime;
        if (currentTime < _startTime) return;
        
        var elapsedTime = currentTime - _startTime;
        if (elapsedTime >= _lifetime) {
            Parent?.RemoveChild(this);
            return;
        }
        
        Scale = new Vector2(Settings.CameraZoom);
        
        var matrix = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
        var worldPos = new Vector3(_owner.Position.X, _owner.Position.Y, _owner.Z);
        var ndc = Vector3.TransformPerspective(worldPos, matrix);

        float screenX = ((ndc.X + 1.0f) / 2.0f) * Settings.ScreenWidth;
        float screenY = ((1.0f - ndc.Y) / 2.0f) * Settings.ScreenHeight;

        var drift = (elapsedTime / _lifetime) * MaxDrift * Settings.CameraZoom;
        float startOffset = -25f * Settings.CameraZoom;

        X = (int)MathF.Round(screenX);
        Y = (int)MathF.Round((float)(screenY + startOffset - drift));
        
        var remainingLifetime = _lifetime - elapsedTime;
        Alpha = (float)(remainingLifetime / _lifetime);
    }
}