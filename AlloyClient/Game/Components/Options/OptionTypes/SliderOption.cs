using System;
using AlloyClient.State.SettingTypes;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Game.Components.Options.OptionTypes;

public class SliderOption : Option {
    private const int LabelWidth = 60;
    private const int TrackWidth = 420;
    private const int TrackHeight = 8;
    private const int HandleSize = 20;
    private const int OptionHeight = 75;
    private readonly Action<float> _callback;

    private readonly ColorRect _handle;
    private readonly ValueSetting<float> _setting;

    private readonly ColorRect _trackBg;
    private readonly ColorRect _trackFill;
    private readonly SimpleText _valueText;
    private float _currentValue;

    private bool _dragging;

    public SliderOption(ValueSetting<float> setting, string label, string overviewLabel, Action<float> sliderCallback)
        : base(setting, null, null) {
        _setting = setting;
        _callback = sliderCallback;
        _currentValue = Math.Clamp(setting.Value, 0f, 1f);

        var labelText = new SimpleText(new TextConfig {
            Text = label,
            FontSize = 22,
            FontType = FontType.Bold,
            OutlineThickness = 0,
            Glow = 0.5f,
            Color = 0xABABAB,
            X = 0,
            Y = (OptionHeight - 22) / 2
        });
        AddChild(labelText);

        var trackX = LabelWidth;
        var trackY = (OptionHeight - TrackHeight) / 2;

        _trackBg = new ColorRect(new ColorRectConfig {
            X = trackX,
            Y = trackY,
            Width = TrackWidth,
            Height = TrackHeight,
            Color = 0x3A3A3A
        });
        AddChild(_trackBg);

        _trackFill = new ColorRect(new ColorRectConfig {
            X = trackX,
            Y = trackY,
            Width = 0,
            Height = TrackHeight,
            Color = 0xFFAA00
        });
        AddChild(_trackFill);

        _handle = new ColorRect(new ColorRectConfig {
            X = 0,
            Y = (OptionHeight - HandleSize) / 2,
            Width = HandleSize,
            Height = HandleSize,
            Color = 0xFFFFFF
        });
        _handle.SetAnchor(UiAnchor.LeftTop);
        _handle.MouseEnabled = true;
        AddChild(_handle);
        _valueText = new SimpleText(new TextConfig {
            FontSize = 20,
            FontType = FontType.Bold,
            OutlineThickness = 0,
            Glow = 0.5f,
            Color = 0xCCCCCC,
            X = trackX + TrackWidth + 12,
            Y = (OptionHeight - 20) / 2
        });
        AddChild(_valueText);

        var overviewLabelText = new SimpleText(new TextConfig {
            Text = overviewLabel,
            FontSize = 28,
            FontType = FontType.Bold,
            OutlineThickness = 0,
            Glow = 0.5f,
            Color = 0xABABAB,
            X = (trackX + TrackWidth + 12) / 2,
            Y = 10,
            Anchor = UiAnchor.Middle,
        });
        AddChild(overviewLabelText);

        MouseEnabled = true;
        _trackBg.MouseEnabled = true;
        _trackFill.MouseEnabled = true;

        _trackBg.AddEventListener(MouseEvent.LeftDown, OnTrackDown);
        _trackFill.AddEventListener(MouseEvent.LeftDown, OnTrackDown);

        _handle.AddEventListener(MouseEvent.LeftDown, OnHandleDown);

        AddEventListener(Event.AddedToStage, () => {
            Stage.AddEventListener(MouseEvent.MouseMove, OnMouseMove);
            Stage.AddEventListener(MouseEvent.LeftUp, OnDragEnd);
        });
        AddEventListener(Event.RemovedFromStage, () => {
            Stage.RemoveEventListener(MouseEvent.MouseMove, OnMouseMove);
            Stage.RemoveEventListener(MouseEvent.LeftUp, OnDragEnd);
        });

        Refresh();
    }

    private void OnTrackDown(MouseEvent e) {
        if (_disabled) return;
        e.StopPropagation();
        _dragging = true;
        UpdateFromMouseX();
    }

    private void OnHandleDown(MouseEvent e) {
        if (_disabled) return;
        e.StopPropagation();
        _dragging = true;
    }

    private void OnDragEnd(MouseEvent e) {
        _dragging = false;
    }

    private void OnMouseMove(MouseEvent e) {
        if (!_dragging || _disabled) return;
        UpdateFromMouseX();
    }

    private void UpdateFromMouseX() {
        var rel = GetRelativeMousePosition();
        var localX = rel.X / _trueScale.X - LabelWidth;
        _currentValue = Math.Clamp(localX / TrackWidth, 0f, 1f);

        _setting.SetValue(_currentValue);
        _callback?.Invoke(_currentValue);

        UpdateVisuals();
    }

    private void UpdateVisuals() {
        var fillWidth = (int)(_currentValue * TrackWidth);
        var handleX = LabelWidth + fillWidth - HandleSize / 2;

        _trackFill.Resize(Math.Max(0, fillWidth), TrackHeight);
        _handle.X = handleX;
        _valueText.SetText($"{(int)(_currentValue * 100)}%");
    }

    public override void Refresh() {
        _currentValue = Math.Clamp(_setting.Value, 0f, 1f);
        UpdateVisuals();
    }

    public override void SetDisabled(bool val) {
        _disabled = val;
        Alpha = val ? 0.4f : 1f;
    }
}