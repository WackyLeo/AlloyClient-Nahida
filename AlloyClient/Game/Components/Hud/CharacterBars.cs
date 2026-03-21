using AlloyClient.UiLib.Core;

namespace AlloyClient.Game.Components.Hud;

public class CharacterBars : Sprite {

    public const int TotalHeight = 85;
    private const int BarWidth = 220;
    private const int RestorationFixedHeight = 7;

    private readonly StatusBar _expBar;
    private readonly StatusBar _fameBar;
    private readonly StatusBar _hpBar;
    private readonly StatusBar _shieldBar;
    private readonly StatusBar _restorationBar;
    private readonly StatusBar _mpBar;

    private readonly StatusBar[] _allBars;

    public CharacterBars() {
        _expBar = new StatusBar(BarWidth, 1, 5931045,  5526612, 0x676767, "Lvl X");
        _fameBar = new StatusBar(BarWidth, 1, 14835456, 5526612, 0x676767, "Fame");
        _hpBar = new StatusBar(BarWidth, 1, 14693428, 5526612, 0x676767, "HP");
        _shieldBar = new StatusBar(BarWidth, 1, 0x00C9DD, 5526612, 0x676767, "Shield");
        _restorationBar = new StatusBar(BarWidth, RestorationFixedHeight, 0x800000, 5526612, 0x676767, "");
        _mpBar = new StatusBar(BarWidth, 1, 6325472,  5526612, 0x676767, "MP");

        _allBars = [_expBar, _fameBar, _hpBar, _shieldBar, _restorationBar, _mpBar];

        foreach (var bar in _allBars)
            bar.Visible = false;

        AddChild(_hpBar);
        AddChild(_shieldBar);
        AddChild(_restorationBar);
        AddChild(_mpBar);
        AddChild(_fameBar);
        AddChild(_expBar);
    }

    public void Update() {
        if (Map.LocalPlayer == null) return;
        var player = Map.LocalPlayer;

        var lvlText = "Lvl " + player.Level;
        if (lvlText != _expBar.labelString)
            _expBar.UpdateLabel(lvlText);

        if (!_fameBar.Visible && player.Level == 20) {
            _expBar.Visible  = false;
            _fameBar.Visible = true;
        }

        var active = new System.Collections.Generic.List<StatusBar>();
        if (player.Level != 20)
            active.Add(_expBar);
        else
            active.Add(_fameBar);

        var showShield = player.MaxShield > 0;
        if (showShield) {
            active.Add(_shieldBar);
            active.Add(_hpBar);
        } else {
            active.Add(_hpBar);
        }
        
        active.Add(_restorationBar);
        active.Add(_mpBar);
        foreach (var bar in _allBars)
            bar.Visible = false;
        foreach (var bar in active)
            bar.Visible = true;

        if (_expBar.Visible)  _expBar.Update(player.Experience, player.NextLevelExp);
        if (_fameBar.Visible) _fameBar.Update(player.CurrentFame, player.FameGoal);
        if (_shieldBar.Visible) _shieldBar.Update(player.Shield, player.MaxShield);
        _hpBar.Update(player.Hp, player.MaxHp, player.MaxHpBoost, 250, player.Level);
        if (_restorationBar.Visible) {
            _restorationBar.Update(player.Restoration, player.MaxHp);
            _restorationBar.drawLabel = false;
        }
        _mpBar.Update(player.Mp, player.MaxMp, player.MaxMpBoost, 250, player.Level);
        Layout(active);
    }

    private void Layout(System.Collections.Generic.List<StatusBar> active) {
        var count = active.Count;
        if (count == 0) return;

        var hasResto = active.Contains(_restorationBar);
        var layoutBars = active.FindAll(b => b != _restorationBar);

        var totalGap = 0;
        for (var i = 1; i < layoutBars.Count; i++)
            totalGap += 3;

        var available = TotalHeight - totalGap;
        if (hasResto) available -= RestorationFixedHeight;

        var resizableCount = layoutBars.Count;
        var baseHeight = resizableCount > 0 ? available / resizableCount : 0;
        var remainder  = resizableCount > 0 ? available % resizableCount : 0;

        int currentY = 0;
        for (var i = 0; i < layoutBars.Count; i++) {
            var bar = layoutBars[i];
            if (i > 0) currentY += 3;

            var barH = System.Math.Max(1, baseHeight + (remainder-- > 0 ? 1 : 0));
            bar.Y = currentY;
            bar.SetSize(BarWidth, barH);
            currentY += barH;

            if (hasResto && bar == _hpBar) {
                _restorationBar.Y = _hpBar.Y + barH - 3; 
                _restorationBar.SetSize(BarWidth, RestorationFixedHeight);
                currentY += 3;
            }
        }
    }
}