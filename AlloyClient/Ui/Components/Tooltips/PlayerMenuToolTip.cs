using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Components.Hud.Chat;
using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.UiLib;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Components.Tooltips;

public class PlayerMenuToolTip : Tooltip {
    private readonly Player _target;
    private int _currentY = 56;
    public Vector2i SpawnMouse;
    public PlayerMenuToolTip(Player target) : base(170, 320) {
        _target = target;
        TooltipMode = false;
        MouseEnabled = true;

        var portrait = new ObjectRect(new ObjectRectConfig {
            Width = 32, Height = 32, GlowEnabled = true, OutlineEnabled = true
        });
        portrait.ChangeTexture(TextureHelper.Create(_target.Texture, TextureType.GameAtlas));
        portrait.X = 10;
        portrait.Y = 10;
        AddChild(portrait);

        var nameColor = _target.IsFellowGuild ? 0x00FF00u : 0xFCDF00u; // Green if guild
        var nameText = new SimpleText(EquipmentToolTip.SimpleConfig(_target.Name ?? "Unknown", 14, FontType.Bold, nameColor, outline: 0x000000, glow: 1.0f, thickness: 0));
        nameText.X = 46;
        nameText.Y = 10;
        AddChild(nameText);

        var charInfoText = new SimpleText(EquipmentToolTip.SimpleConfig($"({ObjectLibrary.TypeToObjectProps[_target.Type].DisplayName} {_target.Level})", 14, FontType.Normal, nameColor, outline: 0x000000, glow: 1.0f, thickness: 0));
        charInfoText.X = nameText.X + nameText.Width + 5;
        charInfoText.Y = 10;
        AddChild(charInfoText);
        
        var accountIdText = new SimpleText(EquipmentToolTip.SimpleConfig($"Account ID: {_target.AccountId}", 14, FontType.Bold, nameColor, outline: 0x000000, glow: 1.0f, thickness: 0));
        accountIdText.X = 46;
        accountIdText.Y = 28;
        AddChild(accountIdText);

        if (Map.LocalPlayer != null && Map.LocalPlayer.AccountId <= 4) {
            AddOption("Ban Menu", "lofiInterfaceBig", 10, () => { });
            AddOption("Kick", "lofiInterfaceBig", 10, () => { ChatBox.OnChatOpen.Dispatch($"/kick {_target.Name}"); });
            AddOption("Mute Menu", "lofiInterfaceBig", 4, () => { });
        }

        AddOption("Teleport", "lofiInterface2", 3, () => { 
            var pkt = Teleport.CreatePacket();
            pkt.ObjectId = _target.ObjectId;
            Client.QueuePacket(pkt);
        });

        AddOption("Lock", "lofiInterface2", 5, () => {
            // Lock Logic
        });

        AddOption("Trade", "lofiInterfaceBig", 7, () => {
            var pkt = RequestTrade.CreatePacket();
            pkt.Name = _target.Name;
            Client.QueuePacket(pkt);
        });

        AddOption("Whisper", "lofiInterfaceBig", 21, () => {
            ChatBox.OnChatOpen.Dispatch($"/tell {_target.Name} ");
        });

        AddOption("Block", "lofiInterfaceBig", 8, () => {
            // Block Logic
        });

        ToolHeight = _currentY + 10;
        DrawSprite();
        
        AddEventListener(Event.AddedToStage, () => {
            Scale = Stage.ScreenScale;
            Stage.AddEventListener(ResizeEvent.Resize, OnResizeEvent);
        });
        AddEventListener(Event.RemovedFromStage, () => {
            Stage?.RemoveEventListener(ResizeEvent.Resize, OnResizeEvent);
        });
        
        AddEventListener(Event.AddedToStage, () => {
            Stage.AddEventListener(MouseEvent.LeftDown, OnStageClick);
        });

        AddEventListener(Event.RemovedFromStage, () => {
            Stage?.RemoveEventListener(MouseEvent.LeftDown, OnStageClick);
        });
    }

    private void AddOption(string text, string atlas, int index, Action onClick) {
        var option = new MenuOption(text, atlas, index, ToolWidth, () => {
            onClick?.Invoke();
            Close();
        });
        option.Y = _currentY;
        AddChild(option);
        _currentY += 32;
    }

    private void OnStageClick(MouseEvent args) {
        if (!IsInBounds(args.Coords)) {
            Close();
        }
    }

    private void Close() {
        Parent?.RemoveChild(this);
    }

    private void OnResizeEvent(ResizeEvent args) {
        Scale = Stage.ScreenScale;
        X = SpawnMouse.X - (int)(ToolWidth * Scale.X);
        Y = SpawnMouse.Y - (int)(ToolHeight * Scale.Y);
    }
    
    private class MenuOption : Sprite {
        public MenuOption(string text, string atlas, int index, int width, Action onClick) {
            MouseEnabled = true;
            var icon = new ObjectRect(new ObjectRectConfig {
                Texture = TextureHelper.FromGameAtlas(atlas, index, false),
                Width = 20, Height = 20 
            });
            icon.X = 10;
            icon.Y = 2;
            AddChild(icon);

            var label = new SimpleText(new TextConfig {
                Text = text, FontSize = 18, FontType = FontType.Bold, Color = 0xFFFFFF,
                OutlineThickness = 0.5f, OutlineColor = 0x000000
            });
            label.X = 40;
            label.Y = 4;
            AddChild(label);
            AddEventListener(MouseEvent.LeftClick, new Action(() => onClick?.Invoke()));
        }
    }
}