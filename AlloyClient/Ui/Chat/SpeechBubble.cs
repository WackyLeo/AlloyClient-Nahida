using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Chat;

public record struct SpeechData(Entity Owner, string Text, string Recipient);

public sealed class SpeechBubble : Sprite {

    private double _lifetime = 5000;
    private readonly Entity _owner;

    public SpeechBubble(SpeechData data) {
        _owner = data.Owner;
        var type = SpeechColors.Default;
        
        switch (data.Recipient) {
            case "*Guild*": type = SpeechColors.Guild; break;
            case "*Party*": type = SpeechColors.Party; break;
            default: {
                if (!string.IsNullOrEmpty(data.Recipient)) { type = SpeechColors.Tell; break; }
                if (_owner.Properties.IsEnemy) type = SpeechColors.Enemy; 
                break;
            }
        }

        var colors = type switch {
            SpeechColors.Enemy => (0x561F1Cu, 0xFC8642u, 0xCDC0BFu),
            SpeechColors.Tell => (0x260AB6u, 0x00F0FFu, 0xD3CCF7u),
            SpeechColors.Guild => (0x3E8A00u, 0xA6FF5Du, 0xD3F7CCu),
            SpeechColors.Party => (0x6428AFu, 0xB478FFu, 0xF0C8E6u),
            _ => (0xE1DFDCu, 0xFFFFFFu, 0x545454u)
        };
        
        const int cut = 10;
        const int outline = 2;
        
        var txt = new SimpleText(new TextConfig { Text = data.Text, FontSize = 15, X = cut, Y = cut, Color = colors.Item3, MaxWidth = 120,OutlineThickness = 0, Glow = 0});
        var rect = new CutEdgeRect(new CutEdgeConfig { Width = cut * 2 + txt.Width, Height = cut * 2 + txt.Height, CutX = cut, CutY = cut, Color = colors.Item1 });
        var outlineRect = new CutEdgeRect(new CutEdgeConfig { Width = (cut + outline) * 2 + txt.Width, Height = (cut + outline) * 2 + txt.Height, CutX = cut + outline / 2, CutY = cut + outline / 2, Color = colors.Item2 }) {
            X = -outline,
            Y = -outline
        };
        
        AddChild(outlineRect);
        AddChild(rect);
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
        if ((_lifetime -= gameTime.ElapsedMs) <= 0) 
        {
            Parent?.RemoveChild(this);
            return;
        }

        Scale = new Vector2(Settings.CameraZoom);

        var k = _owner.Size / 100f;
        var spriteTop = (_owner.RenderBaseType?.Scale.Y ?? 0.5f) * k;

        var matrix = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
        var worldPos = new Vector3(_owner.Position.X, _owner.Position.Y, _owner.Z + spriteTop);
        var ndc = Vector3.TransformPerspective(worldPos, matrix);

        float screenX = ((ndc.X + 1.0f) / 2.0f) * Settings.ScreenWidth;
        float screenY = ((1.0f - ndc.Y) / 2.0f) * Settings.ScreenHeight;

        float extraPixelOffset = -4f * Settings.CameraZoom;
        
        X = (int)MathF.Round(screenX);
        Y = (int)MathF.Round(screenY + extraPixelOffset);
    }
}