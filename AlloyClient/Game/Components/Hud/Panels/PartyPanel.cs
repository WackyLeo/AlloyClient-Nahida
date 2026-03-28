using System;
using AlloyClient.Display;
using AlloyClient.Game.Objects;
using AlloyClient.Ui.Components.Tooltips;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Game.Components.Hud.Panels;

public sealed class PartyPanel : Panel {
    private const int NumMembers = 10;
    private const int SlotWidth = 100;
    private const int SlotHeight = 24;
    private const int Cols = 2;

    private static readonly MemberSlot[] _slots = new MemberSlot[NumMembers];

    public PartyPanel() {
        for (var i = 0; i < NumMembers; i++) {
            var col = i % Cols;
            var row = i / Cols;
            var slot = new MemberSlot();
            slot.Root.X = col * SlotWidth;
            slot.Root.Y = row * SlotHeight;
            _slots[i] = slot;
            AddChild(slot.Root);
        }
    }

    public void Refresh() {
        var members = PartyData.PartyMembers;
        for (var i = 0; i < NumMembers; i++) {
            Player player = null;
            if (i < members.Count && members[i] != null) player = members[i].Player;

            _slots[i].SetPlayer(player);
            _slots[i].Refresh();
        }
    }

    private sealed class MemberSlot {
        private const int PortraitSize = 24;
        private const int BarHeight = 6;
        private const int BarWidth = 66;

        private readonly SimpleText _nameText;

        private readonly ColorRect _hpBarBg;
        private readonly ColorRect _hpBar;
        private readonly ObjectRect _portrait;

        public readonly Sprite Root;
        private double _lowHealthStart;
        private Player _player;
        private PlayerToolTip _tooltip;

        public MemberSlot() {
            Root = new Sprite();

            Root.MouseEnabled = true;
            Root.AddEventListener(MouseEvent.MouseOver, OnMouseOver);
            Root.AddEventListener(MouseEvent.MouseOut, OnMouseOut);
            Root.AddEventListener(MouseEvent.LeftClick, OnClick);
            
            _portrait = new ObjectRect(new ObjectRectConfig {
                Width = PortraitSize,
                Height = PortraitSize,
                OutlineEnabled = true,
                GlowEnabled = true
            });
            Root.AddChild(_portrait);

            _nameText = new SimpleText(new TextConfig {
                FontSize = 12,
                Color = 0xFFFFFFFFu,
                OutlineColor = 0xFF000000u,
                OutlineThickness = 0.5f,
                FontType = FontType.Bold
            }) { X = PortraitSize + 2, Y = 2 };
            Root.AddChild(_nameText);

            _hpBarBg = new ColorRect(new ColorRectConfig { Width = BarWidth, Height = BarHeight, Color = 0xFF222222u });
            _hpBarBg.X = PortraitSize + 2;
            _hpBarBg.Y = SlotHeight - BarHeight - 4;
            Root.AddChild(_hpBarBg);

            _hpBar = new ColorRect(new ColorRectConfig { Width = BarWidth, Height = BarHeight, Color = 0xFF00FF00u });
            _hpBar.X = PortraitSize + 2;
            _hpBar.Y = SlotHeight - BarHeight - 4;
            Root.AddChild(_hpBar);

            Root.Visible = false;
        }

        private void OnClick() {
            if (_player == null) return;
            OnMouseOut(); 

            var menu = new PlayerMenuToolTip(_player);
            var mousePos = AlloyClient.UiLib.Input.MouseInput.GetMousePosition();
    
            menu.X = mousePos.X - menu.ToolWidth;
            menu.Y = mousePos.Y - menu.ToolHeight;

            menu.SpawnMouse = mousePos;
            menu.X = mousePos.X - (int)(menu.ToolWidth * Stage.ScreenScale.X);
            menu.Y = mousePos.Y - (int)(menu.ToolHeight * Stage.ScreenScale.Y);
            Map.GameSprite.AddChild(menu);
        }
        
        private void OnMouseOver() {
            if (_player == null) return;
            _tooltip = new PlayerToolTip(_player);
            TooltipManager.AddTooltip(_tooltip);
        }

        private void OnMouseOut() {
            if (_tooltip != null) {
                TooltipManager.RemoveTooltip(_tooltip);
                _tooltip = null;
            }
        }

        public void SetPlayer(Player player) {
            _player = player;
            Root.Visible = player != null;
        }

        public void Refresh() {
            if (_player == null) {
                Root.Visible = false;
                if (_tooltip != null) OnMouseOut();
                return;
            }

            Root.Visible = true;
            _nameText.SetText(_player.Name ?? "");
            _portrait.ChangeTexture(TextureHelper.Create(_player.Texture, TextureType.GameAtlas));

            if (_player.Hp < _player.MaxHp * 0.2f) {
                if (_lowHealthStart == 0) _lowHealthStart = Stage.GameTime.TotalMs;
                var intensity = (float)(Math.Abs(Math.Sin(_lowHealthStart / 200.0)) * 10) / 10f;
                var offset = (int)(intensity * 128);
                Root.Color = new Engine.Common.Color((uint)((255 << 24) | (Math.Min(255, 255 + offset) << 16) | (Math.Max(0, 255 - offset) << 8) | Math.Max(0, 255 - offset)));
            } else {
                _lowHealthStart = 0;
                Root.Color = Engine.Common.Color.White;
            }

            float hpRatio = _player.MaxHp > 0 ? Math.Clamp((float)_player.Hp / _player.MaxHp, 0f, 1f) : 0f;
            var barColor = hpRatio > 0.5f
                ? LerpColor(0xFF00FF00u, 0xFFFFFF00u, 1f - (hpRatio - 0.5f) * 2f)
                : LerpColor(0xFFFFFF00u, 0xFFFF0000u, 1f - hpRatio * 2f);

            _hpBar.Resize((int)Math.Max(1, BarWidth * hpRatio), BarHeight);
            _hpBar.SetColor(barColor);
        }
    }
}