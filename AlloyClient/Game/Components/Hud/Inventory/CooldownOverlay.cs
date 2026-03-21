using System;
using AlloyClient.Game.Objects;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Components.Hud.Inventory;

public sealed class CooldownOverlay : Sprite {

    private readonly int _size;
    private readonly Player _player;
    private readonly SimpleText _cdText;
    private readonly ColorRect _darkBg;

    public CooldownOverlay(Player player, int size = 49) {
        _size = size;
        _player = player;

        _darkBg = new ColorRect(new ColorRectConfig {
            Width = size, Height = size, Color = 0x000000, Alpha = 0.7f
        });
        AddChild(_darkBg);

        _cdText = new SimpleText(new TextConfig {
            Text = "",
            FontSize = 18,
            FontType = FontType.Bold,
            Color = 0xFFFFFF,
            OutlineColor = 0x000000,
            OutlineThickness = 0.5f,
            Glow = 0,
            Anchor = UiAnchor.Middle,
            X = size / 2,
            Y = size / 2,
        });
        AddChild(_cdText);

        Visible = false;
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnFrameEnter() {
        if (_player?.Equipment[1] == null) {
            Visible = false;
            return;
        }

        var ability = _player.Equipment[1];
        int cooldownMs = (int)(ability.Cooldown * 1000f);
        if (cooldownMs <= 0) cooldownMs = 550;

        double elapsed = Main.GameTime.TotalMs - _player.LastAltAttack;
        double percentage = Math.Clamp(elapsed / cooldownMs, 0.0, 1.0);

        if (percentage >= 1.0) {
            Visible = false;
            return;
        }

        Visible = true;
        double remaining = cooldownMs * (1.0 - percentage) / 1000.0;
        _cdText.SetText(remaining.ToString("F1") + "s");
    }
}