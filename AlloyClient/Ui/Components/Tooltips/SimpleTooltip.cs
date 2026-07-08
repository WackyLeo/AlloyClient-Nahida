using System;
using AlloyClient.UiLib;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Input;

namespace AlloyClient.Ui.Components.Tooltips;

public sealed class SimpleTooltip : Tooltip {
    private const int Pad = 10;
    private const int FontSize = 13;
    private const uint TextColor = 0xB3B3B3u;

    public SimpleTooltip(string message, uint tintColor = 0u) : base(10, 10,
        tintColor != 0u ? Darken(tintColor) : 0xFF1A1A1Au,
        tintColor != 0u ? DarkenOutline(tintColor) : 0xFF676767u) {
        var label = new SimpleText(EquipmentToolTip.SimpleConfig(
            message, FontSize, FontType.Normal,
            TextColor, 0x000000u,
            0f, 280));
        label.X = Pad;
        label.Y = Pad;
        AddChild(label);

        ToolWidth = label.Width + Pad * 2;
        ToolHeight = label.Height + Pad * 2;
        DrawSprite();

        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    private void OnAddedToStage() {
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnRemovedFromStage() {
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnFrameEnter() {
        var mouse = MouseInput.GetMousePosition();
        var s = Stage.ScreenScale;
        X = Math.Clamp(mouse.X - (int)(ToolWidth * s.X) - 6,
            0, UiRender.Screen.X - (int)(ToolWidth * s.X));
        Y = Math.Clamp(mouse.Y + 14,
            0, UiRender.Screen.Y - (int)(ToolHeight * s.Y));
    }

    private static uint Darken(uint argb) {
        var a = (argb >> 24) & 0xFF;
        var r = (uint)(((argb >> 16) & 0xFF) * 0.30f);
        var g = (uint)(((argb >> 8) & 0xFF) * 0.30f);
        var b = (uint)((argb & 0xFF) * 0.30f);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    private static uint DarkenOutline(uint argb) {
        var a = (argb >> 24) & 0xFF;
        var r = (uint)(((argb >> 16) & 0xFF) * 0.55f);
        var g = (uint)(((argb >> 8) & 0xFF) * 0.55f);
        var b = (uint)((argb & 0xFF) * 0.55f);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
}

