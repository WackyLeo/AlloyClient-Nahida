using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Data;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using AlloyClient.UiLib.Signals;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace AlloyClient.UiLib.BuiltIn;

public struct InputConfig {
    public int X = 0;
    public int Y = 0;
    public float FontSize = 10;
    public FontType FontType = FontType.Normal;
    public uint Color = 0xFFFFFF;
    public uint OutlineColor = 0x0;
    public uint OutlineThickness = 1;
    public int Width = 100;
    public string DefaultText = "";
    public byte MaxCharacters = byte.MaxValue;
    public bool Password = false;
    public bool ClickToActivate = true;
    public Action OnFocus = null;
    public Action OnUnfocus = null;
    public UiAnchor Anchor = UiAnchor.LeftTop;

    public Action OnChange = null;

    public InputConfig() {
    }
}

public sealed class TextInput : Sprite {
    public const string BoxLookup = "textBox";

    private const int CutX = 2;
    private const int CutY = 2;

    internal static TextInput ActiveInput;

    /// <summary>True while any text input has keyboard focus (used to suppress hotkeys while typing).</summary>
    public static bool IsAnyFocused => ActiveInput != null;
    private readonly SimpleText _caret;
    private readonly bool _clickActivate;
    private readonly string _defaultText;
    private readonly BitmapFont _font;

    private readonly float _fontScale;
    private readonly Action<MouseEvent> _globalMouseListener;
    private readonly StringBuilder _inputText = new();
    private readonly byte _maxCharacters;
    private readonly Action _onChange;
    private readonly Action _onFocus;
    private readonly Action _onUnfocus;
    private readonly float _outlineThickness;
    private readonly bool _password;

    private readonly NineSliceRect _textBox;
    private readonly int _width;
    private int _caretIndex = -1;
    private bool _isCaretActive;
    private bool _isDefaultText = true;
    private double _lastCaretUpdateTime;

    private Vector2i _mousePosition;
    private int _startIndex;

    public TextInput(InputConfig config) {
        X = config.X;
        Y = config.Y;
        _fontScale = config.FontSize;
        _font = UiRender.GetFont(config.FontType);
        SetColor(config.Color);
        SetColorSecondary(config.OutlineColor);
        _outlineThickness = _font.ValidateOutlineSize(config.OutlineThickness);
        _width = config.Width;
        _defaultText = config.DefaultText;
        _maxCharacters = config.MaxCharacters;
        _password = config.Password;
        _clickActivate = config.ClickToActivate;
        _onFocus = config.OnFocus;
        _onUnfocus = config.OnUnfocus;
        _onChange = config.OnChange;
        SetAnchor(config.Anchor);

        MouseEnabled = true;

        TextureId = TextureType.Text;

        Extra1.X = _outlineThickness;

        _inputText.Append(config.DefaultText);

        var caretConfig = new TextConfig {
            Text = "|", FontSize = config.FontSize, FontType = config.FontType, Color = config.Color,
            OutlineColor = config.OutlineColor, OutlineThickness = (int)_outlineThickness
        };
        _caret = new SimpleText(caretConfig);
        _caret.Visible = false;
        AddChild(_caret);

        var rectConfig = new NineSliceConfig {
            Width = _width, Height = (int)(_font.LineHeight * _fontScale) + CutY * 3, SliceData = BoxLookup,
            CutX = CutX, CutY = CutY
        };
        _textBox = new NineSliceRect(rectConfig);
        AddChild(_textBox);

        SetHitboxType(CollisionType.Square);

        _globalMouseListener = OnGlobalMouseDown;

        AddEventListener(MouseEvent.LeftClick, new Action<MouseEvent>(OnMouseClick));

        ResizeBackBuffer();
        FillData();
    }
    
    public string Text => _inputText.ToString();

    private void ResizeBackBuffer() {
        var size = _maxCharacters + 1;
        VertexData = new VertexUi[size * 4];
        Indices = new ushort[size * 6];
        for (var i = 0; i < Indices.Length / 6; i++) {
            var idx6 = i * 6;
            var idx4 = i * 4;

            Indices[idx6] = (ushort)(0 + idx4);
            Indices[idx6 + 1] = (ushort)(1 + idx4);
            Indices[idx6 + 2] = (ushort)(2 + idx4);
            Indices[idx6 + 3] = (ushort)(0 + idx4);
            Indices[idx6 + 4] = (ushort)(2 + idx4);
            Indices[idx6 + 5] = (ushort)(3 + idx4);
        }
    }

    private void OnFrameEnter() {
        if (!_isCaretActive) return;
        var gameTime = Stage.GameTime;
        if (gameTime.TotalMs - _lastCaretUpdateTime < 500) return;

        _lastCaretUpdateTime = gameTime.TotalMs;
        _caret.Visible = !_caret.Visible;
    }

    private void FillData() {
        var startX = CutX * 3;
        var startY = _font.Ascender * _fontScale + CutY * 3;
        var zero = new Vector2(startX, startY);

        var (start, end) = _font.GetStartIndex(_inputText, _caretIndex, _width - startX * 2 - _caret.Width,
            _outlineThickness, _fontScale);
        _startIndex = start;
        OverridePrimCount = 2;

        var idx = 4;
        var len = _inputText.Length;
        var caret = false;

        var password = _password && !_isDefaultText;

        for (var i = start; i < end; i++) {
            if (!caret && i == _caretIndex) {
                caret = true;
                i--;

                _caret.X = (int)zero.X;
                _caret.Y = CutY * 3;
                zero.X += _caret.Width;

                continue;
            }

            var c = password ? '*' : _inputText[i];
            switch (c) {
                case '\n':
                case '\r':
                    continue;
                default:
                    if (!_font.Glyphs.TryGetValue(c, out var glyph)) continue;

                    var uv = glyph.UV;
                    var pos = glyph.Position;

                    VertexData[idx + 0] =
                        new VertexUi(new Vector2(zero.X + pos.X0 * _fontScale, zero.Y - pos.Y1 * _fontScale),
                            new Vector2(uv.X0, uv.Y1)); //bl
                    VertexData[idx + 1] =
                        new VertexUi(new Vector2(zero.X + pos.X0 * _fontScale, zero.Y - pos.Y0 * _fontScale),
                            new Vector2(uv.X0, uv.Y0)); //tl
                    VertexData[idx + 2] =
                        new VertexUi(new Vector2(zero.X + pos.X1 * _fontScale, zero.Y - pos.Y0 * _fontScale),
                            new Vector2(uv.X1, uv.Y0)); //tr
                    VertexData[idx + 3] =
                        new VertexUi(new Vector2(zero.X + pos.X1 * _fontScale, zero.Y - pos.Y1 * _fontScale),
                            new Vector2(uv.X1, uv.Y1)); //br

                    if (i < len - 1) {
                        var k = password ? '*' : _inputText[i + 1];
                        _font.Kernings.TryGetValue((c, k), out var kern);
                        zero.X += kern * _fontScale;
                    }

                    zero.X += glyph.Advance * _fontScale;
                    idx += 4;
                    OverridePrimCount += 2;
                    continue;
            }
        }

        if (!caret) {
            _caret.X = (int)zero.X;
            _caret.Y = CutY * 3;
        }

        SetGraphicsBuffer();
    }

    protected override bool CustomHitbox(Vector2i pos) 
    {
        var hit = pos.X > 0 && pos.X < _textBox.Width && pos.Y > 0 && pos.Y < _textBox.Height;
        _mousePosition = pos;
        return hit;
    }

    private void OnGlobalMouseDown(MouseEvent e) 
    {
        if (!IsInBounds(e.Coords))
        {
            UnFocus();
        }
    }

    private void OnMouseClick(MouseEvent args) 
    {
        if (ActiveInput != this && _clickActivate) 
        {
            ActiveInput?.UnFocus();
            Parent?.SetTextInputIndex(this);
            Focus();
        }

        SetCaretIndex();
    }

    private void SetCaretIndex() 
    {
        var i = 1; // Offset by 1 for rect
        for (var j = _startIndex; j < _inputText.Length; j++) 
        {
            var p1 = VertexData[i * 4 + 1].Position.X;
            var p2 = VertexData[i * 4 + 3].Position.X;
            var half = (p2 - p1) / 2f;

            if (j == _startIndex && _mousePosition.X <= p1)
                _caretIndex = 0;
            else if (_mousePosition.X >= p1 && _mousePosition.X < p1 + half)
                _caretIndex = _startIndex + i - 1;
            else if (_mousePosition.X <= p2 && _mousePosition.X >= p2 - half)
                _caretIndex = _startIndex + i;
            else if (j + 1 == _inputText.Length && _mousePosition.X >= p2) _caretIndex = -1;

            i++;
        }

        FillData();
    }

    internal void OnTextInput(TextInputEventArgs e) {
        var text = e.Text;
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length != 1) return;

        var c = char.Parse(text);
        var ca = "\\" + text;
        var backspace = c == '\b';
        var tab = c == '\t';
        var enter = c == '\n';
        var allowedControlKey = backspace || tab || enter;

        if (char.IsControl(c) && !allowedControlKey) return;

        switch (true) 
        {
            case true when tab:
                Parent?.TabToNextTextInput();
                return;
            case true when enter:
                
            case true when backspace && _inputText.Length > 0:
                if (_caretIndex == -1) 
                {
                    _inputText.Remove(_inputText.Length - 1, 1);
                    FillData();
                    _onChange?.Invoke();
                } else if (_caretIndex > 0) 
                {
                    _caretIndex--;
                    _inputText.Remove(_caretIndex, 1);
                    FillData();
                    _onChange?.Invoke();
                }
                return;
        }

        if (_inputText.Length == _maxCharacters) return;
        if (!_font.Glyphs.ContainsKey(c)) return;
        if (char.IsWhiteSpace(c) && c != ' ') return;

        if (_caretIndex == -1) {
            _inputText.Append(c);
        } else {
            _inputText.Insert(_caretIndex, c);
            _caretIndex++;
        }

        FillData();
        _onChange?.Invoke();
    }

    public bool HasText(bool ignoreWhitespace) {
        if (ignoreWhitespace) return !string.IsNullOrWhiteSpace(_inputText.ToString());
        return _inputText.Length > 0;
    }

    public void Focus() 
    {
        if (ActiveInput != this) 
        {
            ActiveInput?.UnFocus();
            ActiveInput = this;
        }

        var changed = ClearIfDefault();
        
        _caretIndex = -1;
        _isCaretActive = true;
        _caret.Visible = true;
        _onFocus?.Invoke();

        AddEventListener(Event.EnterFrame, OnFrameEnter);
        Stage?.AddEventListener(MouseEvent.LeftDown, _globalMouseListener);

        FillData();
        
        if (changed) _onChange?.Invoke();
    }

    public void UnFocus(bool clearText = false) {
        if (ActiveInput == this) ActiveInput = null;

        _isCaretActive = false;
        _caretIndex = -1;
        _caret.Visible = false;
        _onUnfocus?.Invoke();

        var changed = false;

        if (clearText && _inputText.Length > 0) {
            _inputText.Clear();
            changed = true;
        }

        if (_inputText.Length == 0 && !_isDefaultText) {
            SetDefault();
            changed = true;
        }

        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
        Stage?.RemoveEventListener(MouseEvent.LeftDown, _globalMouseListener);
        FillData();

        if (changed) _onChange?.Invoke();
    }

    public void InsertText(string text) {
        ClearIfDefault();

        if (_caretIndex == -1) {
            _inputText.Append(text);
        } else {
            _inputText.Insert(_caretIndex, text);
            _caretIndex += text.Length;
        }

        FillData();
        _onChange?.Invoke();
    }

    public void SetText(string text) {
        if (text == string.Empty) {
            SetDefault();
            _onChange?.Invoke();
            return;
        }

        ClearIfDefault();
        _inputText.Append(text);
        FillData();
        _onChange?.Invoke();
    }

    private bool ClearIfDefault() 
    {
        if (!_isDefaultText) return false;
        _inputText.Clear();
        _isDefaultText = false;
        return true;
    }

    private void SetDefault() {
        _inputText.Clear();
        _inputText.Append(_defaultText);
        _isDefaultText = true;
    }
}