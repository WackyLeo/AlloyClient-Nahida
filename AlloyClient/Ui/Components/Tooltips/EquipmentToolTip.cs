using System;
using System.Linq;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Ui.Components.Elements;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Components.Tooltips;

public sealed class EquipmentToolTip : Tooltip
{
    private const uint BgDefault = 0xFF363636u;
    private const uint BgUnusable = 0xFF5C1D1Du;

    private const uint BgOutlineDefault = 0xFF676767u;
    private const uint BgOutlineUnusable = 0xFFA7502Fu;

    private const uint ColorDefault = 0xB3B3B3;
    private const uint ColorWhite = 0xFFFFFF;
    private const uint ColorRed = 0xFF0000;
    private const uint ColorKhaki = 0xE8E873;
    private const uint ColorGold = 0xFCDF00;

    private readonly ItemDesc _itemDesc;
    private readonly bool _usable;
    private readonly uint ColorGreen = 0x00FF00;

    // 
    private ItemDesc _compareItemDesc;
    private int _currentY;

    public EquipmentToolTip(ItemDesc itemDesc) : base(260, 100,
        IsUsableByPlayer(itemDesc) ? BgDefault : BgUnusable,
        IsUsableByPlayer(itemDesc) ? BgOutlineDefault : BgOutlineUnusable)
    {
        _itemDesc = itemDesc;
        _currentY = 10;

        // Like this is just silly
        _usable = IsUsableByPlayer(itemDesc);
        if (_usable && Map.LocalPlayer != null) SetComparison(itemDesc.SlotType);

        AddHeader();
        AddDescription();
        AddLineBreak();
        AddExtraTooltipData();
        AddCombatStats();
        AddActivateEffects();
        AddEquipStats();
        AddLineBreak();
        AddRestrictions();
        AddTribute();
        ToolHeight = _currentY + 12;
        DrawSprite();
    }

    private float CalcDps(ItemDesc item, int defense)
    {
        if (item?.AllProjectiles == null || item.AllProjectiles.Length == 0) return 0f;
        if (Map.LocalPlayer == null) return 0f;

        var attackFreq = 0.0015f + Map.LocalPlayer.Dexterity / 250f * (0.008f - 0.0015f);
        var shotsPerSec = 1000f * attackFreq * item.RateOfFire / 1.02f;

        var avgRawDmg = 0f;
        foreach (var proj in item.AllProjectiles.Where(e => !e.OmitText))
        {
            avgRawDmg += ((proj.MinDamage + proj.MaxDamage) / 2f) * proj.HitAmount;

            // Check for split shots and add to avgRawDmg
            if (proj.SplitShots != null && proj.SplitShots.Count > 0)
            {
                var splitId = ObjectLibrary.IdToObjectType[proj.SplitShots.Id];
                var splitType = ObjectLibrary.TypeToObjectProps[splitId];

                // TODO: Update this so it's not hardcoded to Projectile index 0
                avgRawDmg +=
                    ((splitType.Projectiles[0].MinDamage + splitType.Projectiles[0].MaxDamage) / 2f *
                     proj.SplitShots.Count) * proj.HitAmount;
            }
        }

        avgRawDmg /= item.AllProjectiles.Count(e => !e.OmitText);

        var dmgMult = 0.5f + (Map.LocalPlayer.Attack / 250f * 1.5f);
        var rawPerShot = avgRawDmg * dmgMult;

        var armorPiercing = item.AllProjectiles[0].ArmorPiercing;
        var actualDef = armorPiercing ? 0 : defense;
        var limit = rawPerShot * 0.15f;
        var dmgAfterDef = rawPerShot - actualDef < limit ? limit : rawPerShot - actualDef;

        return dmgAfterDef * item.NumProjectiles * shotsPerSec;
    }

    private void SetComparison(int slotType)
    {
        var curSlotIndex = 0;
        for (curSlotIndex = 0; curSlotIndex < 4; curSlotIndex++)
        {
            if (curSlotIndex == 0 && slotType == 0)
                break;
            if (Map.LocalPlayer.Properties.SlotTypes[curSlotIndex] == slotType)
                break;
        }


        if (Map.LocalPlayer.Equipment[curSlotIndex] != null) _compareItemDesc = Map.LocalPlayer.Equipment[curSlotIndex];
    }

    private void AddHeader()
    {
        var obj = _itemDesc.ObjectType;
        var icon = new ObjectRect(new ObjectRectConfig
        {
            Texture = TextureHelper.FromGameAtlas(obj <= 0 ? (ushort)0x0096 : obj),
            Width = 36,
            Height = 36
        });
        icon.X = 10;
        icon.Y = 10;
        AddChild(icon);

        var effectId = TextEffect.None;
        if (_itemDesc.Ferozium) effectId = TextEffect.Void;

        var titleConfig = SimpleConfig(
            _itemDesc.DisplayName ?? _itemDesc.ObjectId,
            18, FontType.Bold, ColorWhite, 0x000000, 0f, 150, 0.5f, effectId);
        titleConfig.X = 62;
        var title = new SimpleText(titleConfig);

        const int iconCentreY = 28;
        var titleY = title.Height <= 22
            ? iconCentreY - title.Height / 2
            : _currentY;
        title.Y = titleY;
        AddChild(title);

        if (!_itemDesc.Consumable && _itemDesc.SlotType != 10)
        {
            var tierTag = new TierText(_itemDesc);
            tierTag.X = ToolWidth - 28 - tierTag.Width / 2;
            tierTag.Y = _currentY + 12;
            AddChild(tierTag);
        }

        _currentY = Math.Max(titleY + title.Height, 10 + 44) + 4;
    }

    private void AddDescription()
    {
        if (string.IsNullOrEmpty(_itemDesc.Description)) return;
        AddTextLine(_itemDesc.Description, ColorDefault, FontType.Bold, 14, 12, null, ToolWidth - 24, 0.0f);
    }

    private void AddLineBreak()
    {
        _currentY += 2;

        var line = new DiamondDivider(new DiamondDividerConfig
        {
            LineLength = ToolWidth - 36,
            LineThickness = 2,
            DiamondSize = 4,
            Color = 0xFFAAAAAAu
        });

        line.X = 12;
        line.Y = _currentY;
        AddChild(line);

        _currentY += 12;
    }

    private void AddCombatStats()
    {
        if (_itemDesc.Doses > 0) AddSplitRow("Doses: ", $"{_itemDesc.Doses}", ColorKhaki);
        if (_itemDesc.Quantity > 0) AddSplitRow("Quantity: ", $"{_itemDesc.Quantity}", ColorKhaki);

        if (_itemDesc.NumProjectiles > 1)
        {
            AddTextLine($"Projectiles: {_itemDesc.NumProjectiles}", ColorDefault, FontType.Bold, 14);

            // something here

            var i = 0;
        }

        if (_itemDesc.AllProjectiles != null && _itemDesc.AllProjectiles.Length > 0)
        {
            var proj = _itemDesc.AllProjectiles[0];

            var dmgClr = _usable ? ColorGreen : ColorKhaki;
            var curDmg = (proj.MinDamage + proj.MaxDamage) / 2;

            if (_usable && _compareItemDesc != null && _compareItemDesc.AllProjectiles.Length > 0)
            {
                var comparProj = _compareItemDesc.AllProjectiles[0];
                var compDmg = (comparProj.MinDamage + comparProj.MaxDamage) / 2;

                if (curDmg > compDmg)
                    dmgClr = ColorGreen;
                else if (Math.Abs(curDmg - compDmg) < 0.001)
                    dmgClr = ColorKhaki;
                else if (curDmg < compDmg)
                    dmgClr = ColorRed;
            }

            var dmgStr = proj.MinDamage == proj.MaxDamage
                ? $"{proj.MinDamage}"
                : $"{proj.MinDamage} - {proj.MaxDamage}";
            AddSplitRow("Damage: ", dmgStr, dmgClr);

            var rangeClr = _usable ? ColorGreen : ColorKhaki;
            var range = proj.Speed * proj.LifetimeMS / 10000f;

            if (_usable && _compareItemDesc != null && _compareItemDesc.AllProjectiles.Length > 0)
            {
                var comparProj = _compareItemDesc.AllProjectiles[0];
                var compRange = comparProj.Speed * comparProj.LifetimeMS / 10000f;

                if (range > compRange)
                    rangeClr = ColorGreen;
                else if (Math.Abs(range - compRange) < 0.001)
                    rangeClr = ColorKhaki;
                else if (range < compRange)
                    rangeClr = ColorRed;
            }

            AddSplitRow("Range: ", $"{MathF.Round(range, 1):0.0}", rangeClr);

            var rateClr = _usable ? ColorGreen : ColorKhaki;
            if (_usable && _compareItemDesc != null)
            {
                if (_itemDesc.RateOfFire > _compareItemDesc.RateOfFire)
                    rateClr = ColorGreen;
                else if (Math.Abs(_itemDesc.RateOfFire - _compareItemDesc.RateOfFire) < 0.001)
                    rateClr = ColorKhaki;
                else if (_itemDesc.RateOfFire < _compareItemDesc.RateOfFire)
                    rateClr = ColorRed;
            }

            AddSplitRow("Rate of Fire: ", $"{MathF.Round(_itemDesc.RateOfFire * 100)}%", rateClr);

            if (proj.MultiHit) AddTextLine("Shots hit multiple targets", ColorKhaki, FontType.Bold, 14);
            if (proj.PassesCover) AddTextLine("Shots pass through obstacles", ColorKhaki, FontType.Bold, 14);
            if (proj.ArmorPiercing) AddTextLine("Armor Piercing", ColorKhaki, FontType.Bold, 14);

            var dps0 = CalcDps(_itemDesc, 0);
            var dps75 = CalcDps(_itemDesc, 75);
            if (dps0 > 0)
            {
                var dpsGreenClr = ColorKhaki;
                var dpsPurpleClr = 0xCC88FFu;
                if (_usable && _compareItemDesc != null)
                {
                    var compDps0 = CalcDps(_compareItemDesc, 0);
                    dpsGreenClr = dps0 > compDps0 ? ColorGreen : dps0 < compDps0 ? ColorRed : ColorKhaki;
                }

                var lbl = new SimpleText(SimpleConfig("DPS: ", 14, FontType.Bold, ColorDefault, thickness: 0f,
                    glow: 0.5f));
                lbl.X = 12;
                lbl.Y = _currentY;
                AddChild(lbl);

                var x = 12 + lbl.Width + 2;
                var green = new SimpleText(SimpleConfig($"{MathF.Round(dps0)}", 14, FontType.Bold, dpsGreenClr,
                    thickness: 0f, glow: 1f));
                green.X = x;
                green.Y = _currentY;
                AddChild(green);

                x += green.Width + 4;
                var purple = new SimpleText(SimpleConfig($"({MathF.Round(dps75)})", 14, FontType.Bold, dpsPurpleClr,
                    thickness: 0f, glow: 1f));
                purple.X = x;
                purple.Y = _currentY;
                AddChild(purple);
                _currentY += Math.Max(lbl.Height, green.Height) + 4;
            }

            // Ported: Projectile Condition Effects
            if (proj.Effects != null)
                foreach (var cond in proj.Effects)
                    AddTextLine($"Shot Effect: {cond.EffectName} for {cond.DurationMS / 1000f} secs", ColorKhaki,
                        FontType.Bold, 14);
        }

        if (_itemDesc.MpCost > 0) AddSplitRow("MP Cost: ", $"{_itemDesc.MpCost}", ColorKhaki);
        if (_itemDesc.MpEndCost > 0) AddSplitRow("MP Drain: ", $"{_itemDesc.MpEndCost} / sec", ColorKhaki);
        if (_itemDesc.MpCost > 0) AddSplitRow("Cooldown: ", $"{_itemDesc.Cooldown} Seconds", ColorKhaki);
    }

    private void AddActivateEffects()
    {
        if (_itemDesc.ActivateEffects == null) return;

        foreach (var act in _itemDesc.ActivateEffects)
            switch (act.Effect)
            {
                case "Heal":
                    AddTextLine($"+{act.Amount} HP", ColorGreen, FontType.Bold, 14);
                    break;
                case "Magic":
                    AddTextLine($"+{act.Amount} MP", ColorGreen, FontType.Bold, 14);
                    break;
                case "HealNova":
                    AddSplitRow("Party Heal: ", $"+{act.Amount} HP within {act.Radius} sqrs", ColorKhaki);
                    break;
                case "MagicNova":
                    AddTextLine($"Fill Party Magic: {act.Amount} MP at {act.Radius} sqrs", ColorKhaki, FontType.Bold,
                        14);
                    break;
                case "Teleport":
                    AddTextLine("Teleport to cursor", ColorKhaki, FontType.Bold, 14);
                    break;
                case "StatBoostSelf":
                    AddSplitRow("Increase ",
                        $"{StatsUtil.FromId(act.Stats)} by {act.Amount} for {act.DurationSec} secs", ColorKhaki);
                    break;
                case "StatBoostAura":
                    AddSplitRow("Party: ",
                        $"Increase {StatsUtil.FromId(act.Stats)} by {act.Amount} for {act.DurationSec} secs",
                        ColorKhaki);
                    break;
                case "ConditionEffectSelf":
                    AddTextLine($"{act.ConditionEffect} on self for {act.DurationSec} secs", ColorKhaki, FontType.Bold,
                        14);
                    break;
                case "ConditionEffectAura":
                    AddSplitRow("Party: ", $"{act.ConditionEffect} for {act.DurationSec} secs within {act.Radius} sqrs",
                        ColorKhaki);
                    break;
                case "Decoy":
                    AddTextLine($"Decoy for {act.DurationSec} secs", ColorKhaki, FontType.Bold, 14);
                    break;
                case "VampireBlast":
                    AddSplitRow("Steal: ", $"{act.TotalDamage} HP within {act.Radius} sqrs", ColorKhaki);
                    break;
                case "Trap":
                    AddSplitRow("Trap: ", $"{act.TotalDamage} HP within {act.Radius} sqrs", ColorKhaki);
                    AddTextLine($"{act.ConditionEffect} for {act.DurationMS / 1000f} secs", ColorKhaki, FontType.Bold,
                        14);
                    break;
                case "StasisBlast":
                    AddTextLine($"Stasis group for {act.DurationSec} secs", ColorKhaki, FontType.Bold, 14);
                    break;
                case "Lightning":
                    AddTextLine($"Lightning: {act.TotalDamage} damage to {act.MaxTargets} targets", ColorKhaki,
                        FontType.Bold, 14);
                    break;
                case "LightningPlayer":
                    AddSplitRow("Player Lightning: ", $"{act.Amount} to {act.MaxTargets} players", ColorKhaki);
                    break;
                case "PoisonGrenade":
                    AddTextLine("Poison Grenade", ColorKhaki, FontType.Bold, 14);
                    AddTextLine($"{act.TotalDamage} damage over {act.DurationSec} secs within {act.Radius} sqrs",
                        ColorKhaki, FontType.Bold, 14);
                    break;
                case "RemoveNegCond":
                case "RemoveNegCondSelf":
                    AddTextLine("Removes negative conditions", ColorKhaki, FontType.Bold, 14);
                    break;
                case "GenericActivate":
                    AddSplitRow("Activate",
                        $"Within {act.Radius} sqrs {act.ConditionEffect} for {act.DurationSec} seconds", ColorKhaki);
                    break;
            }
    }

    private void AddExtraTooltipData()
    {
        if (_itemDesc.ExtraTooltipData == null || _itemDesc.ExtraTooltipData.Length == 0) return;

        foreach (var extraData in _itemDesc.ExtraTooltipData)
        {
            var desc = extraData.Description?.Replace("\\n", "\n").Trim();
            if (!string.IsNullOrEmpty(extraData.Name) && !string.IsNullOrEmpty(desc))
            {
                if (desc.Length <= 24 && !desc.Contains('\n'))
                {
                    AddSplitRow($"{extraData.Name}: ", desc, ColorKhaki);
                }
                else
                {
                    AddTextLine($"{extraData.Name}:", ColorDefault, FontType.Bold, 14, maxWidth: ToolWidth - 24);
                    AddTextLine(desc, ColorKhaki, FontType.Bold, 14, maxWidth: ToolWidth - 24);
                }
            }
            else if (!string.IsNullOrEmpty(desc))
            {
                AddTextLine(desc, ColorKhaki, FontType.Bold, 14, maxWidth: ToolWidth - 24);
            }
            else if (!string.IsNullOrEmpty(extraData.Name))
            {
                AddTextLine(extraData.Name, ColorDefault, FontType.Bold, 14, maxWidth: ToolWidth - 24);
            }
        }
    }

    private void AddEquipStats()
    {
        if (_itemDesc.StatBoosts != null && _itemDesc.StatBoosts.Length > 0)
        {
            AddTextLine("On Equip:", ColorDefault, FontType.Bold, 14);
            for (var i = 0; i < _itemDesc.StatBoosts.Length; i++)
            {
                var stat = _itemDesc.StatBoosts[i];
                var prefix = stat.Amount >= 0 ? "+" : "";
                var color = _usable ? stat.Amount >= 0 ? ColorGreen : ColorRed : ColorKhaki;
                if (_usable && _compareItemDesc != null)
                {
                    var amount = -1;
                    foreach (var stat2 in _compareItemDesc.StatBoosts)
                        if (stat2.Stat == stat.Stat)
                            amount = stat2.Amount;

                    // Here
                    if (stat.Amount > amount)
                        color = ColorGreen;
                    else if (stat.Amount == amount)
                        color = ColorKhaki;
                    else if (stat.Amount < amount)
                        color = ColorRed;
                }

                AddTextLine($"{prefix}{stat.Amount} {StatsUtil.FromId(stat.Stat)}", color, FontType.Bold, 14);
            }
        }

        if (_itemDesc.FameBonus > 0)
        {
            var color = _usable ? ColorGreen : ColorKhaki;

            if (_usable && _compareItemDesc != null)
                color = _compareItemDesc.FameBonus >= _itemDesc.FameBonus ? ColorKhaki : ColorGreen;

            AddSplitRow("Fame Bonus: ", $"{_itemDesc.FameBonus}%", color);
        }
    }

    private void AddRestrictions()
    {
        if (_itemDesc.Soulbound) AddTextLine("Soulbound", ColorDefault, FontType.Bold, 14);

        if (!string.IsNullOrEmpty(_itemDesc.SetName))
            AddTextLine($"This item is a part of {_itemDesc.SetName}", 0xFF9900, FontType.Bold, 14);

        if (_itemDesc.SlotType != 0 && _itemDesc.SlotType != 10)
        {
            var usableClass = string.IsNullOrEmpty(_itemDesc.Class) ? "Any" : _itemDesc.Class;
            AddTextLine($"Usable by: {usableClass}", ColorDefault, FontType.Bold, 14);
        }

        /*if (_itemDesc.EquipRequirements != null) {
            foreach (var req in _itemDesc.EquipRequirements) {
                bool meetsReq = true;
                var color = meetsReq ? ColorDefault : ColorRed;
                AddTextLine($"Requires {StatsUtil.FromId(req.Stat)} of {req.Value}", color, FontType.Bold, 14);
            }
        }*/
    }

    private void AddTribute()
    {
        if (_itemDesc.TributePoints > 0)
        {
            _currentY += 4;

            AddTextLine($"Sacrificial Value: {_itemDesc.TributePoints}", 0xFFFFFF, FontType.Bold, 14);

        }
    }

    private SimpleText AddTextLine(string text, uint color, FontType font, int fontSize, int xOffset = 12,
        int? yOverride = null, int maxWidth = 320, float glow = 0.5f, int effect = 0)
    {
        var config = SimpleConfig(text, fontSize, font, color, 0x000000, 0f, maxWidth, glow, effect);
        config.X = xOffset;
        config.Y = yOverride ?? _currentY;

        var st = new SimpleText(config);
        AddChild(st);

        if (yOverride == null) _currentY += Math.Max(st.Height, fontSize + 4);
        return st;
    }

    private void AddSplitRow(string label, string value, uint valueColor, bool increaseY = true)
    {
        var lbl = new SimpleText(SimpleConfig(label, 14, FontType.Bold, ColorDefault, thickness: 0f, glow: 0.5f));
        lbl.X = 12;
        lbl.Y = _currentY;
        AddChild(lbl);

        var valX = 12 + lbl.Width + 2;
        var maxValWidth = ToolWidth - valX - 12;

        var val = new SimpleText(SimpleConfig(value, 14, FontType.Bold, valueColor, thickness: 0f,
            maxWidth: maxValWidth, glow: 1f));
        val.X = valX;
        val.Y = _currentY;
        AddChild(val);

        if (!increaseY) return;
        _currentY += Math.Max(lbl.Height, val.Height) + 4;
    }

    public static TextConfig SimpleConfig(string text = "", int size = 14, FontType type = FontType.Bold,
        uint color = 0xFFFFFFFFu, uint outline = 0xFF000000u, float thickness = 1f,
        int maxWidth = 320, float glow = 0.5f, int effect = 0)
    {
        return new TextConfig
        {
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

    private static bool IsUsableByPlayer(ItemDesc itemDesc)
    {
        if (Map.LocalPlayer == null || itemDesc == null) return true;
        if (itemDesc.ObjectType == 0) return false;

        var slotType = itemDesc.SlotType;

        if (slotType == ItemConstants.PotionType)
            return true;

        var slots = Map.LocalPlayer.Properties.SlotTypes;
        for (var i = 0; i < slots.Count; i++)
            if (slots[i] == slotType)
                return true;

        return false;
    }
}