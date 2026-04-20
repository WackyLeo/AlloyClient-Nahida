using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Components.Hud;
using AlloyClient.Game.Components.Hud.Inventory;
using AlloyClient.Game.Objects;
using AlloyClient.Ui.Components.Elements;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.Components.Tooltips;

public sealed class PlayerToolTip : Tooltip {
    private const int BaseWidth = 235;
    private const int BaseHeight = 260;
    private readonly EquippedGrid _eGrid;
    private readonly StatusBar _hpBar;
    private readonly StatusBar _mpBar;
    private readonly Player _player;

    private readonly ObjectRect _portrait;

    public PlayerToolTip(Player player) : base(BaseWidth, BaseHeight, 0xBF161616u) {
        _player = player;
        _portrait = new ObjectRect(new ObjectRectConfig {
            Width = 32, Height = 32, GlowEnabled = true, OutlineEnabled = true
        });
        _portrait.X = 10;
        _portrait.Y = 10;
        AddChild(_portrait);

        var nameColor = player.IsFellowGuild ? 0x00FF00u : 0xFCDF00u; // Green if guild
        var nameText = new SimpleText(EquipmentToolTip.SimpleConfig(player.Name ?? "Unknown", 15, FontType.Bold,
            nameColor, 0x000000, glow: 1.0f, thickness: 0));
        nameText.X = 46;
        nameText.Y = 10;
        AddChild(nameText);

        var charInfoText = new SimpleText(EquipmentToolTip.SimpleConfig(
            $"({ObjectLibrary.TypeToObjectProps[player.Type].DisplayName} {player.Level})", 15, FontType.Normal,
            nameColor, 0x000000, glow: 1.0f, thickness: 0));
        charInfoText.X = nameText.X + nameText.Width + 5;
        charInfoText.Y = 10;
        AddChild(charInfoText);

        var accountIdText = new SimpleText(EquipmentToolTip.SimpleConfig($"Account ID: {player.AccountId}", 15,
            FontType.Bold, nameColor, 0x000000, glow: 1.0f, thickness: 0));
        accountIdText.X = 46;
        accountIdText.Y = 28;
        AddChild(accountIdText);

        var rankText = new SimpleText(EquipmentToolTip.SimpleConfig("Rank:", 19, FontType.Normal, 0xB3B3B3, 0x000000,
            glow: 0.5f, thickness: 0));
        rankText.X = 10;
        rankText.Y = 50;
        AddChild(rankText);

        var star = new FameStar(16, player.Stars);
        star.X = rankText.X + rankText.Width + 5;
        star.Y = rankText.Y + rankText.Height / 2 - star.Height / 2;
        AddChild(star);

        var guiltText = new GuildText(player.Guild ?? "Null", player.GuildRank);
        guiltText.X = 10;
        guiltText.Y = 75;
        AddChild(guiltText);

        _hpBar = new StatusBar(BaseWidth - 20, 24, 0xE03838, 0x545454, 0x363636, "HP");
        _hpBar.X = 10;
        _hpBar.Y = 110;
        AddChild(_hpBar);

        _mpBar = new StatusBar(BaseWidth - 20, 24, 0x6084E0, 0x545454, 0x363636, "MP");
        _mpBar.X = 10;
        _mpBar.Y = 140;
        AddChild(_mpBar);

        var items = new ItemDesc[4];
        CutEdges[] Cuts = [CutEdges.Left, CutEdges.None, CutEdges.None, CutEdges.Right];
        for (byte i = 0; i < 4; i++) {
            items[i] = _player.Equipment[i];

            var slot = new ItemTile(null, i, false, Cuts[i], true, (byte)_player.Properties.SlotTypes[i], 49, 0x454545,
                false);
            slot.X = 10 + i % 4 * (49 + 4) + 3;
            slot.Y = 170 + i / 4 * (49 + 4) + 3;
            slot.SetItem(items[i]);
            AddChild(slot);
        }

        var clickMsg = new SimpleText(EquipmentToolTip.SimpleConfig("(Click to open menu)", 14, FontType.Bold, 0xB3B3B3,
            thickness: 0, glow: 0.5f, outline: 0x000000));
        clickMsg.SetAnchor(UiAnchor.MiddleTop);
        clickMsg.X = ToolWidth / 2;
        clickMsg.Y = 235;
        AddChild(clickMsg);

        DrawSprite();
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnFrameEnter() {
        if (_player == null) return;
        _portrait.ChangeTexture(TextureHelper.Create(_player.Texture, TextureType.GameAtlas));
        _hpBar.Update(_player.Hp, _player.MaxHp, _player.MaxHpBoost, -1, _player.Level);
        _mpBar.Update(_player.Mp, _player.MaxMp, _player.MaxMpBoost, -1, _player.Level);
    }
}