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
        new("Stun Immune",      ConditionEffects.StunImmume,        null),
        new("Invisible",        ConditionEffects.Invisible,         null),
        new("Paralyzed",        ConditionEffects.Paralyzed,         [53, 54]),
        new("Speedy",           ConditionEffects.Speedy,            [0]),
        new("Bleeding",         ConditionEffects.Bleeding,          [46]),
        new("Armor Broken Immune", ConditionEffects.ArmorBreakImmune, null),
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
        new("Paralyzed Immune", ConditionEffects.ParalyzeImmune,   null),
        new("Petrify",          ConditionEffects.Petrify,           null),
        new("Petrify Immune",   ConditionEffects.PetrifyImmune,   null),
        new("Pet Disable",      ConditionEffects.PetDisable,     [27]),
        new("Curse",            ConditionEffects.Curse,             [58]),
        new("Curse Immune",     ConditionEffects.CurseImmune,       null),
        new("HP Boost",         ConditionEffects.HPBoost,           [32]),
        new("MP Boost",         ConditionEffects.MPBoost,           [33]),
        new("Att Boost",        ConditionEffects.AttBoost,          [34]),
        new("Def Boost",        ConditionEffects.DefBoost,          [35]),
        new("Spd Boost",        ConditionEffects.SpdBoost,          [36]),
        new("Dex Boost",        ConditionEffects.DexBoost,          [37]),
        new("Vit Boost",        ConditionEffects.VitBoost,          [38]),
        new("Wis Boost",        ConditionEffects.WisBoost,          [39]),
        new("Hidden",           ConditionEffects.Hidden,            null),
        new("Muted",            ConditionEffects.Muted,             [21]),
        new("Party Vision",     ConditionEffects.PartyVision,       null),
        new("XMas Vision",      ConditionEffects.XMasVision,        null),
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
        return (int)0;
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
    public enum ConditionEffects : ulong
    {
        Dead = 1 << 0,
        Quiet = 1 << 1,
        Weak = 1 << 2,
        Slowed = 1 << 3,
        Sick = 1 << 4,
        Dazed = 1 << 5,
        Stunned = 1 << 6,
        Blind = 1 << 7,
        Hallucinating = 1 << 8,
        Drunk = 1 << 9,
        UnstableImmune = 1 << 10,
        StunImmume = 1 << 11,
        Invisible = 1 << 12,
        Paralyzed = 1 << 13,
        Speedy = 1 << 14,
        Bleeding = 1 << 15,
        ArmorBreakImmune = 1 << 16,
        Healing = 1 << 17,
        Damaging = 1 << 18,
        Berserk = 1 << 19,
        Paused = 1 << 20,
        Stasis = 1 << 21,
        StasisImmune = 1 << 22,
        Invincible = 1 << 23,
        Invulnerable = 1 << 24,
        Armored = 1 << 25,
        ArmorBroken = 1 << 26,
        Hexed = 1 << 27,
        NinjaSpeedy = 1 << 28,
        Unstable = 1 << 29,
        Darkness = 1 << 30,
        SlowedImmune = (ulong)1 << 31,
        DazedImmune = (ulong)1 << 32,
        ParalyzeImmune = (ulong)1 << 33,
        Petrify = (ulong)1 << 34,
        PetrifyImmune = (ulong)1 << 35,
        PetDisable = (ulong)1 << 36,
        Curse = (ulong)1 << 37,
        CurseImmune = (ulong)1 << 38,
        HPBoost = (ulong)1 << 39,
        MPBoost = (ulong)1 << 40,
        AttBoost = (ulong)1 << 41,
        DefBoost = (ulong)1 << 42,
        SpdBoost = (ulong)1 << 43,
        DexBoost = (ulong)1 << 44,
        VitBoost = (ulong)1 << 45,
        WisBoost = (ulong)1 << 46,
        Hidden = (ulong)1 << 47,
        Muted = (ulong)1 << 48,
        PartyVision = (ulong)1 << 49,
        XMasVision = (ulong)1 << 50,
        Blessed = (ulong)1 << 51,
        Exposed = (ulong)1 << 52,
        SpookyVision = (ulong)1 << 53,
        BunnyVision = (ulong)1 << 54,
        Silenced = (ulong)1 << 55,
        Energized = (ulong)1 << 56,
        QuietImmune = (ulong)1 << 57,
        SilencedImmune = (ulong)1 << 58,
        SickImmune = (ulong)1 << 59,
        BleedingImmune = (ulong)1 << 60,
        ConfusedImmune = (ulong)1 << 61,
        Protected = (ulong)1 << 62,
        Intangible = (ulong)1 << 63,
        RingDamage = (ulong)1 << 64
    }