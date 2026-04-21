using System;
using System.Collections.Generic;
using AlloyClient.Game.Objects;
using AlloyClient.UiLib;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Input;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Components.Tooltips;

public class PlayerGroupToolTip : Tooltip {
    public readonly List<Player> Players;

    public PlayerGroupToolTip(List<Player> players) : base(180, 50, 0xBF161616u) {
        Players = new List<Player>(players);
        TooltipMode = false;

        var y = 10;
        foreach (var p in players) {
            var portrait = new ObjectRect(new ObjectRectConfig {
                Texture = TextureHelper.Create(p.Texture, TextureType.GameAtlas),
                Width = 24, Height = 24, GlowEnabled = true, OutlineEnabled = true
            });
            portrait.X = 10;
            portrait.Y = y;
            AddChild(portrait);

            var nameColor = p.IsFellowGuild ? 0x00FF00u : 0xFCDF00u;
            var nameText = new SimpleText(EquipmentToolTip.SimpleConfig(
                p.Name ?? "Unknown", 13, FontType.Bold, nameColor,
                0x000000, glow: 1.0f, thickness: 0.5f));
            nameText.X = 38;
            nameText.Y = y + 4;
            AddChild(nameText);

            y += 28;
        }

        var hint = new SimpleText(EquipmentToolTip.SimpleConfig(
            "(Click to select player)", 11, FontType.Normal, 0xB3B3B3u,
            0x000000, glow: 1.0f, thickness: 0.5f));
        hint.X = 10;
        hint.Y = y;
        AddChild(hint);

        if (players.Count > 1) y += 6;

        ToolWidth = 180;
        ToolHeight = y + (players.Count == 1 ? 20 : 10);
        DrawSprite();

        AddEventListener(Event.EnterFrame, () => {
            var mouse = MouseInput.GetMousePosition();
            var s = Stage.ScreenScale;
            X = mouse.X - (int)(ToolWidth * s.X);
            Y = mouse.Y;
            X = Math.Clamp(X, 0, UiRender.Screen.X - (int)(ToolWidth * s.X));
            Y = Math.Clamp(Y, 0, UiRender.Screen.Y - (int)(ToolHeight * s.Y));
        });
    }

    public bool Matches(List<Player> other) {
        if (Players.Count != other.Count) return false;
        for (var i = 0; i < Players.Count; i++)
            if (Players[i].ObjectId != other[i].ObjectId)
                return false;
        return true;
    }
}