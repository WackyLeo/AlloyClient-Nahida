namespace AlloyClient.Game.Objects.Util;

public static class StatsUtil {
    public static string FromId(int statId) {
        return statId switch {
            0 => "Maximum HP",
            3 => "Maximum MP",
            20 => "Attack",
            21 => "Defense",
            22 => "Speed",
            28 => "Dexterity",
            26 => "Vitality",
            27 => "Wisdom",
            102 => "Luck",
            109 => "Maximum Shield",
            110 => "Shield",
            113 => "% Damage Resistance",
            114 => "% To All Damage Multiplier",
            115 => "Defense Damage Reduction Cap",
            129 => "Maximum Ammunition",
            _ => "Invalid Stat!"
        };
    }
}