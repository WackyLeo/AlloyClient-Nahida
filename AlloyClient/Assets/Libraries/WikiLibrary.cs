using System;
using System.Text.Json.Nodes;
using Common;

namespace AlloyClient.Assets.Libraries;

public static class WikiLibrary {
    private static readonly Logger Log = new(nameof(WikiLibrary));

    public static string RawLootDropJson { get; private set; }
    public static JsonNode LootDropData { get; private set; }

    public static void LoadLootDropJson(string jsonString) {
        try {
            RawLootDropJson = jsonString;
            LootDropData = JsonNode.Parse(jsonString);
            Log.Info("Wiki LootDrop JSON successfully parsed and cached.");
        } catch (Exception e) {
            Log.Error($"Failed to parse Wiki JSON: {e.Message}");
        }
    }
}