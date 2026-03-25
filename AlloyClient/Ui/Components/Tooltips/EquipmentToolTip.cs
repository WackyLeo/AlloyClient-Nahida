using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Ui.Components.Elements;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Components.Tooltips;

public sealed class EquipmentToolTip : Tooltip {
    
    private const uint BgDefault = 0xFF363636u;
    private const uint BgUnusable = 0xFF5C1D1Du;
    
    private const uint BgOutlineDefault = 0xFF676767u;
    private const uint BgOutlineUnusable = 0xFFA7502Fu;
    private bool _usable = false;
    
    private const uint ColorDefault = 0xB3B3B3;
    private const uint ColorWhite = 0xFFFFFF;
    private const uint ColorRed = 0xFF0000;
    private const uint ColorKhaki = 0xE8E873;
    private const uint ColorGold = 0xFCDF00;
    private uint ColorGreen = 0x00FF00;

    private readonly ItemDesc _itemDesc;
    private int _currentY;

    public EquipmentToolTip(ItemDesc itemDesc) : base(260, 100,
        IsUsableByPlayer(itemDesc) ? BgDefault : BgUnusable, 
        IsUsableByPlayer(itemDesc) ? BgOutlineDefault : BgOutlineUnusable) {
        
        _itemDesc = itemDesc;
        _currentY = 10;
        
        // Like this is just silly
        _usable = IsUsableByPlayer(itemDesc);
        if (!_usable) {
            ColorGreen = ColorKhaki;
        }
        
        AddHeader();
        AddDescription();
        AddLineBreak();
        AddCombatStats();
        AddActivateEffects();
        AddEquipStats();
        AddLineBreak();
        AddRestrictions();
        AddTribute();

        ToolHeight = _currentY + 12;
        DrawSprite();
    }

    private void AddHeader() {
        var obj = _itemDesc.ObjectType;
        var icon = new ObjectRect(new ObjectRectConfig {
            Texture = TextureHelper.FromGameAtlas(obj <= 0 ? (ushort)0x0096 : obj),
            Width = 36,
            Height = 36
        });
        icon.X = 10;
        icon.Y = 10;
        AddChild(icon);
 
        int effectId = TextEffect.None;
        if (_itemDesc.Ferozium) effectId = TextEffect.Void;
 
        var titleConfig = SimpleConfig(
            _itemDesc.DisplayName ?? _itemDesc.ObjectId,
            18, FontType.Bold, ColorWhite, 0x000000, 0f, 150, 0.5f, effectId);
        titleConfig.X = 62;
        var title = new SimpleText(titleConfig);
        
        const int iconCentreY = 28;
        int titleY = title.Height <= 22
            ? iconCentreY - title.Height / 2
            : _currentY;
        title.Y = titleY;
        AddChild(title);
 
        if (!_itemDesc.Consumable && _itemDesc.SlotType != 10) {
            ushort divIcon = 0;
            if (!string.IsNullOrEmpty(_itemDesc.Divinity))
                switch (_itemDesc.Divinity) {
                    case "Azamoth": divIcon = 4568; break;
                    case "Serleon": divIcon = 4569; break;
                    case "Arcanios": divIcon = 4570; break;
                    case "Celeste": divIcon = 4571; break;
                    case "Singigna": divIcon = 4572; break;
                    case "Neracia": divIcon = 4573; break;
                    case "Siera": divIcon = 4574; break;
                    case "Paradox": divIcon = 3058; break;
                }
 
            if (divIcon != 0) {
                var divinityIcon = new ObjectRect(new ObjectRectConfig {
                    Texture = TextureHelper.FromGameAtlas(divIcon),
                    Width = 36,
                    Height = 36,
                    OutlineEnabled = true,
                    GlowEnabled = true
                });
 
                divinityIcon.X = ToolWidth - divinityIcon.Width - 10;
                divinityIcon.Y = 10;
                AddChild(divinityIcon);
            }
 
            var tierTag = new TierText(_itemDesc);
            tierTag.X = ToolWidth - 28 - tierTag.Width / 2;
            tierTag.Y = _currentY + 12;
            AddChild(tierTag);
        }
 
        _currentY = Math.Max(titleY + title.Height, 10 + 44) + 4;
    }

    private void AddDescription() {
        if (string.IsNullOrEmpty(_itemDesc.Description)) return;
        AddTextLine(_itemDesc.Description, ColorDefault, FontType.Bold, 14, 12, null, ToolWidth - 24, 0.0f);
    }

    private void AddLineBreak() {
        _currentY += 2;
        var line = new ColorRect(new ColorRectConfig { Width = ToolWidth - 24, Height = 2, Color = 0xFF111111u });
        line.X = 12;
        line.Y = _currentY;
        AddChild(line);
        _currentY += 8;
    }

    private void AddCombatStats() {
        if (_itemDesc.NumProjectiles > 1)
            AddTextLine("Projectile 1", ColorDefault, FontType.Bold, 14);

        if (_itemDesc.AllProjectiles != null && _itemDesc.AllProjectiles.Length > 0) {
            var proj = _itemDesc.AllProjectiles[0];

            var dmgStr = proj.MinDamage == proj.MaxDamage
                             ? $"{proj.MinDamage}"
                             : $"{proj.MinDamage} - {proj.MaxDamage}";
            AddSplitRow("Damage: ", dmgStr, ColorKhaki);

            var range = proj.Speed * proj.LifetimeMS / 10000f;
            AddSplitRow("Range: ", $"{MathF.Round(range, 1):0.0}", ColorKhaki);

            AddSplitRow("Rate of Fire: ", $"{MathF.Round(_itemDesc.RateOfFire * 100)}%", ColorKhaki);

            if (proj.MultiHit) AddTextLine("Shots hit multiple targets", ColorKhaki, FontType.Bold, 14);
            if (proj.PassesCover) AddTextLine("Shots pass through obstacles", ColorKhaki, FontType.Bold, 14);
            if (proj.ArmorPiercing) AddTextLine("Armor Piercing", ColorKhaki, FontType.Bold, 14);
        }

        if (_itemDesc.MpCost > 0) AddSplitRow("MP Cost: ", $"{_itemDesc.MpCost}", ColorKhaki);
        if (_itemDesc.MpEndCost > 0) AddSplitRow("MP Drain: ", $"{_itemDesc.MpEndCost} / sec", ColorKhaki);
        if (_itemDesc.MpCost > 0) AddSplitRow("Cooldown: ", $"{_itemDesc.Cooldown} Seconds", ColorKhaki);
    }

    private void AddActivateEffects() {
        if (_itemDesc.ActivateEffects == null) return;

        foreach (var act in _itemDesc.ActivateEffects)
            switch (act.Effect) {
                case "Heal": {
                    AddTextLine($"+{act.Amount} HP", ColorGreen, FontType.Bold, 14);
                    break;
                }
                case "Magic": {
                    AddTextLine($"+{act.Amount} MP", ColorGreen, FontType.Bold, 14);
                    break;
                }
                case "HealNova": {
                    AddSplitRow("Party Heal: ", $"+{act.Amount} HP within {act.Radius} sqrs", ColorKhaki);
                    break;
                }
                case "Teleport": {
                    AddTextLine("Teleport to cursor", ColorKhaki, FontType.Bold, 14);
                    break;
                }
                case "StatBoostSelf": {
                    AddSplitRow("Increase ",
                                $"{StatsUtil.FromId(act.Stats)} by {act.Amount} for {act.DurationSec} secs",
                                ColorKhaki);
                    break;
                }
                case "StatBoostAura": {
                    AddSplitRow("Party: ",
                                $"Increase {StatsUtil.FromId(act.Stats)} by {act.Amount} for {act.DurationSec} secs",
                                ColorKhaki);
                    break;
                }
                case "ConditionEffectSelf": {
                    AddTextLine($"{act.ConditionEffect} on self for {act.DurationSec} secs", ColorKhaki,
                                FontType.Bold, 14);
                    break;
                }
                case "ConditionEffectAura": {
                    AddSplitRow("Party: ",
                                $"{act.ConditionEffect} for {act.DurationSec} secs within {act.Radius} sqrs",
                                ColorKhaki);
                    break;
                }
                case "Decoy": {
                    AddTextLine($"Decoy for {act.DurationSec} secs", ColorKhaki, FontType.Bold, 14);
                    break;
                }
            }
    }

    private void AddEquipStats() {
        if (_itemDesc.StatBoosts != null && _itemDesc.StatBoosts.Length > 0) {
            AddTextLine("On Equip:", ColorDefault, FontType.Bold, 14);
            foreach (var stat in _itemDesc.StatBoosts) {
                var prefix = stat.Amount >= 0 ? "+" : "";
                var color = stat.Amount >= 0 ? ColorGreen : ColorRed;
                AddTextLine($"{prefix}{stat.Amount} {StatsUtil.FromId(stat.Stat)}", color, FontType.Bold, 14);
            }
        }

        if (_itemDesc.FameBonus > 0) AddSplitRow("Fame Bonus: ", $"{_itemDesc.FameBonus}%", ColorGreen);
    }

    private void AddRestrictions() {
        if (_itemDesc.Soulbound) AddTextLine("Soulbound", ColorDefault, FontType.Bold, 14);

        if (_itemDesc.SlotType != 0 && _itemDesc.SlotType != 10) {
            AddTextLine("Must be equipped to use", ColorDefault, FontType.Bold, 14);
            AddTextLine("Double-Click to equip", ColorDefault, FontType.Bold, 14);

            var usableClass = string.IsNullOrEmpty(_itemDesc.Class) ? "Any" : _itemDesc.Class;
            AddTextLine($"Usable by: {usableClass}", ColorDefault, FontType.Bold, 14);
        }
    }

    private void AddTribute() {
        if (_itemDesc.TributePoints > 0) {
            _currentY += 4;
            AddSplitRow("Sacrificial Value: ", $"{_itemDesc.TributePoints}", ColorGreen);

            if (!string.IsNullOrEmpty(_itemDesc.Divinity))
                AddTextLine($"({_itemDesc.Divinity})", ColorGreen, FontType.Bold, 14);
        }
    }

    private SimpleText AddTextLine(string text, uint color, FontType font, int fontSize, int xOffset = 12,
                                   int? yOverride = null, int maxWidth = 320, float glow = 0.5f, int effect = 0) {
        var config = SimpleConfig(text, fontSize, font, color, 0x000000, 0f, maxWidth, glow, effect);
        config.X = xOffset;
        config.Y = yOverride ?? _currentY;

        var st = new SimpleText(config);
        AddChild(st);

        if (yOverride == null) _currentY += Math.Max(st.Height, fontSize + 4);
        return st;
    }

    private void AddSplitRow(string label, string value, uint valueColor, bool increaseY = true) {
        var lbl = new SimpleText(SimpleConfig(label, 14, FontType.Bold, ColorDefault, thickness: 0f, glow: 1f));
        lbl.X = 12;
        lbl.Y = _currentY;
        AddChild(lbl);

        var val = new SimpleText(SimpleConfig(value, 14, FontType.Bold, valueColor, thickness: 0f, glow: 1f));
        val.X = 12 + lbl.Width + 2;
        val.Y = _currentY;
        AddChild(val);

        // early exit on the increase Y
        if (!increaseY) return;

        _currentY += Math.Max(lbl.Height, 14 + 4);
    }

    public static TextConfig SimpleConfig(string text = "", int size = 14, FontType type = FontType.Bold,
                                          uint color = 0xFFFFFFFFu, uint outline = 0xFF000000u, float thickness = 1f,
                                          int maxWidth = 320, float glow = 0.5f, int effect = 0) {
        return new TextConfig {
            FontSize = size,
            FontType = type,
            Text = text,
            Color = color,
            OutlineColor = outline,
            OutlineThickness = thickness,
            MaxWidth = maxWidth,
            Glow = glow,
            ShaderEffect = effect
        };
    }
    
    private static bool IsUsableByPlayer(ItemDesc itemDesc) {
        if (Map.LocalPlayer == null || itemDesc == null) return true;
        if (itemDesc.ObjectType == 0) return false;

        var slotType = itemDesc.SlotType;

        if (slotType == ItemConstants.PotionType)
            return true;

        var slots = Map.LocalPlayer.Properties.SlotTypes;
        for (var i = 0; i < slots.Count; i++) {
            if (slots[i] == slotType)
                return true;
        }

        return false;
    }
}