using System;
using AlloyClient.State;
using AlloyClient.Ui;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Hud;

public class StatusBar : Sprite {
    private readonly int width;
    private readonly int height;

    private NineSliceRect mainBar;
    private NineSliceRect backgroundBar;
    private NineSliceRect outlineBar;
    private SimpleText label;
    private SimpleText valueText;
    
    public int CurrentHeight;
    public string labelString;
    public bool drawLabel;
    private bool mouseOver;

    public StatusBar(int width, int height, uint color, uint backColor, uint outlineColor, string label) {
        this.width = width;
        this.height = height;
        CurrentHeight = height;
        
        backgroundBar = new NineSliceRect(new NineSliceConfig { SliceData = SliceLibrary.StatusBar, CutX = 10, CutY = 10, Width = this.width, Height = this.height });
        backgroundBar.SetColor(backColor);
        AddChild(backgroundBar);

        mainBar = new NineSliceRect(new NineSliceConfig { SliceData = SliceLibrary.StatusBar, CutX = 10, CutY = 10, Width = this.width, Height = this.height });
        mainBar.SetColor(color);
        AddChild(mainBar);

        labelString = label;
        this.label = new SimpleText(new TextConfig { Text = label, FontSize = 16, FontType = FontType.Bolder, X = 4, Y = this.height / 2, OutlineThickness = 0f, Color = 0xFFFFFF, Glow = 0, OutlineColor = outlineColor, Anchor = UiAnchor.MiddleLeft });
        AddChild(this.label);
        
        valueText = new SimpleText(new TextConfig { Text = "", FontSize = 16, FontType = FontType.Bold, Y = this.height / 2, OutlineThickness = 0f, Color = 0xFFFFFF, OutlineColor = outlineColor, Anchor = UiAnchor.MiddleLeft });
        AddChild(valueText);

        AddEventListener(MouseEvent.MouseOver, OnMouseOver);
        AddEventListener(MouseEvent.MouseOut, OnMouseOut);

        drawLabel = true;
    }

    public void Update(int val, int max, int boost = 0, int baseMax = -1, int level = -1) {
        if (max > 0)
            mainBar.Resize((int)(width * Math.Clamp(val / (float)max, 0f, 1f)), CurrentHeight);
        else
            mainBar.Resize(width, CurrentHeight);

        valueText.Visible = false;

        if ((mouseOver || Settings.ToggleBarText) && drawLabel)
            UpdateText(val, max, boost, baseMax, level);
    }
    
    public void SetSize(int w, int h) {
        CurrentHeight = h;

        backgroundBar.Resize(w, h);

        if (label != null) label.Y = CurrentHeight / 2 + 1;
        if (valueText != null) valueText.Y = CurrentHeight / 2 + 1;
    }

    public void UpdateLabel(string newLabel) {
        if (label != null)
            RemoveChild(label);

        labelString = newLabel;
        label = new SimpleText(new TextConfig {
            Text = newLabel,
            FontSize = 16,
            FontType = FontType.Bolder,
            X = 4,
            Y = CurrentHeight / 2 + 1,
            OutlineThickness = 0,
            Color = 0xFFFFFF,
            OutlineColor = 0x000000,
            Glow = 0,
            Anchor = UiAnchor.MiddleLeft
        });
        AddChild(label);
    }

    private void UpdateText(int val, int max, int boost, int baseMax, int level) {
        valueText.Visible = true;

        var ltmt = "";

        if (Settings.ToggleLeftToMax) {
            var ltm = baseMax - max - boost;
            if (level >= 20 && ltm > 0)
                ltmt = $"|{Math.Ceiling(ltm / 5f)}";
        }

        if (max > 0)
            valueText.SetText($"{val}/{max}" + ltmt);
        else
            valueText.SetText($"{val}");
        valueText.X = width / 2 - valueText.Width / 2;
    }

    private void OnMouseOver() => mouseOver = true;
    private void OnMouseOut() => mouseOver = false;
}