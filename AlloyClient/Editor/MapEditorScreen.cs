using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AlloyClient.Display;
using AlloyClient.Screens;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Input;
using Common;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace AlloyClient.Editor;

/// <summary>
/// The standalone realm editor (RealmEditorLib's MainView), ported to the C# client. Opened from the MAP button on the
/// title screen. The instance is kept while the client runs, so leaving and coming back keeps the opened maps.
/// </summary>
public sealed class MapEditorScreen : Screen
{
    private const int MaxZoom = 1000;
    private const int T = TileMapView.TileSize;

    private static readonly Logger Log = new(typeof(MapEditorScreen));
    private static MapEditorScreen _instance;

    public static MapEditorScreen GetOrCreate() => _instance ??= new MapEditorScreen();

    // Layers
    private readonly ColorRect _background;
    private readonly Sprite _mapContainer = new();
    private readonly ColorRect _inputLayer;
    private readonly Sprite _uiLayer = new();
    private readonly Sprite _windowLayer = new();

    // Maps
    private readonly Dictionary<int, MapView> _maps = new();
    private int _nextMapId;
    public MapView MapView { get; private set; }
    private MapData MapData => MapView?.MapData;

    // Editing state
    public readonly MEBrush UserBrush = new(DrawType.Ground);
    private readonly MEClipboard _clipboard = new();
    private readonly TimeControl _timeControl = new();
    private readonly AutoMapSaver _autoSaver = new();
    public METool SelectedTool { get; private set; }
    private METool _previousTool; // Tool to go back to after a shift+drag selection
    private (DrawType type, int element)? _hoveredElement;
    private bool _defaultMapCreated;
    private bool _defaultElementsSet;

    // Mouse state on the map
    private bool _mouseDown;
    private bool _dragging;
    private bool _middleDown;
    private Vector2i? _lastMousePos;

    // UI
    private readonly EditorButton _loadButton;
    private readonly EditorButton _newButton;
    private readonly EditorButton _saveButton;
    private readonly EditorButton _saveWmapButton;
    private readonly EditorButton _testButton;
    private readonly EditorButton _backButton;
    private readonly MapSelectorView _mapSelector;
    private readonly EditorPanel _settingsView;
    private readonly EditorInput _zoomInput;
    private readonly EditorCheckBox _gridCheckbox;
    private readonly EditorSwitch _drawTypeSwitch;
    private readonly InfoLabel _mapInfoPanel;
    private readonly InfoLabel _selectionInfoPanel;
    private readonly TileInfoPanel _tileInfoPanel;
    private readonly DrawElementListView _drawElementsList;
    private readonly ObjectFilterView _objectFilterView;
    private readonly MapToolbar _toolBar;
    private readonly BrushOptions _brushOptions;
    private readonly TileHotkeys _tileHotkeys;
    private readonly NotificationView _notifications;
    private readonly DebugView _debugView;
    private readonly EditorTooltip _tooltip;

    // Windows
    private readonly CreateMapWindow _createMapWindow;
    private readonly MapDimensionsWindow _mapDimensionsWindow;
    private readonly EditTileNameWindow _editNameWindow;
    private readonly PathPromptWindow _pathPrompt;
    private readonly TextWindow _closeMapPrompt;
    private int _closingMapId = -1;
    private Action<string> _pathPromptCallback;

    // Small delayed actions (lets a notification render before a slow operation runs)
    private readonly List<(double timeLeft, Action action)> _timers = [];

    private int _stageWidth = 1280;
    private int _stageHeight = 720;
    private bool _zoomInputUpdating;
    private Key? _ignoreKeyUp; // Key that closed/submitted a window, its key up shouldn't trigger a hotkey

    private MapEditorScreen()
    {
        _background = new ColorRect(new ColorRectConfig { Width = 1, Height = 1, Color = 0x1B1B1B });
        AddChild(_background);
        AddChild(_mapContainer);

        _inputLayer = new ColorRect(new ColorRectConfig
            { Width = 1, Height = 1, Color = 0x0, Alpha = 0f, MouseEnabled = true });
        _inputLayer.AddEventListener(MouseEvent.LeftDown, OnLeftDown);
        _inputLayer.AddEventListener(MouseEvent.LeftUp, OnLeftUp);
        _inputLayer.AddEventListener(MouseEvent.MiddleDown, OnMiddleDown);
        _inputLayer.AddEventListener(MouseEvent.MiddleUp, OnMiddleUp);
        _inputLayer.AddEventListener(MouseEvent.MouseMove, OnMouseMove);
        _inputLayer.AddEventListener(MouseEvent.MouseOut, OnRollOut);
        _inputLayer.AddEventListener(MouseEvent.ScrollVertical, new Action<MouseEvent>(OnMouseWheel));
        AddChild(_inputLayer);

        AddChild(_uiLayer);
        AddChild(_windowLayer);

        SelectedTool = METool.GetTool(METool.DrawId, this);
        UserBrush.Changed += OnBrushChanged;

        // Top buttons
        _loadButton = AddUi(new EditorButton("Load", OnLoadClick));
        _newButton = AddUi(new EditorButton("New", OnNewClick));
        _saveButton = AddUi(new EditorButton("Save JSON", () => OnSaveClick(false)));
        _saveWmapButton = AddUi(new EditorButton("Save Wmap", () => OnSaveClick(true)));
        _backButton = AddUi(new EditorButton("Back", OnBackClick));

        _mapSelector = AddUi(new MapSelectorView());
        _mapSelector.OnMapSelect = OnMapSelected;
        _mapSelector.OnMapClose = OnMapCloseRequest;

        // Left settings box
        _settingsView = AddUi(new EditorPanel(10, 10));
        var settingsTitle = EditorStyle.Text("Settings", 12, EditorStyle.TitleColor, true);
        settingsTitle.X = 8;
        settingsTitle.Y = 5;
        _settingsView.AddChild(settingsTitle);
        _zoomInput = new EditorInput("Zoom", 40, "100", true, 3, 13, OnZoomInputChange);
        _gridCheckbox = new EditorCheckBox("Grid (Shift+G)", false, OnGridClick);
        var autoSaveCheckbox = new EditorCheckBox("Autosave", true, v => _autoSaver.Disabled = !v);
        _drawTypeSwitch = new EditorSwitch(62, "Ground", "Objects", "Regions");
        _drawTypeSwitch.OnSwitch = OnDrawTypeSwitch;
        var settingsY = settingsTitle.Y + settingsTitle.Height + 6;
        _testButton = new EditorButton("Test map", OnTestMapClick, 120);
        foreach (var s in new Sprite[] { _zoomInput, _gridCheckbox, autoSaveCheckbox, _drawTypeSwitch, _testButton })
        {
            s.X = 8;
            s.Y = settingsY;
            settingsY += s.Height + 8;
            _settingsView.AddChild(s);
        }

        _settingsView.Resize(Math.Max(_drawTypeSwitch.Width, 120) + 16, settingsY);

        _tileInfoPanel = AddUi(new TileInfoPanel());
        _tileInfoPanel.Visible = false;

        _drawElementsList = AddUi(new DrawElementListView(this));
        _drawElementsList.OnSelect = OnDrawElementSelected;

        _toolBar = AddUi(new MapToolbar(this));

        _objectFilterView = AddUi(new ObjectFilterView(_drawElementsList, this));

        _mapInfoPanel = AddUi(new InfoLabel(""));
        _mapInfoPanel.MouseEnabled = true;
        _mapInfoPanel.AddEventListener(MouseEvent.LeftClick, OnMapDimensionsClick);
        _mapInfoPanel.Visible = false;

        _selectionInfoPanel = AddUi(new InfoLabel(""));
        _selectionInfoPanel.Visible = false;

        _brushOptions = AddUi(new BrushOptions(this));

        _tileHotkeys = AddUi(new TileHotkeys());
        _tileHotkeys.OnHotkeyClick = SwitchToHotkey;

        _notifications = AddUi(new NotificationView());
        _debugView = AddUi(new DebugView());

        // Windows
        _createMapWindow = AddWindow(new CreateMapWindow());
        _createMapWindow.OnOk = OnMapCreate;
        _mapDimensionsWindow = AddWindow(new MapDimensionsWindow());
        _mapDimensionsWindow.OnOk = OnMapDimensionsChange;
        _editNameWindow = AddWindow(new EditTileNameWindow());
        _editNameWindow.OnOk = OnEditName;
        _pathPrompt = AddWindow(new PathPromptWindow());
        _pathPrompt.OnOk = () => _pathPromptCallback?.Invoke(_pathPrompt.Path);
        _closeMapPrompt = AddWindow(new TextWindow("Close map", "This map has unsaved changes. Save it before closing?",
            "Save", "Discard"));
        _closeMapPrompt.OnOk = () => SaveThenClose(_closingMapId);
        _closeMapPrompt.OnCancel = () => CloseMap(_closingMapId);

        _tooltip = new EditorTooltip();

        _drawElementsList.SetContent(DrawType.Ground);
        OnToolSwitch(METool.DrawId);

        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    private T AddUi<T>(T sprite) where T : Sprite => _uiLayer.AddChild(sprite);

    private T AddWindow<T>(T window) where T : EditorWindow => _windowLayer.AddChild(window);

#region Lifecycle

    private void OnAddedToStage()
    {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        Stage.AddEventListener(KeyboardEvent.KeyDown, new Action<KeyboardEvent>(OnKeyDown));
        Stage.AddEventListener(KeyboardEvent.KeyUp, new Action<KeyboardEvent>(OnKeyUp));
        AddEventListener(Event.EnterFrame, OnEnterFrame);

        // Assets might have arrived (or changed) since the list was built
        _drawElementsList.SetContent(UserBrush.ElementType);
        UpdateDrawElements();
        OnResize(new ResizeEvent(ResizeEvent.Resize, Stage.StageWidth, Stage.StageHeight));

        if (!EditorAssets.IsLoaded)
            _notifications.Show("Assets aren't loaded yet, connect to the server first", 16, 5000);
    }

    private void OnRemovedFromStage()
    {
        Stage?.RemoveEventListener(ResizeEvent.Resize, OnResize);
        Stage?.RemoveEventListener(KeyboardEvent.KeyDown, new Action<KeyboardEvent>(OnKeyDown));
        Stage?.RemoveEventListener(KeyboardEvent.KeyUp, new Action<KeyboardEvent>(OnKeyUp));
        RemoveEventListener(Event.EnterFrame, OnEnterFrame);
        ResetMouseState();
        HideTooltip();
    }

    private void OnEnterFrame()
    {
        var dt = Stage.GameTime.ElapsedMs;

        TryCreateDefaultMap();
        TrySelectDefaultElements();
        foreach (var view in _maps.Values) view.TileMap.RebuildDirty();

        if (_debugView.Visible) _debugView.UpdateStats(dt, MapView);
        if (MapData != null) _autoSaver.TrySaveMap(MapData, dt);

        for (var i = _timers.Count - 1; i >= 0; i--)
        {
            var (timeLeft, action) = _timers[i];
            timeLeft -= dt;
            if (timeLeft > 0)
            {
                _timers[i] = (timeLeft, action);
                continue;
            }

            _timers.RemoveAt(i);
            action();
        }
    }

    private void RunLater(double ms, Action action) => _timers.Add((ms, action));

    protected override void OnResize(ResizeEvent args)
    {
        _stageWidth = Math.Max(1, args.Width);
        _stageHeight = Math.Max(1, args.Height);
        _background.Resize(_stageWidth, _stageHeight);
        _inputLayer.Resize(_stageWidth, _stageHeight);
        _drawElementsList.SetHeight(_stageHeight - 80);
        UpdatePositions();
    }

#endregion

#region Layout

    public void UpdatePositions()
    {
        _notifications.UpdatePosition(_stageWidth);

        _loadButton.X = 15;
        _loadButton.Y = 15;
        _newButton.X = _loadButton.X + _loadButton.ButtonWidth + 10;
        _newButton.Y = _loadButton.Y;
        _saveButton.X = _newButton.X + _newButton.ButtonWidth + 10;
        _saveButton.Y = _loadButton.Y;
        _saveWmapButton.X = _saveButton.X + _saveButton.ButtonWidth + 10;
        _saveWmapButton.Y = _loadButton.Y;
        _backButton.X = _stageWidth - _backButton.ButtonWidth - 15;
        _backButton.Y = _loadButton.Y;

        _mapSelector.X = _loadButton.X;
        _mapSelector.Y = _loadButton.Y + _loadButton.ButtonHeight + 10;

        _mapInfoPanel.X = 15;
        _mapInfoPanel.Y = _stageHeight - _mapInfoPanel.PanelHeight - 15;

        _settingsView.X = 15;
        _settingsView.Y = (_stageHeight - _settingsView.PanelHeight) / 2;
        var minSettingsY = _mapSelector.Y + _mapSelector.PanelHeight + 15; // Don't overlap the map selector
        if (_settingsView.Y < minSettingsY) _settingsView.Y = minSettingsY;

        _drawElementsList.X = _stageWidth - DrawElementListView.PanelWidth_ - 15;
        _drawElementsList.Y = _backButton.Y + _backButton.ButtonHeight + 15;

        _tileInfoPanel.X = _drawElementsList.X - _tileInfoPanel.PanelWidth - 15;
        _tileInfoPanel.Y = _stageHeight - _tileInfoPanel.PanelHeight - 15;

        _selectionInfoPanel.X = _tileInfoPanel.X - _selectionInfoPanel.PanelWidth - 5;
        _selectionInfoPanel.Y = _stageHeight - _selectionInfoPanel.PanelHeight - 15;

        _toolBar.X = _drawElementsList.X - _toolBar.PanelWidth - 6;
        _toolBar.Y = (_stageHeight - _toolBar.PanelHeight) / 2;

        _objectFilterView.X = _drawElementsList.X - 24;
        _objectFilterView.Y = _drawElementsList.Y;

        _brushOptions.X = _drawElementsList.X - _brushOptions.PanelWidth - 8;
        _brushOptions.Y = _loadButton.Y;

        _tileHotkeys.X = _drawElementsList.X - _tileHotkeys.PanelWidth - 8;
        _tileHotkeys.Y = _toolBar.Y - _tileHotkeys.PanelHeight - 8;

        _debugView.X = 10;
        _debugView.Y = _mapInfoPanel.Y - _debugView.PanelHeight - 10;

        foreach (var window in new EditorWindow[]
                     { _createMapWindow, _mapDimensionsWindow, _editNameWindow, _pathPrompt, _closeMapPrompt })
            window.CenterOn(_stageWidth, _stageHeight);

        PositionMapView();
    }

    private void PositionMapView()
    {
        if (MapView == null) return;

        var scale = GetScale(MapView);
        MapView.Scale = new Vector2(scale);
        MapView.X = (int)((_stageWidth - MapData.MapWidth * T * scale) / 2 +
                          MapView.MapOffset.X * MapView.ZoomLevel / MaxZoom);
        MapView.Y = (int)((_stageHeight - MapData.MapHeight * T * scale) / 2 +
                          MapView.MapOffset.Y * MapView.ZoomLevel / MaxZoom);

        // Hide chunks that are off screen
        MapView.TileMap.UpdateVisibility(-MapView.X / scale, -MapView.Y / scale, _stageWidth / scale,
            _stageHeight / scale);
    }

    private static float GetScale(MapView view) => Math.Max(0.01f, view.ZoomLevel / 100f);

#endregion

#region Map input

    private bool IsWindowOpened() => _createMapWindow.Visible || _mapDimensionsWindow.Visible ||
                                     _editNameWindow.Visible ||
                                     _pathPrompt.Visible || _closeMapPrompt.Visible;

    private MapHistory History => MapView == null ? null : _timeControl.GetHistory(MapView.Id);

    private Vector2i? GetMouseTilePosition()
    {
        if (MapView == null) return null;
        var mouse = MouseInput.GetMousePosition();
        var scale = GetScale(MapView);
        var x = (int)MathF.Floor((mouse.X - MapView.X) / (T * scale));
        var y = (int)MathF.Floor((mouse.Y - MapView.Y) / (T * scale));
        if (!MapData.InBounds(x, y)) return null;
        return new Vector2i(x, y);
    }

    private void OnLeftDown()
    {
        _dragging = false;
        _mouseDown = true;
    }

    private void OnLeftUp()
    {
        if (!_mouseDown) return;
        _mouseDown = false;
        if (!_dragging)
        {
            OnTileClick();
        }
        else
        {
            _dragging = false;
            OnMouseDragEnd();
        }
    }

    private void OnMiddleDown()
    {
        _middleDown = true;
        _lastMousePos = MouseInput.GetMousePosition();
    }

    private void OnMiddleUp()
    {
        _middleDown = false;
        _lastMousePos = null;
    }

    private void OnMouseMove()
    {
        OnMouseMoved();

        if (_mouseDown)
        {
            _dragging = true;
            // The draw tool uses shift for its rectangle fill, the other tools switch to select
            if (KeyboardInput.IsShiftDown() && SelectedTool.Id != METool.DrawId) OnShiftMouseDrag();
            else OnMouseDrag();
        }

        if (_middleDown) DragMap();
    }

    // The mouse left the map (went over a panel), end any drag like the original's ROLL_OUT
    private void OnRollOut()
    {
        if (_dragging) OnMouseDragEnd();
        ResetMouseState();
    }

    private void ResetMouseState()
    {
        _dragging = false;
        _mouseDown = false;
        _middleDown = false;
        _lastMousePos = null;
    }

    private void OnMouseWheel(MouseEvent e)
    {
        if (MapView == null || e.Delta == 0) return;

        var toolUsesBrush = SelectedTool.Id is METool.DrawId or METool.EraserId or METool.ShapeId;
        if (KeyboardInput.IsCtrlDown() && toolUsesBrush)
        {
            // Brush size
            UserBrush.SetSize(UserBrush.Size + Math.Sign(e.Delta));
            return;
        }

        var oldZoom = MapView.ZoomLevel;
        var step = oldZoom / 3 + 1;
        var zoom = Math.Clamp(e.Delta > 0 ? oldZoom + step : oldZoom - step, 1, MaxZoom);
        if (zoom == oldZoom) return;

        // Keep the map point under the cursor in place
        var mouse = MouseInput.GetMousePosition();
        var oldScale = GetScale(MapView);
        var mapPxX = (mouse.X - MapView.X) / oldScale;
        var mapPxY = (mouse.Y - MapView.Y) / oldScale;

        MapView.ZoomLevel = zoom;
        var scale = GetScale(MapView);
        var centerX = (_stageWidth - MapData.MapWidth * T * scale) / 2;
        var centerY = (_stageHeight - MapData.MapHeight * T * scale) / 2;
        MapView.MapOffset.X = (mouse.X - mapPxX * scale - centerX) * MaxZoom / zoom;
        MapView.MapOffset.Y = (mouse.Y - mapPxY * scale - centerY) * MaxZoom / zoom;

        UpdateZoomLevel();
    }

    private void DragMap()
    {
        if (MapView == null || IsWindowOpened()) return;
        var mouse = MouseInput.GetMousePosition();
        _lastMousePos ??= mouse;
        var delta = mouse - _lastMousePos.Value;
        MapView.MapOffset += new Vector2(delta.X, delta.Y) * MaxZoom / MapView.ZoomLevel;
        _lastMousePos = mouse;
        PositionMapView();
    }

    private void OnMouseDrag()
    {
        if (MapView == null || IsWindowOpened()) return;
        SelectedTool.MouseDrag(GetMouseTilePosition(), History);
    }

    // Holding shift switches to the select tool while dragging, the previous tool comes back when the drag ends
    private void OnShiftMouseDrag()
    {
        if (MapView == null || IsWindowOpened()) return;
        if (SelectedTool.Id != METool.SelectId)
        {
            _previousTool = SelectedTool;
            OnToolSwitch(METool.SelectId);
        }

        SelectedTool.MouseDrag(GetMouseTilePosition(), History);
    }

    private void OnMouseDragEnd()
    {
        if (MapView == null || IsWindowOpened()) return;
        SelectedTool.MouseDragEnd(GetMouseTilePosition(), History);
        if (_previousTool != null)
        {
            OnToolSwitch(_previousTool.Id);
            _previousTool = null;
        }
    }

    private void OnTileClick()
    {
        var tilePos = GetMouseTilePosition();
        if (tilePos == null || MapView == null || IsWindowOpened()) return;
        SelectedTool.TileClick(tilePos, History);
    }

    private void OnMouseMoved()
    {
        if (MapView == null) return;
        UpdateSelectionPanel();

        var tilePos = GetMouseTilePosition();
        if (tilePos == null)
        {
            _tileInfoPanel.Visible = false;
            MapView.HideOverlays();
            return;
        }

        if (IsWindowOpened()) return;

        UpdateTileInfoPanel(tilePos.Value);
        MapView.HideOverlays();
        SelectedTool.MouseMoved(tilePos, History);
    }

    private void UpdateSelectionPanel()
    {
        var sel = MapView?.Selection;
        if (sel == null || sel.IsEmpty)
        {
            _selectionInfoPanel.Visible = false;
            return;
        }

        _selectionInfoPanel.Visible = true;
        _selectionInfoPanel.SetText($"Selection  X: {sel.StartX} Y: {sel.StartY}  {sel.Width}x{sel.Height}");
        _selectionInfoPanel.X = _tileInfoPanel.X - _selectionInfoPanel.PanelWidth - 5;
        _selectionInfoPanel.Y = _stageHeight - _selectionInfoPanel.PanelHeight - 15;
    }

    private void UpdateTileInfoPanel(Vector2i pos)
    {
        var tile = MapView.TileMap.GetTileData(pos.X, pos.Y);
        if (tile == null)
        {
            _tileInfoPanel.Visible = false;
            return;
        }

        _tileInfoPanel.Visible = true;
        _tileInfoPanel.SetInfo(pos.X, pos.Y, tile);
        _tileInfoPanel.X = _drawElementsList.X - _tileInfoPanel.PanelWidth - 15;
        _tileInfoPanel.Y = _stageHeight - _tileInfoPanel.PanelHeight - 15;
        UpdateSelectionPanel();
    }

#endregion

#region Keyboard

    private void OnKeyDown(KeyboardEvent e)
    {
        if (Parent == null) return;

        if (IsWindowOpened())
        {
            if (e.Key is Key.Return or Key.KeypadEnter) SubmitOpenWindow();
            else if (e.Key == Key.Escape) CloseOpenWindow();
            _ignoreKeyUp = e.Key;
            return;
        }

        if (TextInput.IsAnyFocused) return;

        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            RefreshDrawRect();
        }

        if (e.Ctrl)
        {
            switch (e.Key)
            {
                case Key.Z:
                    OnUndoAction();
                    break;
                case Key.Y:
                    OnRedoAction();
                    break;
            }
        }
    }

    private void OnKeyUp(KeyboardEvent e)
    {
        if (_ignoreKeyUp == e.Key)
        {
            _ignoreKeyUp = null;
            return;
        }

        if (Parent == null || IsWindowOpened() || TextInput.IsAnyFocused) return;

        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            RefreshDrawRect();
            return;
        }

        if (e.Ctrl)
        {
            switch (e.Key)
            {
                case Key.C:
                    OnCopy();
                    break;
                case Key.V:
                    OnPaste();
                    break;
            }

            return;
        }

        if (e.Shift)
        {
            if (e.Key == Key.G) OnGridToggleKey();
            return;
        }

        if (e.Alt) return;

        switch (e.Key)
        {
            case Key.M: OnToolSwitch(METool.SelectId); break;
            case Key.D: OnToolSwitch(METool.DrawId); break;
            case Key.L: OnToolSwitch(METool.LineId); break;
            case Key.S: OnToolSwitch(METool.ShapeId); break;
            case Key.F: OnToolSwitch(METool.FillId); break;
            case Key.A: OnToolSwitch(METool.SampleId); break;
            case Key.E: OnToolSwitch(METool.EraserId); break;
            case Key.I: OnToolSwitch(METool.EditId); break;
            case Key.T: _drawTypeSwitch.SelectNext(); break;
            case Key.Escape: OnClearSelection(); break;
            case Key.Delete: OnDeleteSelection(); break;
            case Key.UpArrow: MoveSelection(0, -1); break;
            case Key.DownArrow: MoveSelection(0, 1); break;
            case Key.LeftArrow: MoveSelection(-1, 0); break;
            case Key.RightArrow: MoveSelection(1, 0); break;
            case Key.F3: _debugView.Visible = !_debugView.Visible; break;
            case >= Key.D0 and <= Key.D9: OnTileHotkey(e.Key - Key.D0); break;
        }
    }

    // Shift pressed/released with the draw tool: show or hide its rectangle right away, without waiting for a mouse move
    private void RefreshDrawRect()
    {
        if (MapView == null || SelectedTool.Id != METool.DrawId) return;
        var tilePos = GetMouseTilePosition();
        if (tilePos == null) return;

        MapView.HideOverlays();
        SelectedTool.MouseMoved(tilePos, History);
    }

    private void SubmitOpenWindow()
    {
        foreach (var w in new EditorWindow[]
                     { _createMapWindow, _mapDimensionsWindow, _editNameWindow, _pathPrompt, _closeMapPrompt })
            if (w.Visible)
            {
                w.Submit();
                return;
            }
    }

    private void CloseOpenWindow()
    {
        foreach (var w in new EditorWindow[]
                     { _createMapWindow, _mapDimensionsWindow, _editNameWindow, _pathPrompt, _closeMapPrompt })
            if (w.Visible)
            {
                w.Close(false);
                return;
            }
    }

#endregion

#region Tools

    public void OnToolSwitch(int toolId)
    {
        SetSelectedTool(toolId);
        _toolBar.SetSelected(toolId);
        _brushOptions.OnToolChanged(toolId);
        UserBrush.SetBrushShape(toolId == METool.ShapeId ? MEBrush.ShapeRandom : 0);
        UpdatePositions();
    }

    private void SetSelectedTool(int toolId)
    {
        _lastMousePos = null;
        SelectedTool.Reset();
        SelectedTool = METool.GetTool(toolId, this);
        if (MapView == null) return;

        if (toolId != METool.SelectId) MapView.HighlightTile(-1, -1);
        MapView.HideOverlays();
        SelectedTool.Init(GetMouseTilePosition(), History);
    }

    private void OnBrushChanged()
    {
        _brushOptions.UpdateBrush(UserBrush);
        var tilePos = GetMouseTilePosition();
        if (tilePos is not { } p || MapView == null || IsWindowOpened()) return;
        if (SelectedTool is MEPencilTool { RectActive: true }) return; // Keep showing the rectangle

        if (SelectedTool.Id == METool.EraserId) MapView.DrawBrushOutline(p.X, p.Y, UserBrush);
        else if (SelectedTool.Id is METool.DrawId or METool.ShapeId) MapView.DrawBrushTiles(p.X, p.Y, UserBrush);
        SelectedTool.BrushChanged(tilePos, History);
    }

    private void OnUndoAction()
    {
        if (MapView == null) return;
        if (_timeControl.UndoLastAction(MapView.Id)) _notifications.Show("Undone", 18, 1000);
    }

    private void OnRedoAction()
    {
        if (MapView == null) return;
        if (_timeControl.RedoLastUndoneAction(MapView.Id)) _notifications.Show("Redone", 18, 1000);
    }

    private void OnCopy()
    {
        if (MapView == null || MapView.Selection.IsEmpty) return;
        _clipboard.Clear();
        MapView.CopySelectionToClipboard(_clipboard);
        _notifications.Show("Copy");
    }

    private void OnPaste()
    {
        if (MapView == null || GetMouseTilePosition() is not { } pos) return;
        MapView.PasteFromClipboard(_clipboard, pos.X, pos.Y, History);
        _notifications.Show("Paste");
    }

    private void OnClearSelection()
    {
        if (MapView == null) return;
        if (SelectedTool.Id == METool.SelectId) SelectedTool.Reset();
        if (!MapView.Selection.IsEmpty) MapView.RecordSelectionClear(History);
        MapView.ClearTileSelection();
        UpdateSelectionPanel();
    }

    private void OnDeleteSelection()
    {
        if (MapView == null || MapView.Selection.IsEmpty) return;
        MEEraserTool.EraseSelection(MapView, UserBrush, MapView.Selection, History);
    }

    private void MoveSelection(int dx, int dy)
    {
        if (MapView == null || SelectedTool.Id != METool.SelectId) return;
        ((MESelectTool)METool.GetTool(METool.DrawId, this)).DragSelection(dx, dy, History);
        UpdateSelectionPanel();
    }

    private void OnGridClick(bool value) => MapView?.SetGrid(value);

    private void OnGridToggleKey()
    {
        if (MapView == null) return;
        _gridCheckbox.SetValue(MapView.ToggleGrid());
    }

#endregion

#region Draw elements

    private void OnDrawTypeSwitch(int idx)
    {
        UserBrush.ElementType = (DrawType)idx;
        _drawElementsList.ResetFilters();
        _drawElementsList.SetContent(UserBrush.ElementType);
        UpdateDrawElements();
        RefreshBrushOverlay();
    }
    
    // Picks the first entry of the ground/object/region lists as the brush's starting element
    private void TrySelectDefaultElements() {
        if (_defaultElementsSet || !EditorAssets.IsLoaded) return;

        var changed = false;
        var allSet = true;
        foreach (var type in Enum.GetValues<DrawType>()) {
            if (UserBrush.GetElement(type) != MEBrush.EmptyValue(type)) continue; // Already chosen (hotkey, picker...)

            var types = EditorAssets.GetTypes(type);
            if (types.Count == 0) {
                allSet = false; // This list isn't filled yet, try again next frame
                continue;
            }

            UserBrush.SetElement(type, types[0]);
            changed = true;
        }

        _defaultElementsSet = allSet;
        if (!changed) return;

        _drawElementsList.SetContent(UserBrush.ElementType);
        UpdateDrawElements();
        RefreshBrushOverlay();
    }

    // Selects the brush's current element in the list
    public void UpdateDrawElements()
    {
        var element = UserBrush.GetElement(UserBrush.ElementType);
        _drawElementsList.SetSelected(element == MEBrush.EmptyValue(UserBrush.ElementType) ? int.MinValue : element);
    }

    private void OnDrawElementSelected(int element)
    {
        UserBrush.SetElement(UserBrush.ElementType, element);
        RefreshBrushOverlay();
    }

    private void RefreshBrushOverlay()
    {
        if (MapView == null || GetMouseTilePosition() is not { } p) return;
        if (SelectedTool is MEPencilTool { RectActive: true }) return;
        if (SelectedTool.Id is METool.DrawId or METool.ShapeId) MapView.DrawBrushTiles(p.X, p.Y, UserBrush);
    }

    public void OnElementHover(object cell, DrawType? type, int element)
    {
        if (type == null)
        {
            if (!_tooltip.IsOwner(cell)) return;
            _hoveredElement = null;
            _tooltip.Hide(cell);
            return;
        }

        _hoveredElement = (type.Value, element);
        _tooltip.Show(BuildElementTooltip(type.Value, element), cell);
    }

    private static string BuildElementTooltip(DrawType type, int element)
    {
        var sb = new StringBuilder();
        sb.Append(EditorAssets.GetId(type, element) ?? "Unknown");
        sb.Append($"\nType: 0x{element:X4}");
        var xml = EditorAssets.GetXml(type, element);
        if (xml != null)
        {
            var count = 0;
            foreach (var child in xml.Elements())
            {
                var name = child.Name.LocalName;
                if (name is "Texture" or "AnimatedTexture" or "RandomTexture" or "AltTexture" or "Top" or "Edge"
                    or "Corner" or "InnerCorner")
                    continue;
                if (++count > 10)
                {
                    sb.Append("\n...");
                    break;
                }

                var value = child.HasElements ? "" : child.Value.Trim();
                if (value.Length > 40) value = value[..40] + "...";
                sb.Append(value == "" ? $"\n{name}" : $"\n{name}: {value}");
            }
        }

        sb.Append("\n(press 0-9 to bind a hotkey)");
        return sb.ToString();
    }

    public void ShowTooltip(string text, object owner) => _tooltip.Show(text, owner);

    public void HideTooltip(object owner) => _tooltip.Hide(owner);

    public void HideTooltip() => _tooltip.Hide();

    // Number key: binds the hovered element to it, otherwise switches to the element bound to it
    private void OnTileHotkey(int number)
    {
        if (_hoveredElement is { } hovered)
        {
            _tileHotkeys.SetHotkey(number, hovered.type, hovered.element);
            _notifications.Show($"Hotkey {number}: {EditorAssets.GetId(hovered.type, hovered.element)}", 16, 1500);
            UpdatePositions();
            return;
        }

        SwitchToHotkey(number);
    }

    private void SwitchToHotkey(int number)
    {
        if (_tileHotkeys.Get(number) is not { } hk) return;
        UserBrush.SetElement(hk.type, hk.element);
        _drawTypeSwitch.Select((int)hk.type, true);
        _tileHotkeys.SwitchTo(number);
    }

#endregion

#region Maps

    private void AddMap(MapData data)
    {
        var id = _nextMapId++;
        var view = new MapView(id, data);
        view.OnMapLoaded();
        view.Visible = false;
        _maps[id] = view;
        _mapContainer.AddChild(view);
        _timeControl.CreateHistory(id);

        _mapSelector.AddMap(id, data);
        _mapSelector.SelectMap(id);
        ViewMap(id);
        _notifications.Clear();
    }

    private void ViewMap(int mapId)
    {
        foreach (var view in _maps.Values) view.Visible = false;
        if (!_maps.TryGetValue(mapId, out var map))
        {
            MapView = null;
            _mapInfoPanel.Visible = false;
            _selectionInfoPanel.Visible = false;
            _tileInfoPanel.Visible = false;
            return;
        }

        MapView = map;
        map.Visible = true;
        _gridCheckbox.SetValue(map.GridEnabled);
        _mapInfoPanel.Visible = true;
        _mapInfoPanel.SetText($"Map size: {MapData.MapWidth}x{MapData.MapHeight} (click to change)");
        UpdateSelectionPanel();
        UpdateZoomLevel();
        SelectedTool.Init(GetMouseTilePosition(), History);
    }

    private void TryCreateDefaultMap()
    {
        if (_defaultMapCreated || !EditorAssets.IsLoaded) return; // Retried every frame until the assets arrive
        _defaultMapCreated = true;
        if (_maps.Count > 0) return; // Something was already loaded, don't add another map

        var data = new MapData();
        data.NewMap("map", 128, 128); // Saves as map.jm / map.wmap
        data.SavedChanges = true; // Untouched default map shouldn't ask to be saved when closed
        AddMap(data);
    }

    private void OnMapSelected(int mapId) => ViewMap(mapId);

    private void OnMapCloseRequest(int mapId)
    {
        if (!_maps.TryGetValue(mapId, out var view)) return;
        if (view.MapData.SavedChanges)
        {
            CloseMap(mapId);
            return;
        }

        _closingMapId = mapId;
        _closeMapPrompt.Open();
        UpdatePositions();
    }

    private void SaveThenClose(int mapId)
    {
        if (!_maps.TryGetValue(mapId, out var view)) return;
        SaveMap(view.MapData, false, saved =>
        {
            if (saved) CloseMap(mapId);
        });
    }

    private void CloseMap(int mapId)
    {
        if (!_maps.TryGetValue(mapId, out var view)) return;
        _mapContainer.RemoveChild(view);
        _maps.Remove(mapId);
        _timeControl.EraseHistory(mapId);
        _mapSelector.RemoveMap(mapId);

        if (MapView == view)
        {
            _mapSelector.SelectMap(-1);
            ViewMap(_mapSelector.SelectedMap);
        }
    }

    private void OnLoadClick()
    {
        if (!EditorAssets.IsLoaded)
        {
            _notifications.Show("Assets aren't loaded yet", 18, 2500);
            return;
        }

        if (FileDialogs.TryOpenMap(out var path))
        {
            if (path != null) LoadMapFile(path);
            return;
        }

        // No native dialog on this platform, ask for a path
        _pathPromptCallback = LoadMapFile;
        _pathPrompt.Open(Path.Combine(FileDialogs.LastDirectory, ""));
        UpdatePositions();
    }

    private void LoadMapFile(string path)
    {
        path = path?.Trim().Trim('"');
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Log.Warn($"Map file not found: '{path}'");
            _notifications.Show($"File not found: {path}", 16, 4000);
            return;
        }

        FileDialogs.Remember(path);
        _notifications.Show($"Loading map {Path.GetFileName(path)}...", 18, -1);
        RunLater(100, () =>
        {
            try
            {
                var data = new MapData();
                var error = data.LoadFile(path);
                if (error != null)
                {
                    _notifications.Show(error, 16, 5000);
                    return;
                }

                AddMap(data);
                Log.Info($"Loaded map {path} ({data.MapWidth}x{data.MapHeight})");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to open map {path}: {e}");
                _notifications.Show($"Failed to open map: {e.Message}", 16, 5000);
            }
        });
    }

    private void OnNewClick()
    {
        if (!EditorAssets.IsLoaded)
        {
            _notifications.Show("Assets aren't loaded yet", 18, 2500);
            return;
        }

        _createMapWindow.Open();
        UpdatePositions();
    }

    private void OnMapCreate()
    {
        var data = new MapData();
        data.NewMap(_createMapWindow.MapName, _createMapWindow.MapWidth, _createMapWindow.MapHeight);
        AddMap(data);
    }

    private void OnSaveClick(bool wmap)
    {
        if (MapData == null) return;
        SaveMap(MapData, wmap, null);
    }

    private void SaveMap(MapData data, bool wmap, Action<bool> onDone)
    {
        var ext = wmap ? ".wmap" : ".jm";
        var name = (string.IsNullOrWhiteSpace(data.MapName) ? "untitled" : data.MapName) + ext;

        void Write(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _notifications.Clear();
                onDone?.Invoke(false);
                return;
            }

            if (!path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) path += ext;
            _notifications.Show($"Serializing map to {(wmap ? "WMap" : "JSON")}...", 18, -1);
            RunLater(100, () =>
            {
                // Make sure the notification is visible before serializing
                var error = data.SaveFile(path, wmap);
                if (error != null)
                {
                    _notifications.Show(error, 16, 4000);
                    onDone?.Invoke(false);
                    return;
                }

                data.MapName = Path.GetFileNameWithoutExtension(path);
                data.MarkSaved(); // refresh the selector's name
                _notifications.Show($"Map saved in {(wmap ? "WMap" : "JSON")} format!");
                onDone?.Invoke(true);
            });
        }

        if (FileDialogs.TrySaveMap(name, wmap, out var selected))
        {
            Write(selected);
            return;
        }

        _pathPromptCallback = Write;
        _pathPrompt.Open(Path.Combine(FileDialogs.LastDirectory, name));
        UpdatePositions();
    }

    private void OnTestMapClick()
    {
        if (MapData == null)
        {
            _notifications.Show("Open or create a map to test", 18, 2500);
            return;
        }

        if (!MapTestSession.CanTest(out var title, out var reason))
        {
            DialogManager.Enqueue(
                new Ui.Components.Dialogs.Dialog(title, reason, new Ui.Components.Dialogs.DialogOption("Ok")));
            return;
        }

        ResetMouseState();
        MapTestSession.Start(MapData.GetMapJsonString(), this);
    }

    private void OnBackClick()
    {
        ResetMouseState();
        ScreenManager.FadeTo(new TitleScreen());
    }

    private void OnMapDimensionsClick()
    {
        if (MapData == null) return;
        _mapDimensionsWindow.Open(MapData.MapWidth, MapData.MapHeight);
        UpdatePositions();
    }

    // Changing the size re-creates the map with the previous tiles centered in it
    private void OnMapDimensionsChange()
    {
        if (MapView == null) return;
        MapData.ChangeDimensions(_mapDimensionsWindow.MapWidth, _mapDimensionsWindow.MapHeight);
        MapView.OnMapLoaded();
        MapView.SetGrid(MapView.GridEnabled);
        _timeControl.CreateHistory(MapView.Id); // Old actions point at the old coordinates
        _mapInfoPanel.SetText($"Map size: {MapData.MapWidth}x{MapData.MapHeight} (click to change)");
        UpdatePositions();
    }

    public void ShowEditNameView(int x, int y, string objName)
    {
        _editNameWindow.Open(x, y, objName);
        UpdatePositions();
    }

    private void OnEditName()
    {
        if (MapView == null) return;
        var x = _editNameWindow.TileX;
        var y = _editNameWindow.TileY;
        var prev = MapView.TileMap.GetTileData(x, y);
        if (prev == null || prev.ObjType == 0) return;

        History?.Record(new MapEditAction(MapView, x, y, prev.ObjCfg, _editNameWindow.ObjName));
        MapView.EditTileObjCfg(x, y, _editNameWindow.ObjName);
    }

#endregion

#region Zoom

    private void OnZoomInputChange(string text)
    {
        if (_zoomInputUpdating || MapView == null || !int.TryParse(text, out var zoom) || zoom <= 0) return;
        zoom = Math.Min(zoom, MaxZoom);
        if (MapView.ZoomLevel == zoom) return;
        MapView.ZoomLevel = zoom;
        UpdateZoomLevel();
    }

    private void UpdateZoomLevel()
    {
        if (MapView == null) return;
        if (!_zoomInput.Focused)
        {
            _zoomInputUpdating = true;
            _zoomInput.SetText(MapView.ZoomLevel.ToString());
            _zoomInputUpdating = false;
        }

        PositionMapView();
    }

#endregion
}