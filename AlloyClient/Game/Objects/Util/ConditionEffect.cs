using System;
using System.Collections.Generic;
using System.Linq;
using AlloyClient.Utils;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects.Util;

public sealed class ConditionEffect(string name, ConditionEffects bit, int[] iconOffsets) {
    public readonly string Name = name;
    public readonly ConditionEffects Bit = bit;
    public readonly int[] IconOffsets = iconOffsets;
}

public static class ConditionEffectUtil {

    public static readonly ConditionEffect[] Effects = [
        new("Nothing",          ConditionEffects.None,              null),
        new("Dead",             ConditionEffects.Dead,              null),
        new("Quiet",            ConditionEffects.Quiet,             [32]),
        new("Weak",             ConditionEffects.Weak,              [34, 35, 36, 37]),
        new("Slowed",           ConditionEffects.Slowed,            [1]),
        new("Sick",             ConditionEffects.Sick,              [39]),
        new("Dazed",            ConditionEffects.Dazed,             [44]),
        new("Stunned",          ConditionEffects.Stunned,           [45]),
        new("Blind",            ConditionEffects.Blind,             [41]),
        new("Hallucinating",    ConditionEffects.Hallucinating,     [42]),
        new("Drunk",            ConditionEffects.Drunk,             [43]),
        new("Unstable Immune",  ConditionEffects.UnstableImmune,    null),
        new("Stun Immune",      ConditionEffects.StunImmune,        null),
        new("Invisible",        ConditionEffects.Invisible,         null),
        new("Paralyzed",        ConditionEffects.Paralyzed,         [53, 54]),
        new("Speedy",           ConditionEffects.Speedy,            [0]),
        new("Bleeding",         ConditionEffects.Bleeding,          [46]),
        new("Armor Broken Immune", ConditionEffects.ArmorBrokenImmune, null),
        new("Healing",          ConditionEffects.Healing,           [47]),
        new("Damaging",         ConditionEffects.Damaging,          [49]),
        new("Berserk",          ConditionEffects.Berserk,           [50]),
        new("Paused",           ConditionEffects.Paused,            null),
        new("Stasis",           ConditionEffects.Stasis,            null),
        new("Stasis Immune",    ConditionEffects.StasisImmune,      null),
        new("Invincible",       ConditionEffects.Invincible,        null),
        new("Invulnerable",     ConditionEffects.Invulnerable,      [17]),
        new("Armored",          ConditionEffects.Armored,           [16]),
        new("Armor Broken",     ConditionEffects.ArmorBroken,       [55]),
        new("Hexed",            ConditionEffects.Hexed,             [42]),
        new("Ninja Speedy",     ConditionEffects.NinjaSpeedy,       [0]),
        new("Unstable",         ConditionEffects.Unstable,          [56]),
        new("Darkness",         ConditionEffects.Darkness,          [57]),
        
        new("Slowed Immune", ConditionEffects.SlowedImmune,      null),
        new("Dazed Immune",     ConditionEffects.DazedImmune,       null),
        new("Paralyzed Immune", ConditionEffects.ParalyzedImmune,   null),
        new("Petrify",          ConditionEffects.Petrify,           null),
        new("Petrify Immune",   ConditionEffects.PetrifiedImmune,   null),
        new("Pet Disable",      ConditionEffects.PetEffectIcon,     [27]),
        new("Curse",            ConditionEffects.Curse,             [58]),
        new("Curse Immune",     ConditionEffects.CurseImmune,       null),
        new("HP Boost",         ConditionEffects.HpBoost,           [32]),
        new("MP Boost",         ConditionEffects.MpBoost,           [33]),
        new("Att Boost",        ConditionEffects.AttBoost,          [34]),
        new("Def Boost",        ConditionEffects.DefBoost,          [35]),
        new("Spd Boost",        ConditionEffects.SpdBoost,          [36]),
        new("Dex Boost",        ConditionEffects.DexBoost,          [37]),
        new("Vit Boost",        ConditionEffects.VitBoost,          [38]),
        new("Wis Boost",        ConditionEffects.WisBoost,          [39]),
        new("Hidden",           ConditionEffects.Hidden,            null),
        new("Muted",            ConditionEffects.Muted,             [21]),
        new("Party Vision",     ConditionEffects.PartyVision,       null),
        new("XMas Vision",      ConditionEffects.XmasVision,        null),
        new("Blessed",          ConditionEffects.Blessed,           [59]),
        new("Exposed",          ConditionEffects.Exposed,           [60]),
        new("Spooky Vision",    ConditionEffects.SpookyVision,      null),
        new("Bunny Vision",     ConditionEffects.BunnyVision,       null),
        new("Silenced",         ConditionEffects.Silenced,          [33]),
        new("Energized",        ConditionEffects.Energized,         [61]),
        new("Quiet Immune",     ConditionEffects.QuietImmune,       null),
        new("Silenced Immune",  ConditionEffects.SilencedImmune,    null),
        new("Sick Immune",      ConditionEffects.SickImmune,        null),
        new("Bleeding Immune",  ConditionEffects.BleedingImmune,    null),
        new("Confused Immune",  ConditionEffects.ConfusedImmune,    null),
        new("Protected",        ConditionEffects.Protected,         [18]),
    ];

    public static readonly ConditionEffects IconlessEffects;
    public static readonly Dictionary<ConditionEffects, Vector4[]> EffectIcons = [];
    private static readonly Dictionary<string, ConditionEffects> NameToEffect = [];

    static ConditionEffectUtil() {
        foreach (var effect in Effects) {
            if (effect.IconOffsets != null)
                EffectIcons[effect.Bit] = effect.IconOffsets
                    .Select(i => Main.Atlas.GetAtlasData("lofiInterface2", i).ToVector4())
                    .ToArray();
            else
                IconlessEffects |= effect.Bit;

            NameToEffect[effect.Name] = effect.Bit;
        }
    }

    public static int GetConditionEffectId(string name) {
        if (NameToEffect.TryGetValue(name, out var eff))
            return (int)eff;
        Logger.Warn($"[ConditionEffect] Unable to find effect: {name}");
        return (int)ConditionEffects.None;
    }

    public static bool IsNegativeCondition(ConditionEffectIndex effect) {
        return effect switch {
            ConditionEffectIndex.Quiet       => true,
            ConditionEffectIndex.Weak        => true,
            ConditionEffectIndex.Slowed      => true,
            ConditionEffectIndex.Sick        => true,
            ConditionEffectIndex.Dazed       => true,
            ConditionEffectIndex.Stunned     => true,
            ConditionEffectIndex.Blind       => true,
            ConditionEffectIndex.Hallucinating => true,
            ConditionEffectIndex.Drunk       => true,
            ConditionEffectIndex.Paralyzed   => true,
            ConditionEffectIndex.Bleeding    => true,
            ConditionEffectIndex.Stasis      => true,
            ConditionEffectIndex.ArmorBroken => true,
            ConditionEffectIndex.Hexed       => true,
            ConditionEffectIndex.Unstable    => true,
            ConditionEffectIndex.Darkness    => true,
            ConditionEffectIndex.Curse       => true,
            ConditionEffectIndex.Silenced    => true,
            ConditionEffectIndex.Exposed     => true,
            _ => false
        };
    }
}

public enum ConditionEffectIndex {
    None           = 0,
    Dead           = 1,
    Quiet          = 2,
    Weak           = 3,
    Slowed         = 4,
    Sick           = 5,
    Dazed          = 6,
    Stunned        = 7,
    Blind          = 8,
    Hallucinating  = 9,
    Drunk          = 10,
    UnstableImmune = 11,
    StunImmune     = 12,
    Invisible      = 13,
    Paralyzed      = 14,
    Speedy         = 15,
    Bleeding       = 16,
    ArmorBrokenImmune = 17,
    Healing        = 18,
    Damaging       = 19,
    Berserk        = 20,
    Paused         = 21,
    Stasis         = 22,
    StasisImmune   = 23,
    Invincible     = 24,
    Invulnerable   = 25,
    Armored        = 26,
    ArmorBroken    = 27,
    Hexed          = 28,
    NinjaSpeedy    = 29,
    Unstable       = 30,
    Darkness       = 31,
    
    // Second batch
    SlowedImmune   = 32,
    DazedImmune    = 33,
    ParalyzedImmune = 34,
    Petrify        = 35,
    PetrifyImmune  = 36,
    PetDisable     = 37,
    Curse          = 38,
    CurseImmune    = 39,
    HpBoost        = 40,
    MpBoost        = 41,
    AttBoost       = 42,
    DefBoost       = 43,
    SpdBoost       = 44,
    DexBoost       = 45,
    VitBoost       = 46,
    WisBoost       = 47,
    Hidden         = 48,
    Muted          = 49,
    PartyVision    = 50,
    XmasVision     = 51,
    Blessed        = 52,
    Exposed        = 53,
    SpookyVision   = 54,
    BunnyVision    = 55,
    Silenced       = 56,
    Energized      = 57,
    QuietImmune    = 58,
    SilencedImmune = 59,
    SickImmune     = 60,
    BleedingImmune = 61,
    ConfusedImmune = 62,
    Protected      = 63,
}

[Flags]
public enum ConditionEffects : ulong {
    None             = 0,
    Dead             = 1ul << 0,
    Quiet            = 1ul << 1,
    Weak             = 1ul << 2,
    Slowed           = 1ul << 3,
    Sick             = 1ul << 4,
    Dazed            = 1ul << 5,
    Stunned          = 1ul << 6,
    Blind            = 1ul << 7,
    Hallucinating    = 1ul << 8,
    Drunk            = 1ul << 9,
    UnstableImmune   = 1ul << 10,
    StunImmune       = 1ul << 11,
    Invisible        = 1ul << 12,
    Paralyzed        = 1ul << 13,
    Speedy           = 1ul << 14,
    Bleeding         = 1ul << 15,
    ArmorBrokenImmune = 1ul << 16,
    Healing          = 1ul << 17,
    Damaging         = 1ul << 18,
    Berserk          = 1ul << 19,
    Paused           = 1ul << 20,
    Stasis           = 1ul << 21,
    StasisImmune     = 1ul << 22,
    Invincible       = 1ul << 23,
    Invulnerable     = 1ul << 24,
    Armored          = 1ul << 25,
    ArmorBroken      = 1ul << 26,
    Hexed            = 1ul << 27,
    NinjaSpeedy      = 1ul << 28,
    Unstable         = 1ul << 29,
    Darkness         = 1ul << 30,
    
    // Second batch 
    SlowedImmune     = 1ul << 32,
    DazedImmune      = 1ul << 33,
    ParalyzedImmune  = 1ul << 34,
    Petrify          = 1ul << 35,
    PetrifiedImmune  = 1ul << 36,
    PetEffectIcon    = 1ul << 37,
    Curse            = 1ul << 38,
    CurseImmune      = 1ul << 39,
    HpBoost          = 1ul << 40,
    MpBoost          = 1ul << 41,
    AttBoost         = 1ul << 42,
    DefBoost         = 1ul << 43,
    SpdBoost         = 1ul << 44,
    DexBoost         = 1ul << 45,
    VitBoost         = 1ul << 46,
    WisBoost         = 1ul << 47,
    Hidden           = 1ul << 48,
    Muted            = 1ul << 49,
    PartyVision      = 1ul << 50,
    XmasVision       = 1ul << 51,
    Blessed          = 1ul << 52,
    Exposed          = 1ul << 53,
    SpookyVision     = 1ul << 54,
    BunnyVision      = 1ul << 55,
    Silenced         = 1ul << 56,
    Energized        = 1ul << 57,
    QuietImmune      = 1ul << 58,
    SilencedImmune   = 1ul << 59,
    SickImmune       = 1ul << 60,
    BleedingImmune   = 1ul << 61,
    ConfusedImmune   = 1ul << 62,
    Protected        = 1ul << 63,
}