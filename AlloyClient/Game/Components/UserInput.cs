using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AlloyClient.Game.Components.Hud;
using AlloyClient.Game.Components.Hud.Chat;
using AlloyClient.Game.Components.Hud.Panels;
using AlloyClient.Game.Components.Options;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Rendering;
using AlloyClient.State;
using AlloyClient.Ui.Character;
using AlloyClient.Ui.Storage;
using AlloyClient.Ui.Wiki;
using AlloyClient.UiLib.Core;
using AlloyClient.Utils;
using Common;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace AlloyClient.Game.Components;

public sealed class UserInput : Sprite
{
    private static bool _windowFocus;
    private static bool _manualFocus = true;
    private static bool _intputDisable;

    private bool _autoFire;

    private bool _mouseDown;
    
    private int _moveDown;
    private int _moveLeft;
    private int _moveRight;
    private int _moveUp;
    
    private float _rotateLeft;
    private float _rotateRight;
    private bool _specialKeyDown;
    
    private bool _hadFocus = true;
    private static readonly HashSet<Scancode> _pressed = new();

    public UserInput()
    {
        AddEventListener(Event.AddedToStage, AddedToStage);
        AddEventListener(Event.RemovedFromStage, RemovedFromStage);
    }

    public static Vector2 MousePosition { get; private set; }

    public static void SetInputDisabled(bool disable)
    {
        _intputDisable = disable;
    }

    public static bool GetInputDisabled()
    {
        return _intputDisable;
    }

    private void AddedToStage()
    {
        Stage.AddEventListener(KeyboardEvent.KeyDown, OnKeyDown);
        Stage.AddEventListener(KeyboardEvent.KeyUp, OnKeyUp);

        Stage.AddEventListener(MouseEvent.RightDown, OnRightDown);
        Stage.AddEventListener(MouseEvent.LeftDown, OnLeftDown);
        Stage.AddEventListener(MouseEvent.LeftUp, OnLeftUp);
        Stage.AddEventListener(MouseEvent.ScrollVertical, OnScroll);
        Stage.AddEventListener(MouseEvent.MiddleClick, OnMiddleClick);
        Stage.AddEventListener(MouseEvent.MouseMove, OnMouseMove);
        AddEventListener(Event.EnterFrame, OnFrameEnter);
    }

    private void RemovedFromStage()
    {
        Stage.RemoveEventListener(KeyboardEvent.KeyDown, OnKeyDown);
        Stage.RemoveEventListener(KeyboardEvent.KeyUp, OnKeyUp);

        Stage.RemoveEventListener(MouseEvent.RightDown, OnRightDown);
        Stage.RemoveEventListener(MouseEvent.LeftDown, OnLeftDown);
        Stage.RemoveEventListener(MouseEvent.LeftUp, OnLeftUp);
        Stage.RemoveEventListener(MouseEvent.ScrollVertical, OnScroll);
        Stage.RemoveEventListener(MouseEvent.MiddleClick, OnMiddleClick);
        Stage.RemoveEventListener(MouseEvent.MouseMove, OnMouseMove);
        RemoveEventListener(Event.EnterFrame, OnFrameEnter);
    }

    public static void SetWindowFocus(bool focus)
    {
        _windowFocus = focus;
    }

    public static void SetManualFocus(bool focus)
    {
        _manualFocus = focus;
    }

    private static bool IsInputDisabled()
    {
        return !(_windowFocus && _manualFocus) || _intputDisable;
    }

    private void OnLeftDown(MouseEvent args)
    {
        if (args.Coords.X > Settings.ScreenWidth - HudView.Width * Stage.ScreenScale.X) return;
        _mouseDown = true;

        if (args.ShiftKey && Map.LocalPlayer != null)
        {
            var pos = Camera.ScreenToWorld(MousePosition);
            Map.LocalPlayer.UseAbility(pos.X, pos.Y, 1);
            return;
        }

        _mouseDown = true;
    }

    private void OnLeftUp()
    {
        _mouseDown = false;
    }

    private void OnRightDown(MouseEvent args)
    {
        if (IsInputDisabled() || Map.LocalPlayer == null) return;
        var pos = Camera.ScreenToWorld(MousePosition);
        Map.LocalPlayer.TryReload(pos.X, pos.Y);
    }

    private void OnMouseMove(MouseEvent args)
    {
        MousePosition = new Vector2(args.Coords.X, args.Coords.Y);
    }

    
    private void ReleaseAllInput()
    {
        if (_specialKeyDown && Map.LocalPlayer != null)
        {
            Map.LocalPlayer.AutoAbilityToggle(false);
            var pos = Camera.ScreenToWorld(MousePosition);
            Map.LocalPlayer.UseAbility(pos.X, pos.Y, Player.UseEnd);
        }

        ClearInput();
    }
    
    public void ClearInput()
    {
        ClearMovement();
        _mouseDown = false;
        _specialKeyDown = false;
    }
    public void ClearMovement()
    {
        _rotateLeft = 0;
        _rotateRight = 0;
        _moveUp = 0;
        _moveDown = 0;
        _moveLeft = 0;
        _moveRight = 0;
        Map.LocalPlayer?.SetRelativeMovement(0, 0, 0);
    }

    private void OnFrameEnter()
    {
        // Clear all key input when player tabs out
        if (!_windowFocus)
        {
            if (_hadFocus)
            {
                _hadFocus = false;
                ReleaseAllInput();
            }
            return;
        }
        _hadFocus = true;
        
        if (IsInputDisabled() || !(_mouseDown || _autoFire) || Map.LocalPlayer == null)
            return;

        var pos = Camera.ScreenToWorld(MousePosition);
        var hitPos = Camera.WorldToDebug2D(new Vector3(pos.X, pos.Y, -0.5f));

        var dX = hitPos.X - Map.LocalPlayer.Position.X;
        var dY = hitPos.Y - Map.LocalPlayer.Position.Y;
        var angle = MathF.Atan2(dY, dX);

        if (Map.LocalPlayer.HasConditionEffect(ConditionEffects.Unstable))
        {
            var variance = 180f;
            if (Random.Shared.NextSingle() * 5 < 1) variance = 720f;
            angle += (float)(Random.Shared.NextDouble() * variance - variance / 2f) / 360f * MathF.PI;
        }

        Map.LocalPlayer.Shoot(angle, Stage.GameTime, false);
    }

    private void SetPlayerMovement()
    {
        if (Map.LocalPlayer == null) return;

        if (IsInputDisabled())
        {
            Map.LocalPlayer.SetRelativeMovement(0, 0, 0);
            return;
        }

        Map.LocalPlayer.SetRelativeMovement(_rotateRight - _rotateLeft, _moveRight - _moveLeft, _moveDown - _moveUp);
    }

    private void OnScroll(MouseEvent args)
    {
        if (IsInputDisabled()) return;
        if (Map.LocalPlayer == null) return;

        if (args.ShiftKey)
        {
            Settings.CameraZoom = Math.Clamp(Settings.CameraZoom += 0.1f * args.Delta, Settings.MinCameraZoom,
                Settings.MaxCameraZoom);
            Logger.Info($"Camera zoom: {Settings.CameraZoom.Value}");
            Camera.SetZoom(Settings.CameraZoom);
        }
        else
        {
            Minimap.OnZoom.Dispatch((int)args.Delta);
        }
    }

    private void OnMiddleClick(MouseEvent args)
    {
        if (IsInputDisabled())
            return;
    }

    private void OnKeyDown(KeyboardEvent args)
    {
        if (IsInputDisabled() || args.Code == Scancode.Unknown)
            return;
        if (Map.LocalPlayer == null)
            return;

        var key = args.Code;

        switch (true)
        {
            case true when Settings.RotateLeft.CheckValue(key):
                _rotateLeft = 1 * Settings.RotateSpeedMult;
                break;
            case true when Settings.RotateRight.CheckValue(key):
                _rotateRight = 1 * Settings.RotateSpeedMult;
                break;
            case true when Settings.MoveUp.CheckValue(key):
                _moveUp = 1;
                break;
            case true when Settings.MoveDown.CheckValue(key):
                _moveDown = 1;
                break;
            case true when Settings.MoveLeft.CheckValue(key):
                _moveLeft = 1;
                break;
            case true when Settings.MoveRight.CheckValue(key):
                _moveRight = 1;
                break;
            case true when Settings.AutoFire.CheckValue(key):
                _autoFire = !_autoFire;
                break;
            case true when Settings.Special.CheckValue(key):
            {
                if (!_specialKeyDown)
                {
                    var pos = Camera.ScreenToWorld(MousePosition);
                    var success = Map.LocalPlayer.UseAbility(pos.X, pos.Y, Player.UseStart);
                    if (success)
                    {
                        _specialKeyDown = true;
                        Map.LocalPlayer.AutoAbilityToggle(true);
                    }
                }
            }
                break;
            case true when Settings.Escape.CheckValue(key):
                Client.Disconnect();
                Map.Reset();
                break;
            case true when Settings.Interact.CheckValue(key):
                if (Client.IsReconnecting)
                    break;
                Panel.OnInteract.Dispatch();
                break;
            case true when Settings.ResetCameraAngle.CheckValue(key):
                Settings.CameraAngle = 0;
                break;
            case true when Settings.CenterPlayerKey.CheckValue(key):
                Settings.CenterPlayer = !Settings.CenterPlayer;
                Camera.Reset();
                break;
            case true when Settings.Options.CheckValue(key):
                ClearMovement();
                OptionsView.Toggle();
                break;
            case true when Settings.Debug.CheckValue(key):
                Render.DebugEnabled = !Render.DebugEnabled;
                break;
            case true when Settings.Debug2.CheckValue(key):
                //var wiki = new WikiWindow();
                //Stage.AddChild(wiki);
                //SetInputDisabled(true);
                break;
            case true when Settings.Debug3.CheckValue(key):
                var storage = new StorageWindow();
                Stage.AddChild(storage);
                SetInputDisabled(true);
                break;
            case true when Settings.Debug4.CheckValue(key):
                break;
            case true when Settings.Debug5.CheckValue(key):
                break;
            case true when Settings.Crouch.CheckValue(key):
                Map.LocalPlayer.LimitSpeed = !Map.LocalPlayer.LimitSpeed;
                //var notification = Map.LocalPlayer.LimitSpeed ? "crouch" : "off";
                NotificationLayer.AddStatusText(Map.LocalPlayer, "crouch", 0xcfe0ff, 1000, 0);
                break;
            case true when Settings.InvOne.CheckValue(key):
                HandleUseItem(4);
                break;
            case true when Settings.InvTwo.CheckValue(key):
                HandleUseItem(5);
                break;
            case true when Settings.InvThree.CheckValue(key):
                HandleUseItem(6);
                break;
            case true when Settings.InvFour.CheckValue(key):
                HandleUseItem(7);
                break;
            case true when Settings.InvFive.CheckValue(key):
                HandleUseItem(8);
                break;
            case true when Settings.InvSix.CheckValue(key):
                HandleUseItem(9);
                break;
            case true when Settings.InvSeven.CheckValue(key):
                HandleUseItem(10);
                break;
            case true when Settings.InvEight.CheckValue(key):
                HandleUseItem(11);
                break;
            case true when Settings.HealthPotion.CheckValue(key):
                UsePotion(true);
                break;
            case true when Settings.MagicPotion.CheckValue(key):
                UsePotion(false);
                break;

            // Chat //
            case true when Settings.Chat.CheckValue(key):
                ClearMovement();
                ChatBox.OnChatOpen.Dispatch("");
                break;
            case true when Settings.ChatHistoryUp.CheckValue(key):
                ChatBox.OnChatHistoryUp.Dispatch();
                break;
            case true when Settings.ChatHistoryDown.CheckValue(key):
                ChatBox.OnChatHistoryDown.Dispatch();
                break;
        }

        SetPlayerMovement();
    }

    private void OnKeyUp(KeyboardEvent args)
    {
        if (IsInputDisabled() || args.Code == Scancode.Unknown) return;
        if (Map.LocalPlayer == null) return;

        var key = args.Code;

        switch (true)
        {
            case true when Settings.RotateLeft.CheckValue(key):
                _rotateLeft = 0;
                break;
            case true when Settings.RotateRight.CheckValue(key):
                _rotateRight = 0;
                break;
            case true when Settings.MoveUp.CheckValue(key):
                _moveUp = 0;
                break;
            case true when Settings.MoveDown.CheckValue(key):
                _moveDown = 0;
                break;
            case true when Settings.MoveLeft.CheckValue(key):
                _moveLeft = 0;
                break;
            case true when Settings.MoveRight.CheckValue(key):
                _moveRight = 0;
                break;
            case true when Settings.ChatCommand.CheckValue(key):
                ClearMovement();
                ChatBox.OnChatOpen.Dispatch("/");
                break;
            case true when Settings.TellKey.CheckValue(key):
                ClearMovement();
                ChatBox.OnChatOpen.Dispatch("/tell ");
                break;
            case true when Settings.GuildChat.CheckValue(key):
                ClearMovement();
                ChatBox.OnChatOpen.Dispatch("/g ");
                break;
            case true when Settings.PartyChat.CheckValue(key):
                ClearMovement();
                ChatBox.OnChatOpen.Dispatch("/npp ");
                break;
            case true when Settings.Special.CheckValue(key):
            {
                if (_specialKeyDown)
                {
                    _specialKeyDown = false;
                    Map.LocalPlayer.AutoAbilityToggle(false);
                    var pos = Camera.ScreenToWorld(MousePosition);
                    Map.LocalPlayer.UseAbility(pos.X, pos.Y, Player.UseEnd);
                }
            }
                break;
            case true when Settings.Crouch.CheckValue(key):
                Map.LocalPlayer.LimitSpeed = !Map.LocalPlayer.LimitSpeed;
                NotificationLayer.AddStatusText(Map.LocalPlayer, "off", 0xcfe0ff, 1000, 0);
                break;
        }

        SetPlayerMovement();
    }

    private void HandleUseItem(byte slotId)
    {
        var player = Map.LocalPlayer;
        if (player == null || player.Equipment.Length <= slotId) return;

        var item = player.Equipment[slotId];
        if (item == null || item.ObjectType == 0) return;

        if (item.ObjectType == ItemConstants.HealthPotion || item.ObjectType == ItemConstants.MagicPotion)
        {
            var isHp = item.ObjectType == ItemConstants.HealthPotion;
            var currentCount = isHp ? player.HealthStackCount : player.MagicStackCount;
            if (currentCount < 6)
            {
                var targetSlot = isHp ? ItemConstants.HpPotionSlot : ItemConstants.MpPotionSlot;

                var pSwap = InvSwap.CreatePacket();
                pSwap.Time = (int)Map.LastGameTime.TotalMs;
                pSwap.Position = new Position { X = player.Position.X, Y = player.Position.Y };
                pSwap.SlotObj1 = new ObjectSlot
                    { ObjectId = player.ObjectId, SlotId = slotId, ObjectType = item.ObjectType };
                pSwap.SlotObj2 = new ObjectSlot { ObjectId = player.ObjectId, SlotId = targetSlot, ObjectType = 0 };
                Client.QueuePacket(pSwap);

                player.Equipment[slotId] = null;
                if (isHp) player.HealthStackCount++;
                else player.MagicStackCount++;
                player.InventoryUpdate.Dispatch(slotId);
                return;
            }
        }

        if (item.Consumable)
        {
            SendUseItem(slotId, item.ObjectType);
            return;
        }

        var equipSlot = -1;
        for (var i = 0; i < 4; i++)
        {
            if (i == 0 && item.SlotType == 0)
            {
                equipSlot = i;
                break;
            }

            if (player.Properties.SlotTypes.Count > i && player.Properties.SlotTypes[i] == item.SlotType)
            {
                equipSlot = i;
                break;
            }
        }

        if (equipSlot != -1)
        {
            var targetItem = player.Equipment[equipSlot];
            var targetObjType = targetItem?.ObjectType ?? 0;

            var swap = InvSwap.CreatePacket();
            swap.Time = (int)Map.LastGameTime.TotalMs;
            swap.Position = new Position { X = player.Position.X, Y = player.Position.Y };
            swap.SlotObj1 = new ObjectSlot
                { ObjectId = player.ObjectId, SlotId = slotId, ObjectType = item.ObjectType };
            swap.SlotObj2 = new ObjectSlot
                { ObjectId = player.ObjectId, SlotId = (byte)equipSlot, ObjectType = targetObjType };
            Client.QueuePacket(swap);

            player.Equipment[slotId] = targetItem;
            player.Equipment[equipSlot] = item;
            player.InventoryUpdate.Dispatch(slotId);
            player.InventoryUpdate.Dispatch(equipSlot);
        }
        else
        {
            SendUseItem(slotId, item.ObjectType);
        }
    }

    private void UsePotion(bool isHp)
    {
        var player = Map.LocalPlayer;
        if (player == null) return;

        var count = isHp ? player.HealthStackCount : player.MagicStackCount;
        if (count <= 0) return;

        var slotId = isHp ? ItemConstants.HpPotionSlot : ItemConstants.MpPotionSlot;
        var objType = isHp ? ItemConstants.HealthPotion : ItemConstants.MagicPotion;

        SendUseItem(slotId, objType);
        if (isHp) player.HealthStackCount--;
        else player.MagicStackCount--;
    }

    private void SendUseItem(byte slotId, ushort objectType)
    {
        var player = Map.LocalPlayer;
        var packet = UseItem.CreatePacket();

        packet.Time = (int)Map.LastGameTime.TotalMs;
        packet.SlotObject.ObjectId = player.ObjectId;
        packet.SlotObject.SlotId = slotId;
        packet.SlotObject.ObjectType = objectType;
        packet.ItemUsePos.X = player.Position.X;
        packet.ItemUsePos.Y = player.Position.Y;
        packet.UseType = 1;
        packet.ProjId = player.ReadServerId();

        Client.QueuePacket(packet);
    }
}