using System;
using System.Collections.Generic;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Input;
using AlloyClient.Utils;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Components.Panels;

public class PlayerGroupMenu : Tooltip {
    
    public PlayerGroupMenu(List<Player> players) : base(160, 40, 0xBF161616u) {
        TooltipMode = false;
        MouseEnabled = true;

        int y = 10;
        foreach (var p in players) {
            var nameColor = p.IsFellowGuild ? 0x00FF00u : 0xFFFFFFu;
            
            var option = new MenuOption(p.Name ?? "Unknown", "lofiInterface2", 3, ToolWidth, () => {
                var pkt = Teleport.CreatePacket();
                pkt.ObjectId = p.ObjectId;
                Client.QueuePacket(pkt);
                Close();
            }, nameColor);
            
            option.Y = y;
            AddChild(option);
            y += 28;
        }

        ToolHeight = y + 10;
        DrawSprite();
        
        
        AddEventListener(Event.AddedToStage, () => {
            Scale = Stage.ScreenScale;
            Stage.AddEventListener(ResizeEvent.Resize, OnResizeEvent);
        });
        AddEventListener(Event.RemovedFromStage, () => {
            Stage?.RemoveEventListener(ResizeEvent.Resize, OnResizeEvent);
        });
        
        AddEventListener(Event.AddedToStage, new Action(() => {
            Stage.AddEventListener(MouseEvent.LeftDown, new Action<MouseEvent>(OnStageClick));
        }));

        AddEventListener(Event.RemovedFromStage, new Action(() => {
            Stage?.RemoveEventListener(MouseEvent.LeftDown, new Action<MouseEvent>(OnStageClick));
        }));
        
        
    }

    private void OnStageClick(MouseEvent args) {
        if (!IsInBounds(args.Coords)) Close();
    }

    private void Close() {
        Parent?.RemoveChild(this);
    }
    
    public Vector2i SpawnMouse;

    private void OnResizeEvent(ResizeEvent args) {
        Scale = Stage.ScreenScale;
        X = SpawnMouse.X - (int)(ToolWidth * Scale.X);
        Y = SpawnMouse.Y + (int)(0);
    }
    
    private class MenuOption : Sprite {
        public MenuOption(string text, string atlas, int index, int width, Action onClick, uint color) {
            MouseEnabled = true;

            var bgHover = new SdfRect(new SdfRectConfig { Width = width, Height = 28, Color = 0xFF545454u, Alpha = 0f, Cut = 0 });
            AddChild(bgHover);

            var icon = new ObjectRect(new ObjectRectConfig { Texture = TextureHelper.FromGameAtlas(atlas, index, false), Width = 16, Height = 16 });
            icon.X = 10;
            icon.Y = 4;
            AddChild(icon);

            var label = new SimpleText(new TextConfig { Text = text, FontSize = 16, FontType = FontType.Bold, Color = color, OutlineThickness = 0.5f, OutlineColor = 0x000000 });
            label.X = 40;
            label.Y = 4;
            AddChild(label);
            
            AddEventListener(MouseEvent.MouseOver, new Action(() => { label.SetColor(0xFCDF00); }));
            AddEventListener(MouseEvent.MouseOut, new Action(() => { label.SetColor(color); }));
            AddEventListener(MouseEvent.LeftClick, new Action(() => onClick?.Invoke()));
        }
    }
}