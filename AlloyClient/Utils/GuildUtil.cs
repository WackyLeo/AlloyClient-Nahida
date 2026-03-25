using AlloyClient.UiLib.Data;
using AlloyClient.Utils;
using OpenTK.Graphics;

namespace AlloyClient.Game.Util;

public static class GuildUtil {

    public const int Initiate = 0;
    public const int Member = 10;
    public const int Officer = 20;
    public const int Leader = 30;
    public const int Founder = 40;
    public const int MaxMembers = 50;

    public static string RankToString(int rank) => rank switch {
        Initiate => "Initiate",
        Member => "Member",
        Officer => "Officer",
        Leader => "Leader",
        Founder => "Founder",
        _ => "Unknown"
    };

    public static TextureInfo RankToIcon(int rank) {
        var index = rank switch {
            Initiate => 20,
            Member => 19,
            Officer => 18,
            Leader => 17,
            Founder => 16,
            _ => 20
        };
        return TextureHelper.FromGameAtlas("lofiInterfaceBig", index);
    }

    public static TextureInfo GuildFameIcon() =>
        TextureHelper.FromGameAtlas("lofiObj3", 226);

    public static bool AllowedChange(int myRank, int targetRank, int newRank) {
        if (targetRank == newRank) return false;
        if (myRank == Founder && targetRank < Founder && newRank < Founder) return true;
        if (myRank == Leader && targetRank < Leader && newRank <= Leader) return true;
        if (myRank == Officer && targetRank < Officer && newRank < Officer) return true;
        return false;
    }

    public static int PromotedRank(int rank) => rank switch {
        Initiate => Member,
        Member => Officer,
        Officer => Leader,
        _ => Founder
    };

    public static bool CanPromote(int myRank, int targetRank) =>
        AllowedChange(myRank, targetRank, PromotedRank(targetRank));

    public static int DemotedRank(int rank) => rank switch {
        Officer => Member,
        Leader => Officer,
        Founder => Leader,
        _ => Initiate
    };

    public static bool CanDemote(int myRank, int targetRank) =>
        AllowedChange(myRank, targetRank, DemotedRank(targetRank));

    public static bool CanRemove(int myRank, int targetRank) =>
        myRank >= Officer && targetRank < myRank;

    public static int GetRosterSize(string ghallType) => ghallType switch {
        "Guild Hall 1" => 50,
        "Guild Hall 2" => 60,
        "Guild Hall 3" => 75,
        _ => 50
    };
}