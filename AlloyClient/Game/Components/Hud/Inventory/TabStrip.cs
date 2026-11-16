using System;
using System.Collections.Generic;
using AlloyClient.Game.Objects;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Signals;
using AlloyClient.Utils;

namespace AlloyClient.Game.Components.Hud.Inventory;

public sealed class TabStrip : Sprite 
{
    public const uint BackgroundColor = 7039594;
    public const uint SelectedTabColor = 2368034;
    
    private readonly Player _owner;
    private InventoryGrid _inventoryTab;
    private IconButton _inventoryTabButton;
    private StatsPanel _statsTab;
    private IconButton _statsTabButton;
    private CutEdgeRect _cosmeticTab;
    private IconButton _cosmeticTabButton;
    private CutEdgeRect _adminTab;
    private IconButton _adminTabButton;
    
    private CutEdgeRect _invTabButtonBg;
    private IconButton _invTabButton;
    
    public int CurrentTabIndex;
    
    private readonly Dictionary<int, Tab> _tabs = new();
    private readonly Dictionary<int, TabTypes> _tabsTypes = new() 
    {
        { (int)TabTypes.Inventory, TabTypes.Inventory }, //Key is 1
        { (int)TabTypes.StatsView, TabTypes.StatsView } //Key is 2
    };
    
    public static readonly Signal OnSwitchTabsDown = new();
    public static readonly Signal OnBackpackAdded = new();

    public TabStrip(Player owner) 
    {
        _owner = owner;

        CurrentTabIndex = (int)TabTypes.Inventory;
        
        Initialize();
        
        AddEventListener(Event.AddedToStage, AddHandlers);
        AddEventListener(Event.RemovedFromStage, RemoveHandlers);
    }
    
    private void AddHandlers() 
    {
        OnSwitchTabsDown.Add(SwitchTabs);
        OnBackpackAdded.Add(Initialize);
    }

    private void RemoveHandlers() 
    {
        OnSwitchTabsDown.Remove(SwitchTabs);
        OnBackpackAdded.Remove(Initialize);
    }

    private void Initialize() 
    {
        var X = 6; // For initial tab button offset

        //AddTab(TabTypes.Cosmetics);

        /*if (false)
         {
            AddTab(TabTypes.Admin);
        }*/
        RemoveChildren(); //Remove All Panels
        _owner.InventoryUpdate.RemoveAll(); //Remove InventoryUpdate
        InitializeTabs(); // Initialize tabs first to add the tab buttons to them as children
        
        foreach (var tabType in _tabsTypes) // Add the tab button to each tab
        {
            _invTabButtonBg = new CutEdgeRect(new CutEdgeConfig 
            {
                Width = 34, 
                Height = 24, 
                CutX = 5, 
                CutY = 5, 
                Cuts = CutEdges.Top,
                Color = CurrentTabIndex == tabType.Key ? SelectedTabColor : BackgroundColor,
                Anchor = UiAnchor.LeftBottom
            });
            _invTabButtonBg.X = X;
            _invTabButtonBg.Y = 0;
            
            AddChild(_invTabButtonBg);

            _invTabButton = new IconButton(new IconButtonConfig 
            {
                Texture = TextureHelper.FromGameAtlas("lofiInterfaceBig", 23 + tabType.Key, false),
                Alpha = 1,
                X = _invTabButtonBg.Width / 2,
                Y = _invTabButtonBg.Height / 2,
                Width = 24,
                Height = 24,
                OnClick = () => OnTabSelected(tabType.Value),
                Anchor = UiAnchor.Middle
            });

            X = _invTabButtonBg.X + 40;
            
            _invTabButtonBg.AddChild(_invTabButton);
            
            switch (tabType.Value) 
            {
                case TabTypes.Inventory:
                    _tabs.Add((int)tabType.Value, new Tab(_inventoryTab, _invTabButtonBg));
                    break;
                case TabTypes.StatsView:
                    _tabs.Add((int)tabType.Value, new Tab(_statsTab, _invTabButtonBg));
                    break;
                case TabTypes.Cosmetics:
                    break;
                case TabTypes.Admin:
                    break;
            }
        }
        
        SetTabVisibility(CurrentTabIndex);
    }

    private void InitializeTabs() 
    {
        _inventoryTab = new InventoryGrid(_owner, 4, false, GetBonusSlots());
        _inventoryTab.Visible = false;
        AddChild(_inventoryTab);

        _statsTab = new StatsPanel();
        _statsTab.Visible = false;
        AddChild(_statsTab);
    }
    
    // Used for adding extra tabs outside the basic inventory and stats view
    private void AddTab(TabTypes tab)
    {
        _tabsTypes.Add((int)tab, tab);
    }

    // Sets the tab of the given index to visible, and the previously visible tab to not visible
    private void SetTabVisibility(int index)
    {
        var previousTab = _tabs[CurrentTabIndex];
        previousTab.SetVisible(false);
        CurrentTabIndex = index;
        _tabs[CurrentTabIndex].SetVisible(true);
        //Console.WriteLine(previousTab.GetPanel());
    }

    private void OnTabSelected(TabTypes tabType) 
    {
        Console.WriteLine(_tabs.Count);
        SetTabVisibility((int)tabType);
    }

    // Increment current tab index for tab switch hotkey
    private void SwitchTabs()
    {
        if (CurrentTabIndex >= _tabsTypes.Count)
            SetTabVisibility(1);
        else
            SetTabVisibility(CurrentTabIndex + 1);
    }
    
    // For updating stat values in stats view
    // Make an update function in StatsPanel so it doesn't have to make a whole one each frame
    public void UpdateStatsPanel()
    {
        if (CurrentTabIndex == (int)TabTypes.StatsView) 
        {
            RemoveChild(_statsTab);
            _statsTab = new StatsPanel();
            _tabs[(int)TabTypes.StatsView].SetPanel(_statsTab);
            AddChild(_statsTab);
        }
    }

    // For checking all sources of extra slots for player inventory
    // Currently only has backpack, but this is where to add more inv extender checks
    public int GetBonusSlots()
    {
        var slotTotal = 0;
        
        foreach (var expander in _owner.InventoryExpanders)
        {
            slotTotal += expander.Value;
        }

        return slotTotal;
    }

    private enum TabTypes 
    {
        None,
        Inventory,
        StatsView,
        Backpack,
        Cosmetics,
        Admin
    }
}

public class StatsPanel : Sprite 
{
    public StatsPanel() 
    {
        var p = Map.LocalPlayer;
        var y = 150 / 2 - 16 - 10; //Height - Size - Spacing 
        var offset = 10;
        var bg = new CutEdgeRect(new CutEdgeConfig
        {
            Width = 224, 
            Height = 150, 
            CutX = 6, 
            CutY = 6, 
            Cuts = CutEdges.All, 
            Color = 0x242222
        });
        AddChild(bg);

        string[] IndexName = { "ATK", "DEF", "SPD", "DEX", "VIT", "WIS" };
        int[] IndexValue = { p.Attack, p.Defense, p.Speed, p.Dexterity, p.Vitality, p.Wisdom };
        int[] IndexBoostValue = { p.AttackBoost, p.DefenseBoost, p.SpeedBoost, p.DexterityBoost, p.VitalityBoost, p.WisdomBoost };

        for (var i = 0; i < IndexValue.Length; i++) 
        {
            var even = i == 0 || i == 2 || i == 4;
            var extraInfo = IndexBoostValue[i] > 0 || IndexBoostValue[i] < 0;
            uint boostColor = 0xFFFF8F;

            if (extraInfo) 
            {
                if (IndexBoostValue[i] > 0) 
                    boostColor = 0x5EB531;
                
                else if (IndexBoostValue[i] < 0) 
                    boostColor = 0xFF3838;
            }

            var StatName = new SimpleText(new TextConfig 
            {
                Text = IndexName[i],
                FontSize = 14,
                FontType = FontType.Normal,
                X = even ? offset : Width / 2,
                Y = y,
                OutlineThickness = 0,
                Color = 0xFFFFFF,
                OutlineColor = 0xFFFFFF,
                Glow = 0,
                Anchor = UiAnchor.MiddleLeft
            });
            
            AddChild(StatName);
            
            var StatValue = new SimpleText(new TextConfig 
            {
                Text = IndexValue[i].ToString(),
                FontSize = 14,
                FontType = FontType.Bold,
                X = (even ? offset : Width / 2) + StatName.Width + 5, //kinda gross but its needed
                Y = y,
                OutlineThickness = 0,
                Color = 16777103,
                OutlineColor = 0xFFFFFF,
                Glow = 0,
                Anchor = UiAnchor.MiddleLeft
            });

            AddChild(StatValue);

            if (extraInfo) 
            {
                var StatBoostValue = new SimpleText(new TextConfig 
                {
                    Text = $"({(IndexBoostValue[i] > 0 ? "+" : "")}{IndexBoostValue[i]})",
                    FontSize = 14,
                    FontType = FontType.Bold,
                    X = ((even ? offset : Width / 2) + StatName.Width + 5) + StatValue.Width + 5, //kinda grosser but its needed
                    Y = y,
                    OutlineThickness = 0,
                    Color = boostColor,
                    OutlineColor = 0xFFFFFF,
                    Glow = 0,
                    Anchor = UiAnchor.MiddleLeft
                });

                AddChild(StatBoostValue);
            }

            y += even ? 0 : StatName.Height + 10;
        }
    }
}

public class Tab(Sprite tabPanel, CutEdgeRect tabButtonBackground ) 
{
    private Sprite _tabPanel = tabPanel;
    private CutEdgeRect _tabButtonBackground = tabButtonBackground;

    public void SetVisible(bool visible)
    {
        _tabPanel.Visible = visible;
        var color = visible ? TabStrip.SelectedTabColor : TabStrip.BackgroundColor;
        _tabButtonBackground.SetColor(color);
    }

    public Sprite GetPanel()
    {
        return _tabPanel;
    }

    public void SetPanel(Sprite newPanel)
    {
        _tabPanel = newPanel;
    }
}