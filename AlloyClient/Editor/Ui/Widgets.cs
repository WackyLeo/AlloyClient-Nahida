using System;
using System.Collections.Generic;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Editor;

public static class EditorStyle {
    public const uint PanelColor = 0xE0303030;
    public const uint PanelOutline = 0x5A5A5A;
    public const uint TitleColor = 0xB9A960; // the original editor's gold
    public const uint TextColor = 0xB2B2B2;
    public const uint DimTextColor = 0x777777;
    public const uint ButtonColor = 0xF0454545;
    public const uint ButtonHoverColor = 0xF0585858;
    public const uint ButtonSelectedColor = 0xF0706638;

    public static SimpleText Text(string text, float size = 13, uint color = TextColor, bool bold = false, int maxWidth = -1) {
        return new SimpleText(new TextConfig {
            Text = text,
            FontSize = size,
            FontType = bold ? FontType.Bold : FontType.Normal,
            Color = color,
            OutlineColor = 0x0,
            OutlineThickness = 0f,
            Glow = 0.3f,
            MaxWidth = maxWidth
        });
    }
}

/// <summary>Rounded dark background that also blocks mouse input from reaching the map below.</summary>
public class EditorPanel : Sprite {
    private readonly SdfRect _background;
    public int PanelWidth { get; private set; }
    public int PanelHeight { get; private set; }

    public EditorPanel(int width, int height, uint color = EditorStyle.PanelColor, bool blockMouse = true) {
        PanelWidth = width;
        PanelHeight = height;
        _background = new SdfRect(new SdfRectConfig {
            Width = width, Height = height, Color = color, OutlineColor = EditorStyle.PanelOutline,
            OutlineSize = 1f, ShadowRadius = 6f, ShadowAlpha = 0.45f, Cut = 3, MouseEnabled = blockMouse
        });
        AddChild(_background);
    }

    public void Resize(int width, int height) {
        PanelWidth = Math.Max(1, width);
        PanelHeight = Math.Max(1, height);
        _background.Resize(PanelWidth, PanelHeight);
    }
}

public sealed class EditorButton : Sprite {
    private readonly SdfRect _background;
    private readonly SimpleText _label;
    private readonly int _width;
    private readonly int _height;
    private bool _hover;
    private bool _down;
    private bool _selected;

    public Action OnClick;
    public Action OnHover;
    public Action OnHoverEnd;

    public EditorButton(string text, Action onClick = null, int width = -1, float fontSize = 13, int height = -1) {
        OnClick = onClick;
        _label = EditorStyle.Text(text, fontSize, 0xFFFFFF, true);
        _width = width > 0 ? width : _label.Width + 16;
        _height = height > 0 ? height : (int)(fontSize * 1.6f) + 4;

        _background = new SdfRect(new SdfRectConfig {
            Width = _width, Height = _height, Color = EditorStyle.ButtonColor, OutlineColor = 0x6A6A6A,
            OutlineSize = 1f, ShadowRadius = 2f, ShadowAlpha = 0.4f, Cut = 3
        });
        AddChild(_background);
        AddChild(_label);
        CenterLabel();

        MouseEnabled = true;
        AddEventListener(MouseEvent.MouseOver, OnMouseOver);
        AddEventListener(MouseEvent.MouseOut, OnMouseOut);
        AddEventListener(MouseEvent.LeftDown, OnLeftDown);
        AddEventListener(MouseEvent.LeftUp, OnLeftUp);
    }

    public int ButtonWidth => _width;
    public int ButtonHeight => _height;

    public void SetText(string text) {
        _label.SetText(text);
        CenterLabel();
    }

    private void CenterLabel() {
        _label.X = (_width - _label.Width) / 2;
        _label.Y = (_height - _label.Height) / 2;
    }

    public void SetSelected(bool selected) {
        _selected = selected;
        UpdateColor();
    }

    private void UpdateColor() {
        var c = _selected ? EditorStyle.ButtonSelectedColor : _hover ? EditorStyle.ButtonHoverColor : EditorStyle.ButtonColor;
        _background.SetColor(c & 0xFFFFFF, (c >> 24) / 255f);
    }

    private void OnMouseOver() {
        _hover = true;
        UpdateColor();
        OnHover?.Invoke();
    }

    private void OnMouseOut() {
        _hover = false;
        _down = false;
        UpdateColor();
        OnHoverEnd?.Invoke();
    }

    private void OnLeftDown() => _down = true;

    private void OnLeftUp() {
        if (_down) OnClick?.Invoke();
        _down = false;
    }
}

public sealed class EditorCheckBox : Sprite {
    private const int BoxSize = 12;
    private readonly ColorRect _check;
    private bool _down;

    public bool Value { get; private set; }
    public Action<bool> OnChange;

    public EditorCheckBox(string text, bool value, Action<bool> onChange = null) {
        OnChange = onChange;
        var box = new ColorRect(new ColorRectConfig { Width = BoxSize, Height = BoxSize, Color = 0x1E1E1E });
        var border = new QuadBatch(TextureType.Color);
        border.AddOutline(0, 0, BoxSize, BoxSize, 1, 0xFF8A8A8A);
        border.Commit();
        _check = new ColorRect(new ColorRectConfig { X = 3, Y = 3, Width = BoxSize - 6, Height = BoxSize - 6, Color = EditorStyle.TitleColor });
        AddChild(box);
        AddChild(border);
        AddChild(_check);

        if (!string.IsNullOrEmpty(text)) {
            var label = EditorStyle.Text(text);
            label.X = BoxSize + 5;
            label.Y = (BoxSize - label.Height) / 2;
            AddChild(label);
        }

        SetValue(value);
        MouseEnabled = true;
        AddEventListener(MouseEvent.LeftDown, () => { _down = true; });
        AddEventListener(MouseEvent.MouseOut, () => { _down = false; });
        AddEventListener(MouseEvent.LeftUp, OnLeftUp);
    }

    public void SetValue(bool value) {
        Value = value;
        _check.Visible = value;
    }

    private void OnLeftUp() {
        if (!_down) return;
        _down = false;
        SetValue(!Value);
        OnChange?.Invoke(Value);
    }
}

/// <summary>Label followed by a text box.</summary>
public sealed class EditorInput : Sprite {
    private readonly TextInput _input;
    private readonly bool _numeric;
    private readonly int _maxLength;
    private bool _settingText;

    public Action<string> OnChange;

    public EditorInput(string label, int inputWidth, string initial = "", bool numeric = false, int maxLength = 64, float fontSize = 13,
        Action<string> onChange = null) {
        _numeric = numeric;
        _maxLength = maxLength;
        OnChange = onChange;

        var x = 0;
        SimpleText labelText = null;
        if (!string.IsNullOrEmpty(label)) {
            labelText = EditorStyle.Text(label, fontSize);
            AddChild(labelText);
            x = labelText.Width + 6;
        }

        _input = new TextInput(new InputConfig {
            X = x, Width = inputWidth, FontSize = fontSize, Color = 0xFFFFFF, OutlineThickness = 0,
            MaxCharacters = (byte)Math.Clamp(maxLength, 1, 255), DefaultText = "", OnChange = HandleChange,
            OnFocus = () => { Focused = true; }, OnUnfocus = () => { Focused = false; }
        });
        AddChild(_input);

        if (labelText != null) labelText.Y = Math.Max(0, (_input.Height - labelText.Height) / 2);
        if (!string.IsNullOrEmpty(initial)) SetText(initial);
    }

    public string Text => _input.Text;

    public int IntValue => int.TryParse(Text.Trim(), out var v) ? v : 0;

    public bool Focused { get; private set; }

    public void SetText(string text) {
        if (Text == text) return;
        _settingText = true;
        _input.UnFocus(true);
        if (!string.IsNullOrEmpty(text)) _input.SetText(text);
        _settingText = false;
    }

    public void Focus() => _input.Focus();

    public void Unfocus() => _input.UnFocus();

    private void HandleChange() {
        if (_settingText) return;
        var text = Text;
        if (_numeric) {
            var digits = new System.Text.StringBuilder();
            foreach (var c in text)
                if (char.IsDigit(c))
                    digits.Append(c);
            if (digits.Length != text.Length) {
                SetText(digits.ToString());
                return;
            }
        }

        if (text.Length > _maxLength) {
            SetText(text[.._maxLength]);
            return;
        }

        OnChange?.Invoke(text);
    }
}

/// <summary>Row of buttons where exactly one is selected.</summary>
public sealed class EditorSwitch : Sprite {
    private readonly List<EditorButton> _buttons = [];
    public int Selected { get; private set; }
    public Action<int> OnSwitch;

    public EditorSwitch(int buttonWidth, params string[] options) {
        var x = 0;
        for (var i = 0; i < options.Length; i++) {
            var idx = i;
            var b = new EditorButton(options[i], () => Select(idx, true), buttonWidth, 12);
            b.X = x;
            x += b.ButtonWidth + 3;
            _buttons.Add(b);
            AddChild(b);
        }

        Select(0, false);
    }

    public void Select(int idx, bool notify) {
        if (idx < 0 || idx >= _buttons.Count) return;
        Selected = idx;
        for (var i = 0; i < _buttons.Count; i++) _buttons[i].SetSelected(i == idx);
        if (notify) OnSwitch?.Invoke(idx);
    }

    public void SelectNext() => Select((Selected + 1) % _buttons.Count, true);
}

/// <summary>Follows the mouse. Shown by the editor when hovering tools/elements.</summary>
public sealed class EditorTooltip {
    private Ui.Components.Tooltips.SimpleTooltip _current;
    private string _text;
    private object _owner;

    // Uses the game's own tooltip (follows the mouse, drawn above everything by the TooltipManager).
    // The owner is whatever is hovered: the ui lib sends "mouse over" for the new sprite *before* "mouse out" for the old
    // one, so a stale mouse out must not hide the tooltip the newly hovered sprite just showed.
    public void Show(string text, object owner) {
        _owner = owner;
        if (_current != null && _text == text) return;
        HideCurrent();
        _text = text;
        _current = new Ui.Components.Tooltips.SimpleTooltip(text);
        Display.TooltipManager.AddTooltip(_current);
    }

    public bool IsOwner(object owner) => ReferenceEquals(owner, _owner);

    /// <param name="owner">Only hides if this is still the owner. Null hides unconditionally.</param>
    public void Hide(object owner = null) {
        if (owner != null && !ReferenceEquals(owner, _owner)) return;
        _owner = null;
        HideCurrent();
    }

    private void HideCurrent() {
        if (_current == null) return;
        Display.TooltipManager.RemoveTooltip(_current);
        _current = null;
        _text = null;
    }
}

/// <summary>Centered window with a title, rows of content and OK/Cancel buttons.</summary>
public class EditorWindow : EditorPanel {
    private const int Padding = 10;
    private readonly SimpleText _title;
    private readonly List<Sprite> _rows = [];
    protected readonly EditorButton OkButton;
    protected readonly EditorButton CancelButton;

    public Action OnOk;
    public Action OnCancel;

    public EditorWindow(string title, string okText = "OK", string cancelText = "Cancel") : base(100, 100) {
        _title = EditorStyle.Text(title, 18, 0xFFFFFF, true);
        _title.X = Padding;
        _title.Y = Padding;
        AddChild(_title);

        OkButton = new EditorButton(okText, () => Submit(), 80);
        CancelButton = new EditorButton(cancelText, () => Close(true), 80);
        AddChild(OkButton);
        AddChild(CancelButton);
        Visible = false;
    }

    protected T AddRow<T>(T row) where T : Sprite {
        _rows.Add(row);
        AddChild(row);
        Layout();
        return row;
    }

    protected void Layout() {
        var y = _title.Y + _title.Height + 10;
        var width = _title.Width;
        foreach (var row in _rows) {
            row.X = Padding;
            row.Y = y;
            y += row.Height + 8;
            width = Math.Max(width, row.Width);
        }

        OkButton.X = Padding;
        OkButton.Y = y + 4;
        CancelButton.X = OkButton.X + OkButton.ButtonWidth + 10;
        CancelButton.Y = OkButton.Y;
        width = Math.Max(width, CancelButton.X + CancelButton.ButtonWidth - Padding);
        Resize(width + Padding * 2, OkButton.Y + OkButton.ButtonHeight + Padding);
    }

    public virtual void Open() {
        Visible = true;
        Layout();
    }

    public void Submit() {
        if (!Visible) return;
        if (!Validate()) return;
        Visible = false;
        OnOk?.Invoke();
    }

    public void Close(bool notify) {
        if (!Visible) return;
        Visible = false;
        if (notify) OnCancel?.Invoke();
    }

    protected virtual bool Validate() => true;

    public void CenterOn(int stageWidth, int stageHeight) {
        X = (stageWidth - PanelWidth) / 2;
        Y = (stageHeight - PanelHeight) / 2;
    }
}

public sealed class TextWindow : EditorWindow {
    public TextWindow(string title, string message, string okText, string cancelText) : base(title, okText, cancelText) {
        AddRow(EditorStyle.Text(message, 13, EditorStyle.TextColor, false, 360));
    }
}

public sealed class CreateMapWindow : EditorWindow {
    private readonly EditorInput _name;
    private readonly EditorInput _width;
    private readonly EditorInput _height;

    public string MapName => _name.Text.Trim();
    public int MapWidth => _width.IntValue;
    public int MapHeight => _height.IntValue;

    public CreateMapWindow() : base("New Map", "Create") {
        _name = AddRow(new EditorInput("Name:   ", 140, "", false, 40));
        _width = AddRow(new EditorInput("Width:  ", 60, "128", true, 4));
        _height = AddRow(new EditorInput("Height: ", 60, "128", true, 4));
    }

    public override void Open() {
        base.Open();
        _name.Focus();
    }

    protected override bool Validate() => MapName != "" && MapWidth is > 0 and <= 4096 && MapHeight is > 0 and <= 4096;
}

public sealed class MapDimensionsWindow : EditorWindow {
    private readonly EditorInput _width;
    private readonly EditorInput _height;

    public int MapWidth => _width.IntValue;
    public int MapHeight => _height.IntValue;

    public MapDimensionsWindow() : base("Change map dimensions", "Apply") {
        _width = AddRow(new EditorInput("Width:  ", 60, "", true, 4));
        _height = AddRow(new EditorInput("Height: ", 60, "", true, 4));
    }

    public void Open(int width, int height) {
        _width.SetText(width.ToString());
        _height.SetText(height.ToString());
        Open();
    }

    protected override bool Validate() => MapWidth is > 0 and <= 4096 && MapHeight is > 0 and <= 4096;
}

public sealed class EditTileNameWindow : EditorWindow {
    private readonly EditorInput _name;
    public int TileX;
    public int TileY;

    public string ObjName => _name.Text;

    public EditTileNameWindow() : base("Name") {
        _name = AddRow(new EditorInput("", 220, "", false, 200, 16));
    }

    public void Open(int x, int y, string objName) {
        TileX = x;
        TileY = y;
        _name.SetText(objName ?? "");
        Open();
        _name.Focus();
    }

    protected override bool Validate() => ObjName != "";
}

/// <summary>Fallback when the system file dialog isn't available: type a path.</summary>
public sealed class PathPromptWindow : EditorWindow {
    private readonly EditorInput _path;
    public string Path => _path.Text.Trim();

    public PathPromptWindow() : base("File path", "OK") {
        _path = AddRow(new EditorInput("", 380, "", false, 255));
    }

    public void Open(string initialPath) {
        _path.SetText(initialPath);
        Open();
        _path.Focus();
    }

    protected override bool Validate() => Path != "";
}
