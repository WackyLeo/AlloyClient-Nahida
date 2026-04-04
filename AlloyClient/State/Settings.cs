using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using AlloyClient.Data;
using AlloyClient.State.SettingTypes;
using AlloyClient.Utils;
using Common;
using Newtonsoft.Json;
using OpenTK.Platform;

namespace AlloyClient.State;

public static class Settings {
    // Account and setting options
    private const string LocalFolderName = "AlloyClient";

    private const string AccountFileName = "account.json";
    private const string SettingsPath = "settings.json";

    // App-Engine connection info
    public const string BuildVersion = "localhost";
    public const string BuildLabel = $"Version {BuildVersion}";

    private const string AppEngineAddress = "135.125.181.109"; // 135.125.181.109
    private const string AppEnginePort = "8080"; // 65001

    public const string AppEngineUrl = $"http://{AppEngineAddress}:{AppEnginePort}";
    public const int AppEngineTimeout = 10000;

    // World-Engine connection info
    public const string GameServerAddress = "135.125.181.109"; // 116.255.20.214
    public const ushort GameServerPort = 26959; // 26959

    // Screen-Information
    public const int DefaultScreenWidth = 1280;
    public const int DefaultScreenHeight = 720;

    public const float MinCameraZoom = 0.5f;
    public const float MaxCameraZoom = 5;

    private static readonly Logger Log = new(typeof(Settings));
    public static readonly string LocalFolderPath;

    private static readonly Dictionary<string, ISettingType> SettingTypes = [];
    public static readonly List<InputSetting> Inputs = [];

    static Settings() {
        LocalFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LocalFolderName);

        Directory.CreateDirectory(LocalFolderPath);
    }

    public static void ResetToDefault() {
        PacketLogging.SetValue(PacketLogLevel.Off);
        Options.Set(Scancode.Escape);

        Debug.Set(Scancode.F1);
        Debug2.Set(Scancode.F2);
        Debug3.Set(Scancode.F3);
        Debug4.Set(Scancode.F4);
        Debug5.Set(Scancode.F5);

        #region HOTKEYS

        // Movement
        MoveUp.Set(Scancode.W);
        MoveDown.Set(Scancode.S);
        MoveLeft.Set(Scancode.A);
        MoveRight.Set(Scancode.D);

        // Camera
        RotateLeft.Set(Scancode.Q);
        RotateRight.Set(Scancode.E);
        ResetCameraAngle.Set(Scancode.Z);
        CenterPlayerKey.Set(Scancode.X);

        // Key 
        AutoFire.Set(Scancode.I);
        Special.Set(Scancode.Spacebar);
        Interact.Set(Scancode.D0);
        Escape.Set(Scancode.R);
        Crouch.Set(Scancode.Tab);

        // Chat
        Chat.Set(Scancode.Return);
        ChatCommand.Set(Scancode.QuestionMark);
        TellKey.Set(Scancode.F2);
        GuildChat.Set(Scancode.G);
        PartyChat.Set(Scancode.P);
        ChatHistoryUp.Set(Scancode.PageUp);
        ChatHistoryDown.Set(Scancode.PageDown);

        // Inventory
        HealthPotion.Set(Scancode.F);
        MagicPotion.Set(Scancode.V);
        InvOne.Set(Scancode.D1);
        InvTwo.Set(Scancode.D2);
        InvThree.Set(Scancode.D3);
        InvFour.Set(Scancode.D4);
        InvFive.Set(Scancode.D5);
        InvSix.Set(Scancode.D6);
        InvSeven.Set(Scancode.D7);
        InvEight.Set(Scancode.D8);

        // Misc
        PerformanceStats.Set(Scancode.F5);
        SwitchTabs.Set(Scancode.B);
        ResetMScale.Set(Scancode.Unknown);
        SetBagPriority.Set(Scancode.Unknown);
        FullscreenKey.Set(Scancode.F11);

        #endregion

        #region VALUES

        // Camera Settings
        CameraAngle.SetValue(0f);
        CameraZoom.SetValue(1);

        AllowRotation.SetValue(true);
        RotateSpeed.SetValue(0.003f);

        // Screen Settings
        FpsCap.SetValue(-1);
        VSync.SetValue(false);
        Fullscreen.SetValue(false);

        // Sounds Settings
        MusicVolume.SetValue(0.5f);
        SfxVolume.SetValue(0.5f);
        WeaponSfxVolume.SetValue(0.5f);
        LootSfxVolume.SetValue(0.5f);

        PlayMusic.SetValue(true);
        PlaySfx.SetValue(true);
        PlayWeaponSfx.SetValue(true);
        PlayLootSfx.SetValue(true);

        // Personal Preferences
        ToggleLeftToMax.SetValue(true);
        ToggleBarText.SetValue(true);
        InventorySwap.SetValue(true);
        CenterPlayer.SetValue(true);

        // Chat Settings
        ChatInclude.SetValue(0);
        ChatVisible.SetValue(true);
        ChatScaling.SetValue(0f);
        ChatHideList.SetValue(0);

        // Particle Settings
        EyeCandyParticles.SetValue(true);
        ReducedParticles.SetValue(false);

        #endregion
    }

    public static void LoadSettings() {
        // Try to load the LocalAccount
        LoadLocalAccount();

        // Initialize settings to default
        ResetToDefault();

        // Try to load the saved settings
        TryLoadSettings();

        // Call Save settings here to update the list for new entry's
        SaveSettings();
    }

    private static void TryLoadSettings() {
        if (!File.Exists(SettingsPath)) {
            Log.Info("Settings file not found.");
            return;
        }

        // Load the settingsFile into txt then parse it through XML
        var settingsContent = File.ReadAllText(SettingsPath);
        var settingsXml = new XmlDocument();

        settingsXml.LoadXml(settingsContent);

        // Check for valid XML
        var settingsRoot = settingsXml.DocumentElement;
        if (settingsRoot == null) {
            Log.Warn("Settings file is empty.");
            return;
        }

        // Iterate over each entry in the settings field rather than the XML
        foreach (var field in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            // I guess it's safeguarding
            if (!typeof(ISettingType).IsAssignableFrom(field.FieldType)) continue;

            var settingElem = settingsRoot[field.Name];
            if (settingElem == null) {
                Log.Warn($"Setting not found: {field.Name}");
                continue;
            }

            var setting = (ISettingType)field.GetValue(null);
            if (setting == null) {
                Log.Warn($"Failed to get setting: {field.Name}");
                continue;
            }

            // Deserialize and parse the setting
            setting.Deserialize(settingElem.InnerText);
            field.SetValue(null, setting);

            SettingTypes[field.Name] = setting;
            if (setting is InputSetting inputSetting) Inputs.Add(inputSetting);
        }

        Log.Info("Settings loaded");
    }

    public static void SaveSettings() {
        var settingsDict = new Dictionary<string, string>();

        // Iterate over each entry in the settings field rather than the XML
        foreach (var field in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            // I guess it's safeguarding
            if (!typeof(ISettingType).IsAssignableFrom(field.FieldType)) continue;

            var setting = (ISettingType)field.GetValue(null);
            if (setting == null) {
                Log.Warn($"Failed to get setting: {field.Name}");
                continue;
            }

            // Save the setting to our vector
            settingsDict[field.Name] = setting.Serialize();
        }

        // Create a new setting XML
        var settingsXml = new XmlDocument();
        var settingsRoot = settingsXml.CreateElement("Settings");
        settingsXml.AppendChild(settingsRoot);

        // Loop and add out Vector's entries to the XML object
        foreach (var (name, value) in settingsDict) {
            var settingElem = settingsXml.CreateElement(name);
            settingElem.InnerText = value;
            settingsRoot.AppendChild(settingElem);
        }

        // Save the XML to a file
        settingsXml.Save(SettingsPath);

        Log.Info("Settings saved.");
    }

    private static void LoadLocalAccount() {
        var filePath = Path.Combine(LocalFolderPath, AccountFileName);
        if (!File.Exists(filePath)) {
            Log.Debug("No local account data found");
            return;
        }

        // Wrap the ReadAllText function in a try catch because C#
        string fileContent;
        try {
            fileContent = File.ReadAllText(filePath);
        } catch (Exception e) {
            Log.Error($"Failed to read to file {AccountFileName}: {e.Message}");
            return;
        }

        // Parse the fileContents into usable JSON
        var jsonContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
        if (jsonContent == null) {
            Log.Error("Failed to parse local account data");
            return;
        }

        var loadedGuid = jsonContent.TryGetValue("GUID", out var guid) && !string.IsNullOrWhiteSpace(guid);
        var loadedPass = jsonContent.TryGetValue("Password", out var password) && !string.IsNullOrWhiteSpace(password);

        if (!loadedGuid || !loadedPass) {
            Log.Error("Failed to parse account data, GUID or PASSWORD are incomplete");
            return;
        }

        if (guid == string.Empty && password == string.Empty) {
            Log.Debug("No local account data");
            return;
        }

        // Verify the base64 encoding
        var data = new byte[(password.Length * 3 + 3) / 4];
        if (!Convert.TryFromBase64String(password, data.AsSpan(), out var count)) {
            Log.Error("Invalid Base64 encoding on password");
            return;
        }

        GlobalData.Add(new LoginData(guid, Encoding.UTF8.GetString(data.AsSpan(0, count))));
    }

    public static void SaveLocalAccount() {
        // Initialize data to either a stored LoginData or Default
        var data = GlobalData.Get<LoginData>() ?? LoginData.Default;

        // Load in password bytes
        var bytes = new byte[Encoding.UTF8.GetByteCount(data.Password.AsSpan())];
        if (!Encoding.UTF8.TryGetBytes(data.Password.AsSpan(), bytes.AsSpan(), out var byteCount)) {
            Log.Error("Failed to get password bytes");
            return;
        }

        // Encode password
        var chars = new char[4 * (data.Password.Length + 2) / 3];
        if (!Convert.TryToBase64Chars(bytes.AsSpan(0, byteCount), chars.AsSpan(), out var charCount)) {
            Log.Error("Failed to Base64 encode password");
            return;
        }

        var guid = data.GUID;
        var password = new string(chars.AsSpan(0, charCount));

        var jsonString = JsonConvert.SerializeObject(new Dictionary<string, string>
            { { "GUID", guid }, { "Password", password } });

        // Write the encoded password and email to the account.json 
        try {
            File.WriteAllText(Path.Combine(LocalFolderPath, AccountFileName), jsonString);
        } catch (Exception e) {
            Log.Error($"Failed to write to file {AccountFileName}: {e.Message}");
        }
    }

    public static T GetSetting<T>(string settingKey) where T : ISettingType {
        if (!SettingTypes.TryGetValue(settingKey, out var setting)) return default;

        return (T)setting;
    }

    public static void SetSetting(string settingKey, ISettingType value) {
        if (!SettingTypes.TryGetValue(settingKey, out var setting)) {
            Logger.Error($"Failed to save, setting not found: {settingKey}");
            return;
        }

        setting.SetValue(value); // Makes sure we're updating the static field, no use of reflection :pray:
    }

    public static void SetWindowSize(int width, int height) {
        ScreenWidth = width;
        ScreenHeight = height;
    }
#pragma warning disable CA2211

    #region HOTKEYS

    // Movement
    public static InputSetting Options = new();
    public static InputSetting MoveUp = new();
    public static InputSetting MoveDown = new();
    public static InputSetting MoveLeft = new();
    public static InputSetting MoveRight = new();
    public static InputSetting RotateLeft = new();
    public static InputSetting RotateRight = new();

    // Camera
    public static InputSetting ResetCameraAngle = new();
    public static InputSetting CenterPlayerKey = new();

    // Key 
    public static InputSetting AutoFire = new();
    public static InputSetting Special = new();
    public static InputSetting Interact = new();
    public static InputSetting Escape = new();
    public static InputSetting Crouch = new();

    public static InputSetting Debug = new();
    public static InputSetting Debug2 = new();
    public static InputSetting Debug3 = new();
    public static InputSetting Debug4 = new();
    public static InputSetting Debug5 = new();

    // Chat
    public static InputSetting Chat = new();
    public static InputSetting ChatCommand = new();
    public static InputSetting TellKey = new();
    public static InputSetting GuildChat = new();
    public static InputSetting PartyChat = new();
    public static InputSetting ChatHistoryUp = new();
    public static InputSetting ChatHistoryDown = new();

    // Inventory
    public static InputSetting HealthPotion = new();
    public static InputSetting MagicPotion = new();
    public static InputSetting InvOne = new();
    public static InputSetting InvTwo = new();
    public static InputSetting InvThree = new();
    public static InputSetting InvFour = new();
    public static InputSetting InvFive = new();
    public static InputSetting InvSix = new();
    public static InputSetting InvSeven = new();
    public static InputSetting InvEight = new();

    // Misc
    public static InputSetting PerformanceStats = new();
    public static InputSetting SwitchTabs = new();
    public static InputSetting ResetMScale = new();
    public static InputSetting SetBagPriority = new();
    public static InputSetting FullscreenKey = new();

    // Dev

    #endregion

    #region VALUES

    public static ValueSetting<PacketLogLevel> PacketLogging = PacketLogLevel.Off;

    // Camera Settings
    public static ValueSetting<float> CameraAngle = 0f;
    public static ValueSetting<float> CameraZoom = 1;
    public static ValueSetting<bool> AllowRotation = true;
    public static ValueSetting<float> RotateSpeed = 0.003f;

    // Screen Settings
    public static ValueSetting<int> FpsCap = -1;
    public static ValueSetting<bool> VSync = false;
    public static ValueSetting<bool> Fullscreen = false;
    public static ValueSetting<int> ScreenWidth = DefaultScreenWidth;
    public static ValueSetting<int> ScreenHeight = DefaultScreenHeight;
    public static ValueSetting<int> NonFullscreenWidth = DefaultScreenWidth;
    public static ValueSetting<int> NonFullscreenHeight = DefaultScreenHeight;

    // Sounds Settings
    public static ValueSetting<float> MusicVolume = 0.5f;
    public static ValueSetting<float> SfxVolume = 0.5f;
    public static ValueSetting<float> WeaponSfxVolume = 0.5f;
    public static ValueSetting<float> LootSfxVolume = 0.5f;

    public static ValueSetting<bool> PlayMusic = true;
    public static ValueSetting<bool> PlaySfx = true;
    public static ValueSetting<bool> PlayWeaponSfx = true;
    public static ValueSetting<bool> PlayLootSfx = true;

    // Personal Preferences
    public static ValueSetting<bool> ToggleLeftToMax = true;
    public static ValueSetting<bool> ToggleBarText = true;
    public static ValueSetting<bool> InventorySwap = true;
    public static ValueSetting<bool> CenterPlayer = true;

    // Chat Settings
    public static ValueSetting<int> ChatInclude = 0;
    public static ValueSetting<bool> ChatVisible = true;
    public static ValueSetting<float> ChatScaling = 0f;
    public static ValueSetting<int> ChatHideList = 0;

    // Particle Settings
    public static ValueSetting<bool> EyeCandyParticles = true;
    public static ValueSetting<bool> ReducedParticles = false;

    #endregion

#pragma warning restore CA2211
}