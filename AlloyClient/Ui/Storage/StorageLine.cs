using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Display;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Storage;

public class StorageLine : Container {
    private const int IconSize = 34;
    private const int BtnH = 26;
    private const int PullW = 58;
    private const int MaxW = 48;
    private const int CombineW = 72;
    private const int BtnGap = 5;
    private const int BtnPadR = 10;

    private const uint ColBtnActive = 0xFF3A7A3Au;
    private const uint ColBtnActiveOut = 0xFF66BB66u;
    private const uint ColBtnHover = 0xFF4A9A4Au;
    private const uint ColBtnDisabled = 0xFF2A2A2Au;
    private const uint ColBtnDisabledOut = 0xFF444444u;
    private readonly SimpleText _capacityText;
    private readonly SdfRect _combineBtn;

    private readonly StorageItem _item;
    private readonly SdfRect _maxBtn;
    private readonly SdfRect _pullBtn;
    private readonly int _rowH;
    private readonly int _rowW;

    public StorageLine(
        StorageItem item, int rowW, int rowH, bool isEven,
        Action<ushort> onPull, Action<ushort> onMax, Action<ushort> onCombine) {
        _item = item;
        _rowW = rowW;
        _rowH = rowH;

        var bg = new SdfRect(new SdfRectConfig {
            Width = rowW - 6,
            Height = rowH - 6,
            Cut = 4,
            Color = isEven ? 0xFF2A2A2Au : 0xFF202020u,
            OutlineColor = isEven ? 0xFF3A3A3Au : 0xFF2E2E2Eu,
            OutlineSize = 2f,
            ShadowRadius = 0f
        });
        bg.X = 3;
        bg.Y = 3;
        AddChild(bg);

        if (ObjectLibrary.TypeToTextureData.ContainsKey(item.Id)) {
            var iconHitArea = new SdfRect(new SdfRectConfig {
                Width = IconSize,
                Height = IconSize,
                Cut = 3,
                Color = 0x00000000u,
                OutlineSize = 0f,
                ShadowRadius = 0f,
                Alpha = 0,
                ShadowAlpha = 0
            });
            iconHitArea.X = (rowH - IconSize) / 2;
            iconHitArea.Y = (rowH - IconSize) / 2;
            iconHitArea.MouseEnabled = true;
            AddChild(iconHitArea);

            var icon = new ObjectRect(new ObjectRectConfig {
                Texture = TextureHelper.FromGameAtlas(item.Id),
                Width = IconSize,
                Height = IconSize,
                OutlineEnabled = false,
                GlowEnabled = false
            });
            icon.X = iconHitArea.X;
            icon.Y = iconHitArea.Y;
            AddChild(icon);

            EquipmentToolTip tooltip = null;
            iconHitArea.AddEventListener(MouseEvent.MouseOver, () => {
                if (!ObjectLibrary.TypeToItem.TryGetValue(item.Id, out var desc)) return;
                tooltip = new EquipmentToolTip(desc);
                TooltipManager.AddTooltip(tooltip);
            });
            iconHitArea.AddEventListener(MouseEvent.MouseOut, () => {
                if (tooltip == null) return;
                TooltipManager.RemoveTooltip(tooltip);
                tooltip = null;
            });
        }

        var nameText = new SimpleText(new TextConfig {
            FontSize = 11, Color = 0xFFEEEEEEu, FontType = FontType.Bold, Anchor = UiAnchor.LeftTop
        });
        nameText.SetText(GetDisplayName(item.Id));
        nameText.X = rowH + 4;
        nameText.Y = (rowH - 13) / 2;
        AddChild(nameText);

        _capacityText = new SimpleText(new TextConfig {
            FontSize = 11, Color = 0xFF666666u, FontType = FontType.Bold, Anchor = UiAnchor.LeftTop
        });
        _capacityText.X = rowW / 2 - 24;
        _capacityText.Y = (rowH - 13) / 2;
        AddChild(_capacityText);

        var btnY = (rowH - BtnH) / 2;
        var rightEdge = rowW - BtnPadR;

        _pullBtn = MakeButton("Pull", PullW);
        rightEdge -= PullW;
        _pullBtn.X = rightEdge;
        _pullBtn.Y = btnY;
        AddChild(_pullBtn);
        _pullBtn.AddEventListener(MouseEvent.LeftClick, () => onPull(item.Id));
        rightEdge -= BtnGap;

        if (item.IsPotion) {
            _maxBtn = MakeButton("Max", MaxW);
            rightEdge -= MaxW;
            _maxBtn.X = rightEdge;
            _maxBtn.Y = btnY;
            AddChild(_maxBtn);
            _maxBtn.AddEventListener(MouseEvent.LeftClick, () => onMax(item.Id));
            rightEdge -= BtnGap;

            SimpleTooltip maxTip = null;
            _maxBtn.AddEventListener(MouseEvent.MouseOver, () => {
                maxTip = new SimpleTooltip("Pull enough to fill your inventory to the stack cap");
                TooltipManager.AddTooltip(maxTip);
            });
            _maxBtn.AddEventListener(MouseEvent.MouseOut, () => {
                if (maxTip == null) return;
                TooltipManager.RemoveTooltip(maxTip);
                maxTip = null;
            });
        }

        if (item.IsToken) {
            _combineBtn = MakeButton("Combine", CombineW);
            rightEdge -= CombineW;
            _combineBtn.X = rightEdge;
            _combineBtn.Y = btnY;
            AddChild(_combineBtn);
            _combineBtn.AddEventListener(MouseEvent.LeftClick, () => onCombine(item.Id));

            SimpleTooltip combineTip = null;
            _combineBtn.AddEventListener(MouseEvent.MouseOver, () => {
                combineTip = new SimpleTooltip("Combine 100 tokens into a single item");
                TooltipManager.AddTooltip(combineTip);
            });
            _combineBtn.AddEventListener(MouseEvent.MouseOut, () => {
                if (combineTip == null) return;
                TooltipManager.RemoveTooltip(combineTip);
                combineTip = null;
            });
        }

        Refresh();
    }

    public void Refresh() {
        var capacity = _item.IsToken ? 100 : _item.Capacity;
        var status = StorageStatus.Get(_item.Count, capacity);

        _capacityText.SetText(status.Text);
        _capacityText.SetColor(status.Color);
        _capacityText.X = _rowW / 2 - 24;

        SetBtnEnabled(_pullBtn, _item.Count >= 1);
        SetBtnEnabled(_maxBtn, _item.Count >= 1);
        SetBtnEnabled(_combineBtn, _item.Count >= 100);
    }

    private static void SetBtnEnabled(SdfRect btn, bool enabled) {
        if (btn == null) return;
        btn.MouseEnabled = enabled;
        btn.SetColor(enabled ? ColBtnActive : ColBtnDisabled);
        btn.SetColorSecondary(enabled ? ColBtnActiveOut : ColBtnDisabledOut);
    }

    private static string GetDisplayName(ushort id) {
        if (!ObjectLibrary.TypeToObjectProps.TryGetValue(id, out var props))
            return $"0x{id:X4}";
        return props.DisplayName
            .Replace("Mark of the ", "")
            .Replace("Mark of ", "");
    }

    private static SdfRect MakeButton(string label, int w) {
        var btn = new SdfRect(new SdfRectConfig {
            Width = w,
            Height = BtnH,
            Cut = 4,
            Color = ColBtnActive,
            OutlineColor = ColBtnActiveOut,
            OutlineSize = 1.5f,
            ShadowRadius = 0f
        });
        btn.MouseEnabled = true;

        btn.AddEventListener(MouseEvent.MouseOver, () => btn.SetColor(ColBtnHover));
        btn.AddEventListener(MouseEvent.MouseOut, () => btn.SetColor(ColBtnActive));

        var text = new SimpleText(new TextConfig {
            FontSize = 10, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        text.SetText(label);
        text.X = w / 2;
        text.Y = BtnH / 2;
        btn.AddChild(text);

        return btn;
    }
}