using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Data;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Input;
using AlloyClient.UiLib.Rendering;
using AlloyClient.UiLib.Signals;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace AlloyClient.UiLib.BuiltIn;

public struct InputConfig
{
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

    public InputConfig()
    {
    }
}

public sealed class TextInput : Sprite
{
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
    private bool _allSelected;
    private readonly ColorRect _selection;
    private readonly Action<KeyboardEvent> _keyListener;
    private const double RepeatDelayMs = 400;
    private const double RepeatIntervalMs = 35;
    private Key? _heldKey;
    private double _nextRepeatMs;
    private readonly Action<KeyboardEvent> _keyUpListener;

    private readonly NineSliceRect _textBox;
    private readonly int _width;
    private int _caretIndex = -1;
    private bool _isCaretActive;
    private bool _isDefaultText = true;
    private double _lastCaretUpdateTime;

    private Vector2i _mousePosition;
    private int _startIndex;

    public TextInput(InputConfig config)
    {
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

        var caretConfig = new TextConfig
        {
            Text = "|",
            FontSize = config.FontSize,
            FontType = config.FontType,
            Color = config.Color,
            OutlineColor = config.OutlineColor,
            OutlineThickness = (int)_outlineThickness
        };
        _caret = new SimpleText(caretConfig);
        _caret.Visible = false;
        AddChild(_caret);

        var rectConfig = new NineSliceConfig
        {
            Width = _width,
            Height = (int)(_font.LineHeight * _fontScale) + CutY * 3,
            SliceData = BoxLookup,
            CutX = CutX,
            CutY = CutY
        };
        _textBox = new NineSliceRect(rectConfig);
        AddChild(_textBox);

        _selection = new ColorRect(new ColorRectConfig
        {
            X = CutX * 3,
            Y = CutY * 3,
            Width = 1,
            Height = (int)(_font.LineHeight * _fontScale),
            Color = 0x4A7BD0,
            Alpha = 0.45f
        });
        _selection.Visible = false;
        AddChild(_selection);

        _keyListener = OnKeyDown;
        _keyUpListener = OnKeyUp;

        SetHitboxType(CollisionType.Square);

        _globalMouseListener = OnGlobalMouseDown;

        AddEventListener(MouseEvent.LeftClick, new Action<MouseEvent>(OnMouseClick));

        ResizeBackBuffer();
        FillData();
    }

    public string Text => _inputText.ToString();

    private void ResizeBackBuffer()
    {
        var size = _maxCharacters + 1;
        VertexData = new VertexUi[size * 4];
        Indices = new ushort[size * 6];
        for (var i = 0; i < Indices.Length / 6; i++)
        {
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

    private void OnFrameEnter()
    {
        if (_heldKey is { } held)
        {
            var now = Stage.GameTime.TotalMs;
            var steps = 0;
            while (now >= _nextRepeatMs && steps++ < 5)
            {
                ApplyCaretKey(held);
                _nextRepeatMs += RepeatIntervalMs;
            }

            if (now >= _nextRepeatMs) _nextRepeatMs = now + RepeatIntervalMs;
        }

        if (!_isCaretActive) return;
        var gameTime = Stage.GameTime;
        if (gameTime.TotalMs - _lastCaretUpdateTime < 500) return;

        _lastCaretUpdateTime = gameTime.TotalMs;
        _caret.Visible = !_caret.Visible;
    }

    private void FillData()
    {
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

        for (var i = start; i < end; i++)
        {
            if (!caret && i == _caretIndex)
            {
                caret = true;
                i--;

                _caret.X = (int)zero.X;
                _caret.Y = CutY * 3;
                zero.X += _caret.Width;

                continue;
            }

            var c = password ? '*' : _inputText[i];
            switch (c)
            {
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

                    if (i < len - 1)
                    {
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

        if (!caret)
        {
            _caret.X = (int)zero.X;
            _caret.Y = CutY * 3;
        }

        var showSelection = _allSelected && _inputText.Length > 0 && !_isDefaultText;
        _selection.Visible = showSelection;
        if (showSelection)
            _selection.Resize(Math.Max(1, (int)(zero.X - CutX * 3)), (int)(_font.LineHeight * _fontScale));
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
        _allSelected = false;
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
        Console.WriteLine(
            $"caret click: rel={GetRelativeMousePosition()} hit={_mousePosition} start={_startIndex} len={_inputText.Length}");
        var mouseX = GetRelativeMousePosition().X; // Fresh position, local to this text box
        _caretIndex = GetCaretIndexAt(mouseX);
        FillData();
    }

    private int GetCaretIndexAt(float mouseX)
    {
        var password = _password && !_isDefaultText;
        var len = _inputText.Length;
        var x = (float)(CutX * 3);
        var caretSkipped = false;

        for (var i = _startIndex; i < len; i++)
        {
            // FillData leaves room for the caret before the character it sits in front of
            if (!caretSkipped && i == _caretIndex)
            {
                x += _caret.Width;
                caretSkipped = true;
            }

            var c = password ? '*' : _inputText[i];
            float advance = 0;
            if (c != '\n' && c != '\r' && _font.Glyphs.TryGetValue(c, out var glyph))
            {
                advance = glyph.Advance * _fontScale;
                if (i < len - 1)
                {
                    var k = password ? '*' : _inputText[i + 1];
                    _font.Kernings.TryGetValue((c, k), out var kern);
                    advance += kern * _fontScale;
                }
            }

            if (mouseX < x + advance / 2f) return i; // Left half of the character: caret goes before it
            x += advance;
        }

        return -1; // Past the last character: end of text
    }


    internal void OnTextInput(TextInputEventArgs e)
    {
        var text = e.Text;
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length != 1) return;

        var c = char.Parse(text);
        var ca = "\\" + text;
        var backspace = c == '\b';
        var tab = c == '\t';
        var enter = c == '\n';
        var delete = c == '\x7f';
        var allowedControlKey = backspace || tab || enter || delete;

        if (char.IsControl(c) && !allowedControlKey) return;

        if (tab)
        {
            Parent?.TabToNextTextInput();
            return;
        }

        if (enter) return;

        if (_allSelected)
        {
            if (backspace || delete)
            {
                DeleteSelection();
                return;
            }

            DeleteSelection(); // Typing replaces the selection, then falls through to insert the character
        }

        if ((backspace || delete) && KeyboardInput.IsCtrlDown())
        {
            DeleteWordBefore();
            return;
        }
        
        if (backspace)
        {
            if (_inputText.Length == 0) return;
            if (_caretIndex == -1)
            {
                _inputText.Remove(_inputText.Length - 1, 1);
            }
            else if (_caretIndex > 0)
            {
                _caretIndex--;
                _inputText.Remove(_caretIndex, 1);
            }
            else
            {
                return; // Caret at the very start, nothing before it
            }

            FillData();
            _onChange?.Invoke();
            return;
        }
        
        if (delete)
        {
            if (_caretIndex < 0 || _caretIndex >= _inputText.Length) return;
            _inputText.Remove(_caretIndex, 1);
            if (_caretIndex >= _inputText.Length) _caretIndex = -1;
            FillData();
            _onChange?.Invoke();
            return;
        }

        if (_inputText.Length == _maxCharacters) return;
        if (!_font.Glyphs.ContainsKey(c)) return;
        if (char.IsWhiteSpace(c) && c != ' ') return;

        if (_caretIndex == -1)
        {
            _inputText.Append(c);
        }
        else
        {
            _inputText.Insert(_caretIndex, c);
            _caretIndex++;
        }

        FillData();
        _onChange?.Invoke();
    }

    private void DeleteWordBefore()
    {
        if (_isDefaultText) return;
        var end = _caretIndex == -1 ? _inputText.Length : _caretIndex;
        if (end == 0) return;

        var start = end;
        while (start > 0 && char.IsWhiteSpace(_inputText[start - 1])) start--; // Trailing spaces
        while (start > 0 && !char.IsWhiteSpace(_inputText[start - 1])) start--; // The word itself

        _inputText.Remove(start, end - start);
        if (_caretIndex != -1) _caretIndex = start;

        FillData();
        _onChange?.Invoke();
    }

    public bool HasText(bool ignoreWhitespace)
    {
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
        Stage?.AddEventListener(KeyboardEvent.KeyUp, _keyUpListener);
        Stage?.AddEventListener(KeyboardEvent.KeyDown, _keyListener);

        FillData();

        if (changed) _onChange?.Invoke();
    }

    public void UnFocus(bool clearText = false)
    {
        if (ActiveInput == this) ActiveInput = null;

        _isCaretActive = false;
        _caretIndex = -1;
        _caret.Visible = false;
        _onUnfocus?.Invoke();

        var changed = false;

        if (clearText && _inputText.Length > 0)
        {
            _inputText.Clear();
            changed = true;
        }

        if (_inputText.Length == 0 && !_isDefaultText)
        {
            SetDefault();
            changed = true;
        }


        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
        Stage?.RemoveEventListener(KeyboardEvent.KeyUp, _keyUpListener);
        _heldKey = null;
        Stage?.RemoveEventListener(MouseEvent.LeftDown, _globalMouseListener);
        Stage?.RemoveEventListener(KeyboardEvent.KeyDown, _keyListener);
        FillData();

        if (changed) _onChange?.Invoke();
    }

    public void InsertText(string text)
    {
        ClearIfDefault();

        if (_caretIndex == -1)
        {
            _inputText.Append(text);
        }
        else
        {
            _inputText.Insert(_caretIndex, text);
            _caretIndex += text.Length;
        }

        FillData();
        _onChange?.Invoke();
    }

    public void SetText(string text)
    {
        if (text == string.Empty)
        {
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

    private void SetDefault()
    {
        _inputText.Clear();
        _inputText.Append(_defaultText);
        _isDefaultText = true;
    }

    private void OnKeyDown(KeyboardEvent e)
    {
        if (ActiveInput != this) return;

        if (!e.Ctrl)
        {
            var isArrow = e.Key is Key.LeftArrow or Key.RightArrow;
            if (!isArrow && e.Key is not (Key.Home or Key.End)) return;

            if (isArrow)
            {
                if (_heldKey == e.Key) return; // OS auto-repeat, our timer handles repeating
                _heldKey = e.Key;
                _nextRepeatMs = Stage.GameTime.TotalMs + RepeatDelayMs;
            }

            ApplyCaretKey(e.Key);
            return;
        }

        switch (e.Key)
        {
            case Key.A: SelectAll(); break;
            case Key.C: CopySelection(); break;
            case Key.X:
                if (CopySelection()) DeleteSelection();
                break;
            case Key.V: PasteFromClipboard(); break;
        }
    }

    private void OnKeyUp(KeyboardEvent e)
    {
        if (_heldKey == e.Key) _heldKey = null;
    }

    private void ApplyCaretKey(Key key)
    {
        switch (key)
        {
            case Key.LeftArrow: MoveCaret(-1); break;
            case Key.RightArrow: MoveCaret(1); break;
            case Key.Home: SetCaret(0); break;
            case Key.End: SetCaret(-1); break;
        }
    }

    private void MoveCaret(int dir)
    {
        if (_isDefaultText) return;
        var len = _inputText.Length;

        if (_allSelected)
        {
            // Collapse the selection: left goes to the start, right to the end
            SetCaret(dir < 0 ? 0 : -1);
            return;
        }

        var current = _caretIndex == -1 ? len : _caretIndex;
        SetCaret(Math.Clamp(current + dir, 0, len));
    }

// index -1 or == length means end of text
    private void SetCaret(int index)
    {
        if (_isDefaultText) return;
        _allSelected = false;
        _caretIndex = index >= _inputText.Length ? -1 : index;
        _caret.Visible = true; // Show the caret right away instead of mid-blink
        _lastCaretUpdateTime = Stage.GameTime.TotalMs;
        FillData();
    }

    private void SelectAll()
    {
        if (_isDefaultText || _inputText.Length == 0) return;
        _allSelected = true;
        _caretIndex = -1;
        FillData();
    }

    private bool CopySelection()
    {
        if (!_allSelected || _password) return false; // Password fields never copy
        Toolkit.Clipboard.SetClipboardText(Text);
        return true;
    }

    private void DeleteSelection()
    {
        _allSelected = false;
        _caretIndex = -1;
        _inputText.Clear();
        FillData();
        _onChange?.Invoke();
    }

    private void PasteFromClipboard()
    {
        string clip;
        try
        {
            clip = Toolkit.Clipboard.GetClipboardText();
        }
        catch
        {
            return;
        }

        if (string.IsNullOrEmpty(clip)) return;

        if (_allSelected)
        {
            _allSelected = false;
            _caretIndex = -1;
            _inputText.Clear();
        }

        ClearIfDefault();

        var added = 0;
        foreach (var c in clip)
        {
            if (_inputText.Length >= _maxCharacters) break;
            if (char.IsWhiteSpace(c) && c != ' ') continue; // Drop newlines and tabs
            if (!_font.Glyphs.ContainsKey(c)) continue;

            if (_caretIndex == -1)
            {
                _inputText.Append(c);
            }
            else
            {
                _inputText.Insert(_caretIndex, c);
                _caretIndex++;
            }

            added++;
        }

        FillData();
        if (added > 0) _onChange?.Invoke();
    }
}