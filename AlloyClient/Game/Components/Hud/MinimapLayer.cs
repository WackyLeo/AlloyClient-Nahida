using System;
using System.Collections.Generic;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Input;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;
using Common;

namespace AlloyClient.Game.Components.Hud;

public sealed class MinimapLayer : Container {

    private const int MaxEntities = 1000;
    private const int VertexSize = MaxEntities * 4;
    private const int IndexSize = MaxEntities * 6;

    private int _count = 0;
    private float _size;

    private static Entity _focus;

    public readonly List<Player> HoveredPlayers = new();

    public MinimapLayer() : base(new ContainerConfig { EnableClip = true }) {
        TextureId = TextureType.Color;
        MouseEnabled = true;
        ResizeBackBuffer();

        SetGraphicsBuffer();
        SelfContentWidth = Minimap.MapSize;
        SelfContentHeight = Minimap.MapSize;
        UpdateBounds();
        
        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    private void ResizeBackBuffer() {
        VertexData = new VertexUi[VertexSize];
        Indices = new ushort[IndexSize];
        OverridePrimCount = 0;
    }

    public static void SetFocus(Entity entity) {
        _focus = entity;
    }
    
    public void SetSize(float size) => _size = size;

    private void AddObject(Vector2 pos, Color color) {
        if (_count >= MaxEntities) return;

        const float size = 2.5f;

        VertexData[_count * 4 + 0] = new VertexUi(new Vector2(pos.X - size, pos.Y - size), color);
        VertexData[_count * 4 + 1] = new VertexUi(new Vector2(pos.X + size, pos.Y - size), color);
        VertexData[_count * 4 + 2] = new VertexUi(new Vector2(pos.X + size, pos.Y + size), color);
        VertexData[_count * 4 + 3] = new VertexUi(new Vector2(pos.X - size, pos.Y + size), color);

        Indices[_count * 6 + 0] = (ushort)(_count * 4 + 0);
        Indices[_count * 6 + 1] = (ushort)(_count * 4 + 1);
        Indices[_count * 6 + 2] = (ushort)(_count * 4 + 2);
        Indices[_count * 6 + 3] = (ushort)(_count * 4 + 0);
        Indices[_count * 6 + 4] = (ushort)(_count * 4 + 2);
        Indices[_count * 6 + 5] = (ushort)(_count * 4 + 3);

        _count++;
        OverridePrimCount += 2;
    }

    
    private void OnAddedToStage() {
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnRemovedFromStage() {
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void OnFrameEnter() {
        if (Map.LocalPlayer == null || _focus == null) return;
        
        _count = 0;
        OverridePrimCount = 0;
        HoveredPlayers.Clear();

        var center = new Vector2(Minimap.MapSize / 2f);
        var inBounds = IsInBounds(MouseInput.GetMousePosition());
        
        var screenMouse = MouseInput.GetMousePosition();
        var localMouseX = (screenMouse.X - _trueX) / _trueScale.X;
        var localMouseY = (screenMouse.Y - _trueY) / _trueScale.Y;

        foreach (var kvp in Map.Entities) {
            var entity = kvp.Value;
            if (entity is Player) continue; 
            if (entity.Properties == null || entity.Properties.NoMiniMap || entity.ObjectId == _focus.ObjectId) continue;

            Color fillColor = Color.Transparent;
            if (entity.Properties.IsEnemy) fillColor = new Color(255, 0, 0); 
            else if (entity.Properties.Class is "Portal" or "GuildHallPortal") fillColor = new Color(0, 0, 255); 
            else continue;

            var ratio = (entity.Position - _focus.Position) / _size;
            var pos = center + center * ratio;

            pos.X = Math.Clamp(pos.X, 0, Minimap.MapSize);
            pos.Y = Math.Clamp(pos.Y, 0, Minimap.MapSize);

            AddObject(pos, fillColor);
        }

        foreach (var kvp in Map.Players) {
            var player = kvp.Value;
            if (player.Properties == null || player.Properties.NoMiniMap || player.ObjectId == _focus.ObjectId) continue;

            Color fillColor = new Color(255, 255, 0);
            if (player.HasConditionEffect(ConditionEffects.Paused)) fillColor = new Color(111, 127, 127);
            else if (player.PartyId != 0 && Map.LocalPlayer != null && player.PartyId == Map.LocalPlayer.PartyId) fillColor = new Color(189, 145, 255);
            else if (player.IsFellowGuild) fillColor = new Color(0, 255, 0);
            else if (player.Locked) fillColor = new Color(64, 181, 173);

            var ratio = (player.Position - _focus.Position) / _size;
            var pos = center + center * ratio;
            pos.X = Math.Clamp(pos.X, 0, Minimap.MapSize);
            pos.Y = Math.Clamp(pos.Y, 0, Minimap.MapSize);

            AddObject(pos, fillColor);
            if (inBounds) {
                var dx = pos.X - localMouseX;
                var dy = pos.Y - localMouseY;
                if (dx * dx + dy * dy <= 36f) {
                    HoveredPlayers.Add(player);
                }
            }
        }
    }
}

