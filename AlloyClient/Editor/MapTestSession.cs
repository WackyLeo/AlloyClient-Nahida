using System.Linq;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.Game;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.UiLib.Extra;

namespace AlloyClient.Editor;

/// <summary>
/// "Test" button of the editor: connects to the game server with the map json in the hello packet (same as the flash
/// client's map editor, game id -6) and comes back to the editor once the connection ends.
/// </summary>
public static class MapTestSession {
    public const int MapTestGameId = -6;

    private static string _pendingJson;
    private static Screen _returnScreen;
    private static bool _active;

    public static bool CanTest(out string reason) {
        if (!GlobalData.Contains<LoginData>()) {
            reason = "Log in to test maps";
            return false;
        }

        var chars = GlobalData.Get<CharacterListData>()?.Characters;
        if (chars == null || chars.Length == 0) {
            reason = "You need a character to test maps";
            return false;
        }

        reason = null;
        return true;
    }

    public static void Start(string mapJson, Screen returnScreen) {
        var chars = GlobalData.Get<CharacterListData>().Characters;
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
