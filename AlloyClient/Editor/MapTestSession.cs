using System.Linq;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.UiLib.Extra;

namespace AlloyClient.Editor;

public static class MapTestSession {
    public const int MapTestGameId = -6;

    private static string _pendingJson;
    private static Screen _returnScreen;
    private static bool _active;
    public static bool IsActive => _active;
    
    public static int RequiredRank => GlobalData.Get<CharacterListData>()?.MapMinRank ?? 0;

    public static bool CanTest(out string title, out string reason) {
        if (!GlobalData.Contains<LoginData>()) {
            title = "Not logged in";
            reason = "Log in to test maps.";
            return false;
        }

        var rank = GlobalData.Get<AccountData>()?.Rank ?? 0;
        /* if (rank < RequiredRank) {
            title = "Insufficient Rank";
            reason = $"Testing maps requires rank {RequiredRank} (your rank: {rank}).";
            return false;
        } */

        var chars = GlobalData.Get<CharacterListData>()?.Characters;
        if (chars == null || chars.Length == 0) {
            title = "No character";
            reason = "Create a character first, you test maps with it.";
            return false;
        }

        title = reason = null;
        return true;
    }

    public static void Start(string mapJson, Screen returnScreen) {
        // Enter the world with the last selected character (like playing normally), else the first one
        var chars = GlobalData.Get<CharacterListData>().Characters;
        if (chars.All(c => c.Id != GlobalData.SelectedCharacterId))
            GlobalData.SelectedCharacterId = chars.First().Id;

        _pendingJson = mapJson;
        _returnScreen = returnScreen;
        _active = true;
        ScreenManager.FadeToScreen(new GameScreen(), Easing.SineInOut, 1000, 0x0);
    }

    internal static void ApplyToHello(Hello hello) {
        if (_pendingJson == null) return;
        hello.GameId = MapTestGameId;
        hello.MapJSON = _pendingJson;
        _pendingJson = null;
    }

    /// <summary>Called when the game connection closes. Returns true if we went back to the editor.</summary>
    internal static bool TryReturnToEditor() {
        if (!_active) return false;
        _active = false;
        _pendingJson = null;

        var screen = _returnScreen;
        _returnScreen = null;
        if (screen == null) return false;

        ScreenManager.FadeTo(screen);
        return true;
    }
}
