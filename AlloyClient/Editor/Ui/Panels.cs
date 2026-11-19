using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using OpenTK.Mathematics;

namespace AlloyClient.Editor;

/// <summary>Square showing a ground/object texture or a region color.</summary>
public sealed class ElementIcon : Sprite
{
    private readonly QuadBatch _texture = new(TextureType.GameAtlas);
    private readonly QuadBatch _color = new(TextureType.Color);
    private readonly int _size;

    public ElementIcon(int size)
    {
        _size = size;
        AddChild(_texture);
        AddChild(_color);
        SelfContentWidth = size;
        SelfContentHeight = size;
        UpdateBounds();
    }

    public void Set(DrawType drawType, int element)
    {
        _texture.Clear();
        _color.Clear();
        if (drawType == DrawType.Regions)
        {
            if (element != 0)
                _color.AddColorQuad(0, 0, _size, _size, 0xC0000000 | EditorAssets.GetRegionColor(element));
        }
        else if (EditorAssets.TryGetTexture(drawType, element, out var uv))
        {
            _texture.AddTexturedQuad(0, 0, _size, _size, uv);
        }
        else if (element != MEBrush.EmptyValue(drawType))
        {
            _color.AddColorQuad(0, 0, _size, _size, 0x80FF00FF);
        }

        _texture.Commit();
        _color.Commit();
    }
}

#region Draw element list

public sealed class DrawElementListView : EditorPanel
{
    public const int PanelWidth_ = 182;
    private const int ElementSize = 38;
    private const int Gap = 2;
    private const int Columns = 4;
    private const int Row = ElementSize + Gap;
    private const int ListX = 5;

    private readonly MapEditorScreen _editor;
    private readonly SimpleText _title;
    private readonly EditorInput _search;
    private readonly Container _clip;
    private readonly ColorRect _scrollTrack;
    private readonly ColorRect _scrollThumb;
    private bool _draggingThumb;
    private int _thumbGrabOffset;
    private readonly QuadBatch _selection = new(TextureType.Color);
    private readonly List<ElementCell> _cells = [];

    private List<int> _items = [];
    private int _scroll;
    private int _listY;
    private int _listHeight;
    private string _searchText = "";
    private readonly Dictionary<string, string> _propFilters = new(StringComparer.OrdinalIgnoreCase);

    public DrawType DrawType { get; private set; }
    public int SelectedElement { get; private set; } = int.MinValue;
    public Action<int> OnSelect; // Selected element type, or the empty value when deselected

    public DrawElementListView(MapEditorScreen editor) : base(PanelWidth_, 300)
    {
        _editor = editor;

        _title = EditorStyle.Text("Preview", 12, EditorStyle.TitleColor, true);
        _title.X = (PanelWidth_ - _title.Width) / 2;
        _title.Y = 4;
        AddChild(_title);

        _search = new EditorInput("", PanelWidth_ - 14, "", false, 40, 12, OnSearchChange);
        _search.X = ListX;
        _search.Y = _title.Y + _title.Height + 4;
        AddChild(_search);

        _listY = _search.Y + _search.Height + 4;
        _clip = new Container(new ContainerConfig
            { X = ListX, Y = _listY, Width = Columns * Row, Height = 100, EnableClip = true });
        AddChild(_clip);
        _clip.AddChild(_selection);

        _scrollTrack = new ColorRect(new ColorRectConfig
            { Width = 8, Height = 20, Color = 0x151515, Alpha = 0.8f, MouseEnabled = true });
        _scrollTrack.X = PanelWidth_ - 10;
        _scrollTrack.AddEventListener(MouseEvent.LeftDown, () => BeginThumbDrag(false));
        AddChild(_scrollTrack);

        _scrollThumb = new ColorRect(new ColorRectConfig
            { Width = 8, Height = 20, Color = 0x8A8A8A, MouseEnabled = true });
        _scrollThumb.X = PanelWidth_ - 10;
        _scrollThumb.AddEventListener(MouseEvent.LeftDown, () => BeginThumbDrag(true));
        AddChild(_scrollThumb);

        // The panel itself has to be mouse enabled for wheel events bubbling up from the cells to reach it
        MouseEnabled = true;
        AddEventListener(MouseEvent.ScrollVertical, new Action<MouseEvent>(OnScroll));
        AddEventListener(Event.EnterFrame, OnEnterFrame);
    }

    public void SetHeight(int height)
    {
        Resize(PanelWidth_, height);
        _listHeight = Math.Max(Row, height - _listY - 6);
        _clip.SelfContentHeight = _listHeight;
        _clip.UpdateBounds();

        var rows = _listHeight / Row + 2;
        while (_cells.Count < rows * Columns)
        {
            var cell = new ElementCell(this, ElementSize);
            _cells.Add(cell);
            _clip.AddChild(cell);
        }

        ClampScroll();
        RefreshCells();
    }

#region Filters

    private void OnSearchChange(string text)
    {
        _searchText = text?.Trim() ?? "";
        SetContent(DrawType);
    }

    public void ResetFilters()
    {
        _searchText = "";
        _search.SetText("");
    }

    public IReadOnlyDictionary<string, string> PropertyFilters => _propFilters;

    public void AddPropertyFilter(string prop, string value)
    {
        _propFilters[prop] = value ?? "";
        SetContent(DrawType);
    }

    public void RemovePropertyFilter(string prop)
    {
        _propFilters.Remove(prop);
        SetContent(DrawType);
    }

    private bool PassesFilters(int type, HashSet<int> searchMatches)
    {
        if (searchMatches != null && !searchMatches.Contains(type)) return false;
        if (DrawType != DrawType.Objects || _propFilters.Count == 0) return true;

        var xml = EditorAssets.GetXml(DrawType.Objects, type);
        if (xml == null) return false;
        foreach (var (prop, value) in _propFilters)
        {
            var elem = xml.Elements().FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, prop, StringComparison.OrdinalIgnoreCase));
            var has = elem != null;
            if (value == "" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                if (!has) return false;
            }
            else if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                if (has) return false;
            }
            else if (!has || !string.Equals(elem.Value.Trim(), value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

#endregion

    public void SetContent(DrawType drawType)
    {
        DrawType = drawType;
        var searchMatches = _searchText == "" ? null : EditorAssets.Search(drawType, _searchText).ToHashSet();
        _items = EditorAssets.GetTypes(drawType).Where(t => PassesFilters(t, searchMatches)).ToList();
        _scroll = 0;
        ClampScroll();
        RefreshCells();
    }

    public void SetSelected(int element, bool scrollTo = true)
    {
        SelectedElement = element;
        var idx = _items.IndexOf(element);
        if (idx >= 0 && scrollTo)
        {
            var rowY = idx / Columns * Row;
            if (rowY < _scroll || rowY + Row > _scroll + _listHeight) _scroll = rowY;
            ClampScroll();
        }

        RefreshCells();
    }

    internal void OnCellClick(ElementCell cell)
    {
        if (cell.Element == SelectedElement)
        {
            // Clicking the selected element deselects it
            SelectedElement = int.MinValue;
            RefreshCells();
            OnSelect?.Invoke(MEBrush.EmptyValue(DrawType));
            return;
        }

        SelectedElement = cell.Element;
        RefreshCells();
        OnSelect?.Invoke(cell.Element);
    }

    internal void OnCellHover(ElementCell cell, bool hover) =>
        _editor.OnElementHover(cell, hover ? DrawType : null, cell.Element);

    private void OnScroll(MouseEvent e)
    {
        e.StopPropagation();
        _scroll -= (int)(e.Delta * Row);
        ClampScroll();
        RefreshCells();
    }

#region Scrollbar dragging

    private int MaxScroll => Math.Max(0, (_items.Count + Columns - 1) / Columns * Row - _listHeight);

    private void BeginThumbDrag(bool onThumb)
    {
        if (MaxScroll == 0 || Stage == null) return;
        var mouseY = GetRelativeMousePosition().Y;
        // Clicking the track jumps the thumb's center to the mouse
        _thumbGrabOffset = onThumb ? mouseY - _scrollThumb.Y : _scrollThumb.Height / 2;
        _draggingThumb = true;
        Stage.AddEventListener(MouseEvent.LeftUp, EndThumbDrag);
        UpdateThumbDrag();
    }

    private void EndThumbDrag()
    {
        _draggingThumb = false;
        Stage?.RemoveEventListener(MouseEvent.LeftUp, EndThumbDrag);
    }

    private void OnEnterFrame()
    {
        if (_draggingThumb) UpdateThumbDrag();
    }

    private void UpdateThumbDrag()
    {
        var range = _listHeight - _scrollThumb.Height;
        if (range <= 0) return;
        var thumbY = GetRelativeMousePosition().Y - _thumbGrabOffset - _listY;
        var newScroll = (int)((long)Math.Clamp(thumbY, 0, range) * MaxScroll / range);
        if (newScroll == _scroll) return;
        _scroll = newScroll;
        ClampScroll();
        RefreshCells();
    }

#endregion

    private void ClampScroll()
    {
        var rows = (_items.Count + Columns - 1) / Columns;
        var max = Math.Max(0, rows * Row - _listHeight);
        _scroll = Math.Clamp(_scroll, 0, max);

        var total = Math.Max(1, rows * Row);
        var thumbH = Math.Clamp(_listHeight * _listHeight / total, 20, Math.Max(20, _listHeight));
        _scrollThumb.Resize(8, thumbH);
        _scrollThumb.Visible = max > 0;
        _scrollTrack.Resize(8, _listHeight);
        _scrollTrack.Y = _listY;
        _scrollTrack.Visible = max > 0;
        _scrollThumb.Y = _listY + (max == 0 ? 0 : (_listHeight - thumbH) * _scroll / max);
    }

    private void RefreshCells()
    {
        var firstRow = _scroll / Row;
        var offset = -(_scroll % Row);
        _selection.Clear();

        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            var itemIdx = firstRow * Columns + i;
            if (itemIdx >= _items.Count)
            {
                cell.Visible = false;
                continue;
            }

            cell.Visible = true;
            cell.X = i % Columns * Row;
            cell.Y = offset + i / Columns * Row;
            cell.SetElement(DrawType, _items[itemIdx]);

            if (_items[itemIdx] == SelectedElement)
                _selection.AddOutline(cell.X - 1, cell.Y - 1, ElementSize + 2, ElementSize + 2, 2, 0xFFFFFFFF);
        }

        _selection.Commit();
        _clip.SetChildIndex(_selection, _clip.NumChildren - 1); // Keep the selection on top
    }
}

public sealed class ElementCell : Sprite
{
    private readonly DrawElementListView _list;
    private readonly ElementIcon _icon;
    private bool _down;
    private bool _hovering;

    public DrawType DrawType { get; private set; }
    public int Element { get; private set; } = int.MinValue;

    public ElementCell(DrawElementListView list, int size)
    {
        _list = list;
        AddChild(new ColorRect(new ColorRectConfig { Width = size, Height = size, Color = 0x1C1C1C, Alpha = 0.9f }));
        _icon = new ElementIcon(size - 6);
        _icon.X = 3;
        _icon.Y = 3;
        AddChild(_icon);

        MouseEnabled = true;
        AddEventListener(MouseEvent.LeftDown, () => { _down = true; });
        AddEventListener(MouseEvent.LeftUp, () =>
        {
            if (_down) _list.OnCellClick(this);
            _down = false;
        });
        AddEventListener(MouseEvent.MouseOver, () =>
        {
            _hovering = true;
            _list.OnCellHover(this, true);
        });
        AddEventListener(MouseEvent.MouseOut, () =>
        {
            _down = false;
            _hovering = false;
            _list.OnCellHover(this, false);
        });
    }

    public void SetElement(DrawType drawType, int element)
    {
        if (DrawType == drawType && Element == element) return;
        DrawType = drawType;
        Element = element;
        _icon.Set(drawType, element);
        if (_hovering) _list.OnCellHover(this, true); // Content scrolled under the mouse
    }
}

#endregion

public sealed class MapSelectorView : EditorPanel
{
    public const int PanelWidth_ = 150;
    private const int PanelHeight_ = 150;
    private const int SlotHeight = 18;

    private readonly Container _clip;
    private readonly List<MapSlot> _slots = [];
    private readonly int _listY;
    private int _scroll;

    public int SelectedMap { get; private set; } = -1;
    public Action<int> OnMapSelect;
    public Action<int> OnMapClose;

    public MapSelectorView() : base(PanelWidth_, PanelHeight_)
    {
        var title = EditorStyle.Text("Select map", 12, EditorStyle.TitleColor, true);
        title.X = (PanelWidth_ - title.Width) / 2;
        title.Y = 3;
        AddChild(title);

        _listY = title.Y + title.Height + 5;
        _clip = new Container(new ContainerConfig
            { X = 4, Y = _listY, Width = PanelWidth_ - 8, Height = PanelHeight_ - _listY - 4, EnableClip = true });
        AddChild(_clip);

        MouseEnabled = true;
        AddEventListener(MouseEvent.ScrollVertical, new Action<MouseEvent>(e =>
        {
            e.StopPropagation();
            _scroll -= (int)(e.Delta * SlotHeight);
            PositionSlots();
        }));
    }

    public IEnumerable<int> MapIds => _slots.Select(s => s.MapId);

    public void AddMap(int mapId, MapData data)
    {
        var slot = new MapSlot(this, mapId, data);
        _slots.Add(slot);
        _clip.AddChild(slot);
        PositionSlots();
    }

    public void RemoveMap(int mapId)
    {
        var slot = _slots.FirstOrDefault(s => s.MapId == mapId);
        if (slot == null) return;
        slot.Detach();
        _slots.Remove(slot);
        _clip.RemoveChild(slot);
        PositionSlots();
    }

    private void PositionSlots()
    {
        var max = Math.Max(0, _slots.Count * SlotHeight - (PanelHeight_ - _listY - 4));
        _scroll = Math.Clamp(_scroll, 0, max);
        for (var i = 0; i < _slots.Count; i++) _slots[i].Y = i * SlotHeight - _scroll;
    }

    internal void OnSlotClick(MapSlot slot)
    {
        SelectMap(slot.MapId);
        OnMapSelect?.Invoke(slot.MapId);
    }

    internal void OnSlotClose(MapSlot slot) => OnMapClose?.Invoke(slot.MapId);

    /// <summary>-1 selects the first map.</summary>
    public void SelectMap(int mapId)
    {
        if (_slots.Count == 0)
        {
            SelectedMap = -1;
            return;
        }

        if (mapId == -1) mapId = _slots[0].MapId;
        if (_slots.All(s => s.MapId != mapId)) return;

        SelectedMap = mapId;
        foreach (var s in _slots) s.SetSelected(s.MapId == mapId);
    }

    internal sealed class MapSlot : Sprite
    {
        private readonly MapSelectorView _view;
        private readonly MapData _data;
        private readonly SimpleText _text;
        private readonly EditorButton _close;
        private bool _selected;
        private bool _down;

        public readonly int MapId;

        public MapSlot(MapSelectorView view, int mapId, MapData data)
        {
            _view = view;
            _data = data;
            MapId = mapId;

            AddChild(new ColorRect(new ColorRectConfig
                { Width = PanelWidth_ - 30, Height = SlotHeight - 2, Color = 0x202020, Alpha = 0.8f }));
            _text = EditorStyle.Text("", 11);
            _text.X = 3;
            _text.Y = 1;
            AddChild(_text);

            _close = new EditorButton("x", () => _view.OnSlotClose(this), 18, 10, SlotHeight - 2);
            _close.X = PanelWidth_ - 28;
            AddChild(_close);

            data.Changed += UpdateText;
            data.Saved += UpdateText;
            UpdateText();

            MouseEnabled = true;
            AddEventListener(MouseEvent.LeftDown, () => { _down = true; });
            AddEventListener(MouseEvent.MouseOut, () => { _down = false; });
            AddEventListener(MouseEvent.LeftUp, () =>
            {
                if (_down) _view.OnSlotClick(this);
                _down = false;
            });
        }

        public void Detach()
        {
            _data.Changed -= UpdateText;
            _data.Saved -= UpdateText;
        }

        private void UpdateText()
        {
            var name = _data.MapName.Length > 14 ? _data.MapName[..13] + "…" : _data.MapName;
            _text.SetText($"{MapId}.{(_data.SavedChanges ? "" : "*")} {name}");
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            _text.SetColor(selected ? EditorStyle.TitleColor : 0x888888);
        }
    }
}

public sealed class MapToolbar : EditorPanel
{
    private static readonly int[] ToolOrder =
    [
        METool.SelectId, METool.DrawId, METool.EraserId, METool.SampleId, METool.BucketId, METool.LineId,
        METool.ShapeId, METool.EditId
    ];

    private static readonly string[] Keys = ["M", "D", "E", "A", "B", "L", "S", "I"];
    private readonly Dictionary<int, EditorButton> _buttons = new();

    public MapToolbar(MapEditorScreen editor) : base(78, 100)
    {
        var title = EditorStyle.Text("Tools", 12, EditorStyle.TitleColor, true);
        title.X = (78 - title.Width) / 2;
        title.Y = 3;
        AddChild(title);

        var y = title.Y + title.Height + 5;
        for (var i = 0; i < ToolOrder.Length; i++)
        {
            var id = ToolOrder[i];
            var button = new EditorButton($"{METool.ToolIdToName(id)} ({Keys[i]})", () => editor.OnToolSwitch(id), 70,
                11);
            button.X = 4;
            button.Y = y;
            button.OnHover = () =>
                editor.ShowTooltip($"{METool.ToolIdToName(id)}\n{METool.GetToolDescription(id)}", button);
            button.OnHoverEnd = () => editor.HideTooltip(button);
            y += button.ButtonHeight + 3;
            _buttons[id] = button;
            AddChild(button);
        }

        Resize(78, y + 2);
        SetSelected(METool.SelectId);
    }

    public void SetSelected(int toolId)
    {
        foreach (var (id, b) in _buttons) b.SetSelected(id == toolId);
    }
}

public sealed class TileInfoPanel : EditorPanel
{
    private const int ImageSize = 20;
    private readonly SimpleText _pos = EditorStyle.Text("", 12);
    private readonly (ElementIcon icon, SimpleText text)[] _rows = new (ElementIcon, SimpleText)[3];

    public TileInfoPanel() : base(100, 40, 0xC0303030)
    {
        _pos.X = 5;
        _pos.Y = 4;
        AddChild(_pos);
        for (var i = 0; i < 3; i++)
        {
            var icon = new ElementIcon(ImageSize);
            var text = EditorStyle.Text("", 13, EditorStyle.TextColor, true);
            AddChild(icon);
            AddChild(text);
            _rows[i] = (icon, text);
        }
    }

    public void SetInfo(int x, int y, MapTileData tile)
    {
        _pos.SetText($"X: {x} Y: {y}");
        var width = _pos.Width;
        var rowY = _pos.Y + _pos.Height + 5;
        var types = new[]
        {
            (DrawType.Ground, tile.GroundType, -1), (DrawType.Objects, tile.ObjType, 0),
            (DrawType.Regions, tile.RegType, 0)
        };
        for (var i = 0; i < 3; i++)
        {
            var (type, element, empty) = types[i];
            var (icon, text) = _rows[i];
            var visible = element != empty;
            icon.Visible = text.Visible = visible;
            if (!visible) continue;

            icon.Set(type, element);
            var id = EditorAssets.GetId(type, element) ?? $"Unknown ({element})";
            if (type == DrawType.Objects && !string.IsNullOrEmpty(tile.ObjCfg)) id += $" \"{tile.ObjCfg}\"";
            text.SetText(id);
            icon.X = 5;
            icon.Y = rowY;
            text.X = icon.X + ImageSize + 5;
            text.Y = rowY + (ImageSize - text.Height) / 2;
            width = Math.Max(width, text.X + text.Width - 5);
            rowY += ImageSize + 5;
        }

        Resize(width + 10, rowY);
    }
}

/// <summary>Single line of text on a small background.</summary>
public sealed class InfoLabel : EditorPanel
{
    private readonly SimpleText _text = EditorStyle.Text("", 12);

    public InfoLabel(string text = "") : base(50, 20, 0xC0303030)
    {
        _text.X = 6;
        _text.Y = 3;
        AddChild(_text);
        SetText(text);
    }

    public void SetText(string text)
    {
        _text.SetText(text);
        Resize(_text.Width + 12, _text.Height + 6);
    }
}

public sealed class NotificationView : Sprite
{
    private readonly SimpleText _text = new(new TextConfig
    {
        Text = "", FontSize = 20, FontType = FontType.Bold, Color = 0xFFFFFF, OutlineColor = 0x0, OutlineThickness = 1f,
        Glow = 0.5f
    });

    private double _remainingMs;
    private int _stageWidth = 800;

    public NotificationView()
    {
        AddChild(_text);
        _text.Visible = false;
        AddEventListener(Event.EnterFrame, OnEnterFrame);
    }

    /// <param name="durationMs">-1 keeps it until <see cref="Clear"/>.</param>
    public void Show(string text, float size = 20, double durationMs = 2000)
    {
        _text.SetFontSize(size);
        _text.SetText(text);
        _text.Alpha = 1f;
        _text.Visible = true;
        _remainingMs = durationMs;
        UpdatePosition(_stageWidth);
    }

    public void Clear() => _text.Visible = false;

    public void UpdatePosition(int stageWidth)
    {
        _stageWidth = stageWidth;
        _text.X = (stageWidth - _text.Width) / 2;
        _text.Y = 60;
    }

    private void OnEnterFrame()
    {
        if (!_text.Visible || _remainingMs < 0) return;
        _remainingMs -= Stage.GameTime.ElapsedMs;
        if (_remainingMs <= 0) _text.Visible = false;
        else if (_remainingMs < 400) _text.Alpha = (float)(_remainingMs / 400);
    }
}

public sealed class BrushOptions : EditorPanel
{
    private static readonly string[] ShapeNames = ["Random", "Random2"];
    private readonly MapEditorScreen _editor;
    private readonly List<Sprite> _options = [];
    private EditorInput _size;
    private EditorInput _chance;
    private EditorCheckBox _replace;
    private EditorButton _shape;
    private bool _updating;

    public BrushOptions(MapEditorScreen editor) : base(10, 10, 0xC0303030)
    {
        _editor = editor;
        Visible = false;
    }

    public void OnToolChanged(int toolId)
    {
        foreach (var o in _options) RemoveChild(o);
        _options.Clear();
        _size = _chance = null;
        _replace = null;
        _shape = null;

        var brush = _editor.UserBrush;
        if (toolId == METool.ShapeId)
        {
            _shape = new EditorButton("Shape: " + ShapeName(brush.BrushShape), () =>
            {
                var next = brush.BrushShape % ShapeNames.Length + 1;
                brush.SetBrushShape(next);
            }, 110, 12);
            _options.Add(_shape);
            _chance = new EditorInput("Chance:", 30, brush.Chance.ToString(), true, 3, 12, t =>
            {
                if (!_updating && int.TryParse(t, out var v) && v > 0) brush.SetChance(v);
            });
            _options.Add(_chance);
        }

        if (toolId is METool.DrawId or METool.EraserId or METool.ShapeId)
        {
            _replace = new EditorCheckBox("Replace", brush.Replace, v => brush.SetReplace(v));
            _options.Add(_replace);
            _size = new EditorInput("Size:", 30, brush.Size.ToString(), true, 2, 12, t =>
            {
                if (!_updating && int.TryParse(t, out var v)) brush.SetSize(v);
            });
            _options.Add(_size);
        }

        var x = 6;
        foreach (var o in _options)
        {
            AddChild(o);
            o.X = x;
            o.Y = 5;
            x += o.Width + 10;
        }

        foreach (var o in _options) o.Y = 5 + (24 - o.Height) / 2;

        Visible = _options.Count > 0;
        Resize(x - 4, 34);
    }

    private static string ShapeName(int shape) =>
        shape >= 1 && shape <= ShapeNames.Length ? ShapeNames[shape - 1] : ShapeNames[0];

    public void UpdateBrush(MEBrush brush)
    {
        _updating = true;
        if (_size != null && !_size.Focused && _size.IntValue != brush.Size) _size.SetText(brush.Size.ToString());
        _replace?.SetValue(brush.Replace);
        _shape?.SetText("Shape: " + ShapeName(brush.BrushShape));
        _updating = false;
    }
}

public sealed class TileHotkeys : EditorPanel
{
    private const int IconSize = 20;
    private readonly (DrawType type, int element)?[] _hotkeys = new (DrawType, int)?[10];
    private readonly List<Sprite> _entries = [];
    private readonly QuadBatch _selectSquare = new(TextureType.Color);
    private readonly Dictionary<int, int> _entryY = new();
    public Action<int> OnHotkeyClick;

    public TileHotkeys() : base(30, 30, 0xC0565656)
    {
        AddChild(_selectSquare);
        Visible = false;
    }

    public (DrawType type, int element)? Get(int number) => _hotkeys[number];

    public void SetHotkey(int number, DrawType type, int element)
    {
        _hotkeys[number] = (type, element);
        Rebuild();
    }

    public void SwitchTo(int number)
    {
        _selectSquare.Clear();
        if (_entryY.TryGetValue(number, out var y))
            _selectSquare.AddOutline(3, y - 2, IconSize + 4, IconSize + 4, 2, 0xFFFFFFFF);
        _selectSquare.Commit();
    }

    private void Rebuild()
    {
        foreach (var e in _entries) RemoveChild(e);
        _entries.Clear();
        _entryY.Clear();

        var y = 7;
        for (var i = 1; i <= 10; i++)
        {
            var number = i % 10; // 0 is drawn last
            if (_hotkeys[number] is not { } hk) continue;

            var entry = new Sprite { MouseEnabled = true };
            var icon = new ElementIcon(IconSize);
            icon.Set(hk.type, hk.element);
            entry.AddChild(icon);
            var text = new SimpleText(new TextConfig
            {
                Text = number.ToString(), FontSize = 11, FontType = FontType.Bold, Color = 0xFFFFFF, OutlineColor = 0,
                OutlineThickness = 1f
            });
            text.X = (IconSize - text.Width) / 2;
            text.Y = (IconSize - text.Height) / 2;
            entry.AddChild(text);
            entry.X = 5;
            entry.Y = y;
            entry.AddEventListener(MouseEvent.LeftClick, () => OnHotkeyClick?.Invoke(number));
            _entryY[number] = y;
            _entries.Add(entry);
            AddChild(entry);
            y += IconSize + 6;
        }

        SetChildIndex(_selectSquare, NumChildren - 1);
        Visible = _entries.Count > 0;
        Resize(30, y + 1);
    }
}

/// <summary>Property filter for the object list ("<" button next to the element list).</summary>
public sealed class ObjectFilterView : Sprite
{
    private readonly DrawElementListView _list;
    private readonly EditorButton _arrow;
    private readonly EditorPanel _panel;
    private readonly EditorInput _prop;
    private readonly EditorInput _value;
    private readonly List<Sprite> _filterRows = [];
    private readonly int _listTop;

    public ObjectFilterView(DrawElementListView list, MapEditorScreen editor)
    {
        _list = list;
        _arrow = new EditorButton("<", Toggle, 20, 14, 26);
        _arrow.OnHover = () =>
            editor.ShowTooltip(
                "Object property filter\nOnly objects whose xml matches every filter are listed.\nLeave the value blank to require the property.",
                _arrow);
        _arrow.OnHoverEnd = () => editor.HideTooltip(_arrow);
        AddChild(_arrow);

        _panel = new EditorPanel(200, 100);
        _panel.Visible = false;
        AddChild(_panel);

        var title = EditorStyle.Text("Property filter", 12, EditorStyle.TitleColor, true);
        title.X = 8;
        title.Y = 5;
        _panel.AddChild(title);

        _prop = new EditorInput("Property:", 110, "", false, 40, 12);
        _prop.X = 8;
        _prop.Y = title.Y + title.Height + 6;
        _panel.AddChild(_prop);

        _value = new EditorInput("Value:    ", 110, "", false, 40, 12);
        _value.X = 8;
        _value.Y = _prop.Y + _prop.Height + 4;
        _panel.AddChild(_value);

        var add = new EditorButton("Add", OnAdd, 60, 12);
        add.X = 8;
        add.Y = _value.Y + _value.Height + 6;
        _panel.AddChild(add);
        _listTop = add.Y + add.ButtonHeight + 6;

        RebuildFilters();
    }

    private void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        _arrow.SetText(_panel.Visible ? ">" : "<");
    }

    private void OnAdd()
    {
        var prop = _prop.Text.Trim();
        if (prop == "") return;
        _list.AddPropertyFilter(prop, _value.Text.Trim());
        _prop.SetText("");
        _value.SetText("");
        RebuildFilters();
    }

    private void RebuildFilters()
    {
        foreach (var r in _filterRows) _panel.RemoveChild(r);
        _filterRows.Clear();

        var y = _listTop;
        foreach (var (prop, value) in _list.PropertyFilters.ToArray())
        {
            var p = prop;
            var row = new EditorButton($"{prop} = {(value == "" ? "true" : value)}   x", () =>
            {
                _list.RemovePropertyFilter(p);
                RebuildFilters();
            }, 184, 11);
            row.X = 8;
            row.Y = y;
            y += row.ButtonHeight + 3;
            _filterRows.Add(row);
            _panel.AddChild(row);
        }

        _panel.Resize(200, y + 4);
        _panel.X = -_panel.PanelWidth - 4;
    }
}

public sealed class DebugView : EditorPanel
{
    private readonly SimpleText _text = EditorStyle.Text("", 12, 0xFFFFFF);
    private double _accum;
    private int _frames;
    private double _fps;

    public DebugView() : base(10, 10, 0xC0202020)
    {
        _text.X = 6;
        _text.Y = 4;
        AddChild(_text);
        Visible = false;
    }

    public void UpdateStats(double elapsedMs, MapView view)
    {
        _frames++;
        _accum += elapsedMs;
        if (_accum < 500) return;
        _fps = _frames * 1000 / _accum;
        _frames = 0;
        _accum = 0;

        var sb = new StringBuilder();
        sb.Append($"FPS: {_fps:0}\nMemory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        if (view != null)
            sb.Append($"\nMap: {view.MapData.MapWidth}x{view.MapData.MapHeight}  Zoom: {view.ZoomLevel}%");
        _text.SetText(sb.ToString());
        Resize(_text.Width + 12, _text.Height + 8);
    }
}