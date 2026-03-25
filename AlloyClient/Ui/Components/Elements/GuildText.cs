using AlloyClient.Engine.Common;
using AlloyClient.Game.Util;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Components.Elements;

public class GuildText : Sprite {

    private string _name;
    private int _rank;
    private readonly ObjectRect _icon;
    private readonly SimpleText _guildName;

    public GuildText(string name, int rank, int maxWidth = 0) {
        _icon = new ObjectRect(new ObjectRectConfig {
            X = 0,
            Y = 0,
            Width = 24,
            Height = 24,
        });

        _guildName = new SimpleText(new TextConfig {
            X = 24 + 5,
            Y = 5,
            FontSize = 18,
            FontType = FontType.Normal,
            Color = Color.White,
            OutlineThickness = 0.5f,
            MaxWidth = maxWidth == 0 ? 200 : maxWidth - 4,
        });

        Draw(name, rank);
    }

    public void Draw(string name, int rank) {
        if (_name == name && _rank == rank) return;

        _name = name;
        _rank = rank;

        RemoveChild(_icon);
        RemoveChild(_guildName);

        if (string.IsNullOrEmpty(_name)) return;

        _icon.ChangeTexture(GuildUtil.RankToIcon(rank));
        AddChild(_icon);

        _guildName.SetText(_name);
        AddChild(_guildName);
    }
}