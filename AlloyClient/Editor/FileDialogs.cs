using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common;
using OpenTK.Platform;

namespace AlloyClient.Editor;

/// <summary>
/// Open/save dialogs for map files. On Linux zenity is called directly (OpenTK's zenity wrapper mangles the filter and the
/// returned path), elsewhere OpenTK's native dialogs are used. Returns false when no dialog can be shown.
/// </summary>
public static class FileDialogs {
    private static readonly Logger Log = new(typeof(FileDialogs));
    private static readonly DialogFileFilter[] MapFilter = [new("Map files", "jm;wmap")];

    private const string LastDirectoryFile = "editor_last_directory.txt";
    private static string _lastDirectory;

    /// <summary>Folder the dialogs open in: the last folder a map was loaded from/saved to (kept between runs), else the desktop.</summary>
    public static string LastDirectory {
        get {
            if (_lastDirectory == null) {
                _lastDirectory = ReadSavedDirectory();
                if (_lastDirectory == null || !Directory.Exists(_lastDirectory)) _lastDirectory = DefaultDirectory;
            }

            return _lastDirectory;
        }
    }

    public static string DefaultDirectory {
        get {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop)) return desktop;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home) && Directory.Exists(home)) return home;

            return AppContext.BaseDirectory;
        }
    }

    private static string SavedDirectoryPath => Path.Combine(State.Settings.LocalFolderPath, LastDirectoryFile);

    private static string ReadSavedDirectory() {
        try {
            return File.Exists(SavedDirectoryPath) ? File.ReadAllText(SavedDirectoryPath).Trim() : null;
        } catch (Exception e) {
            Log.Warn($"Couldn't read the editor's last directory: {e.Message}");
            return null;
        }
    }

    /// <param name="path">Selected file, or null if the user cancelled.</param>
    public static bool TryOpenMap(out string path) {
        path = null;
        if (OperatingSystem.IsLinux()) {
            if (!TryZenity(out path, "--file-selection", "--title=Load map",
                    $"--filename={WithSlash(LastDirectory)}",
                    "--file-filter=Map files (jm, wmap) | *.jm *.wmap *.JM *.WMAP",
                    "--file-filter=JSON maps (jm) | *.jm *.JM",
                    "--file-filter=WMap (wmap) | *.wmap *.WMAP",
                    "--file-filter=All files | *"))
                return TryOpenTk(out path);
            Remember(path);
            return true;
        }

        return TryOpenTk(out path);
    }

    /// <param name="path">Selected file, or null if the user cancelled.</param>
    public static bool TrySaveMap(string defaultName, bool wmap, out string path) {
        path = null;
        var ext = wmap ? ".wmap" : ".jm";
        bool ok;

        if (OperatingSystem.IsLinux()) {
            ok = TryZenity(out path, "--file-selection", "--save", "--confirm-overwrite", "--title=Save map",
                $"--filename={Path.Combine(LastDirectory, defaultName)}",
                wmap ? "--file-filter=WMap (wmap) | *.wmap" : "--file-filter=JSON map (jm) | *.jm");
            if (!ok) ok = TrySaveOpenTk(defaultName, wmap, out path);
        } else {
            ok = TrySaveOpenTk(defaultName, wmap, out path);
        }

        if (!ok) return false;
        if (path != null && !path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) path += ext;
        Remember(path);
        return true;
    }

    /// <summary>Remembers the folder of a loaded/saved map, also for the next time the client runs.</summary>
    public static void Remember(string path) {
        if (string.IsNullOrEmpty(path)) return;
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || dir == _lastDirectory) return;

        _lastDirectory = dir;
        try {
            File.WriteAllText(SavedDirectoryPath, dir);
        } catch (Exception e) {
            Log.Warn($"Couldn't save the editor's last directory: {e.Message}");
        }
    }

    private static string WithSlash(string dir) => dir.EndsWith(Path.DirectorySeparatorChar) ? dir : dir + Path.DirectorySeparatorChar;

    /// <summary>Runs zenity. Returns false if it isn't available or failed, path is null when the user cancelled.</summary>
    private static bool TryZenity(out string path, params string[] args) {
        path = null;
        try {
            var psi = new ProcessStartInfo("zenity") {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var process = Process.Start(psi);
            if (process == null) return false;
            var output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();

            switch (process.ExitCode) {
                case 0:
                    path = output.Trim('\r', '\n');
                    if (path == "") path = null;
                    return true;
                case 1: // Cancelled / closed
                    return true;
                default:
                    Log.Warn($"zenity exited with code {process.ExitCode}");
                    return false;
            }
        } catch (Exception e) {
            Log.Warn($"zenity unavailable: {e.Message}");
            return false;
        }
    }

    private static bool TryOpenTk(out string path) {
        path = null;
        try {
            var result = Toolkit.Dialog.ShowOpenDialog(Main.GameInstance.Window, "Load map", LastDirectory, MapFilter, 0);
            path = result?.FirstOrDefault()?.Trim('\r', '\n', '"');
            Remember(path);
            return true;
        } catch (Exception e) {
            Log.Warn($"Open dialog unavailable: {e.Message}");
            return false;
        }
    }

    private static bool TrySaveOpenTk(string defaultName, bool wmap, out string path) {
        path = null;
        try {
            DialogFileFilter[] filter = [wmap ? new DialogFileFilter("WMap", "wmap") : new DialogFileFilter("JSON map", "jm")];
            path = Toolkit.Dialog.ShowSaveDialog(Main.GameInstance.Window, "Save map", LastDirectory, defaultName, filter, 0)?.Trim('\r', '\n', '"');
            return true;
        } catch (Exception e) {
            Log.Warn($"Save dialog unavailable: {e.Message}");
            return false;
        }
    }
}
