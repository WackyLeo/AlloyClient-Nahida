using System;
using System.Text.Json.Nodes;
using AlloyClient.Assets.Libraries;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Wiki;

public class EnemyListEntry : Container {
    private const int EntryHeight = 36;
    private const int Padding = 6;
    private const int IconSize = 24;

    private const uint ColorEven = 0xFF2E2E2Eu;
    private const uint ColorOdd = 0xFF262626u;
    private const uint ColorHover = 0xFF3E3E3Eu;
    private const uint ColorSelected = 0xFF4A6A9Au;

    private readonly SdfRect _background;
    private readonly Action<EnemyListEntry> _onSelected;

    public readonly JsonNode Data;
    public readonly string DisplayId;
    public readonly string EnemyKey;
    public readonly ushort EnemyObjectType;

    private bool _isEven;
    private bool _isSelected;

    public EnemyListEntry(string displayId, JsonNode data, int width, Action<EnemyListEntry> onSelected) {
        DisplayId = displayId;
        Data = data;
        _onSelected = onSelected;
        var enemyJsonKey = data?["Enemy"]?.GetValue<string>();
        EnemyObjectType = ResolveObjectType(enemyJsonKey, displayId, out var resolvedKey);
        EnemyKey = resolvedKey;

        _background = new SdfRect(new SdfRectConfig {
            Width = width,
            Height = EntryHeight,
            Cut = 3,
            Color = ColorEven,
            OutlineSize = 0f,
            ShadowRadius = 0f
        });
        _background.MouseEnabled = true;
        AddChild(_background);

        if (EnemyObjectType != 0) {
            var icon = new ObjectRect(new ObjectRectConfig {
                Texture = WikiTextureHelper.ResolveStaticFrame(EnemyObjectType),
                Width = IconSize,
                Height = IconSize,
                OutlineEnabled = false,
                GlowEnabled = false
            });
            icon.X = Padding;
            icon.Y = (EntryHeight - IconSize) / 2;
            _background.AddChild(icon);
        }

        var nameText = new SimpleText(new TextConfig {
            FontSize = 10,
            Color = 0xFFFFFFFFu,
            FontType = FontType.Bold,
            OutlineThickness = 0.0f,
            Glow = 1.0f,
            Anchor = UiAnchor.LeftTop
        });
        nameText.SetText(displayId);
        nameText.X = Padding + IconSize + Padding;
        nameText.Y = (EntryHeight - 12) / 2;
        _background.AddChild(nameText);

        _background.AddEventListener(MouseEvent.MouseOver, OnHoverEnter);
        _background.AddEventListener(MouseEvent.MouseOut, OnHoverExit);
        _background.AddEventListener(MouseEvent.LeftClick, OnClick);
    }

    private static ushort ResolveObjectType(string enemyJsonKey, string displayId, out string resolvedKey) {
        if (!string.IsNullOrEmpty(enemyJsonKey) &&
            ObjectLibrary.IdToObjectType.TryGetValue(enemyJsonKey, out var type1)) {
            resolvedKey = enemyJsonKey;
            return type1;
        }

        if (!string.IsNullOrEmpty(displayId) &&
            ObjectLibrary.IdToObjectType.TryGetValue(displayId, out var type2)) {
            resolvedKey = displayId;
            return type2;
        }

        resolvedKey = enemyJsonKey ?? displayId ?? string.Empty;
        return 0;
    }

    public void SetAlternate(bool even) {
        _isEven = even;
        if (!_isSelected)
            _background.SetColor(even ? ColorEven : ColorOdd);
    }

    public void SetSelected(bool selected) {
        _isSelected = selected;
        _background.SetColor(selected ? ColorSelected : _isEven ? ColorEven : ColorOdd);
    }

    private void OnHoverEnter() {
        if (!_isSelected) _background.SetColor(ColorHover);
    }

    private void OnHoverExit() {
        if (!_isSelected) _background.SetColor(_isEven ? ColorEven : ColorOdd);
    }

    private void OnClick() {
        _onSelected?.Invoke(this);
    }
}