using System;
using System.Collections.Generic;
using AlloyClient.Game.Objects;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Game.Components.Hud.Inventory;

public sealed class TabStrip : Sprite {

    private readonly Entity _owner;
    private readonly CutEdgeRect AdminTab;
    private readonly IconButton AdminTabButton;
    private readonly uint BackgroundColor = 7039594;
    private readonly CutEdgeRect BackpackTab;
    private readonly IconButton BackpackTabButton;
    private readonly CutEdgeRect CosmeticTab;
    private readonly IconButton CosmeticTabButton;

    private readonly CutEdgeRect InventoryTab;

    private readonly IconButton InventoryTabButton;
    private readonly CutEdgeRect StatsViewTab;
    private readonly IconButton StatsViewTabButton;

    private readonly uint TabColor = 2368034;
    private InventoryGrid _backpackPanel;

    private InventoryGrid _inventoryGrid;
    private StatsPanel _statsPanel;

    public int currentTabIndex = 1;

    private CutEdgeRect invTab;
    private IconButton invTabButton;

    private readonly Dictionary<int, TabTypes> Tabs = new() {
        { (int)TabTypes.Inventory, TabTypes.Inventory }, //Key is 1
        { (int)TabTypes.StatsView, TabTypes.StatsView } //Key is 2
    };

    public TabStrip(Entity owner) {
        _owner = owner;

        currentTabIndex = (int)TabTypes.Inventory;

        Update();
    }

    private void Update() {
        var Y = -24;
        var X = 6;

        if (Map.LocalPlayer.HasBackPack && !Tabs.ContainsKey(3)) {
            AddTab(TabTypes.Backpack);
        }

        //AddTab(TabTypes.Cosmetics);

        /*if (false) {
            AddTab(TabTypes.Admin);
        }*/
        RemoveChildren(); //Remove All Panels
        _owner.InventoryUpdate.RemoveAll(); //Remove InventoryUpdate

        foreach (var tab in Tabs) {
            switch (tab.Value) {
                case TabTypes.Inventory:
                    invTab = InventoryTab;
                    invTabButton = InventoryTabButton;
                    break;
                case TabTypes.Backpack:
                    invTab = BackpackTab;
                    invTabButton = BackpackTabButton;
                    break;
                case TabTypes.StatsView:
                    invTab = StatsViewTab;
                    invTabButton = StatsViewTabButton;
                    break;
                case TabTypes.Cosmetics:
                    break;
                case TabTypes.Admin:
                    break;
            }

            invTab = new CutEdgeRect(new CutEdgeConfig {
                Width = 34, Height = 24, CutX = 5, CutY = 5, Cuts = CutEdges.Top,
                Color = currentTabIndex == tab.Key ? TabColor : BackgroundColor
            });
            invTab.X = X;
            invTab.Y = Y;

            AddChild(invTab);

            invTabButton = new IconButton(new IconButtonConfig {
                Texture = TextureHelper.FromGameAtlas("lofiInterfaceBig", 23 + tab.Key, false),
                Alpha = 1,
                X = X + 6,
                Y = Y,
                Width = 24,
                Height = 24,
                OnClick = () => OnTabSelected(tab.Value)
            });

            AddChild(invTabButton);

            X = invTab.X + 40;
        }

        InitializeTabs();
    }
    
    public void UpdateStatsPanel() {
        if (currentTabIndex == (int)TabTypes.StatsView) {
            RemoveChild(_statsPanel);
            _statsPanel = new StatsPanel();
            AddChild(_statsPanel);
        }
    }

    private void AddTab(TabTypes tab) {
        Tabs.Add((int)tab, tab);
    }

    private void InitializeTabs() {
        _inventoryGrid = new InventoryGrid(Map.LocalPlayer, 4);
        AddChild(_inventoryGrid);

        _backpackPanel = new InventoryGrid(Map.LocalPlayer, 12, false, true);
        AddChild(_backpackPanel);

        _statsPanel = new StatsPanel();
        AddChild(_statsPanel);


        SetTabVisibility(currentTabIndex);
    }

    private void SetTabVisibility(int tabType) {
        _inventoryGrid.Visible = tabType == (int)TabTypes.Inventory;
        _backpackPanel.Visible = tabType == (int)TabTypes.Backpack;
        _statsPanel.Visible = tabType == (int)TabTypes.StatsView;
    }

    private void OnTabSelected(TabTypes tabType) {
        currentTabIndex = (int)tabType;
        Update();
    }

    private enum TabTypes {
        None,
        Inventory,
        StatsView,
        Backpack,
        Cosmetics,
        Admin
    }
}

public class StatsPanel : Sprite {
    public StatsPanel() {
        var p = Map.LocalPlayer;
        var y = 150 / 2 - 16 - 10; //Height - Size - Spacing 
        var offset = 10;
        var bg = new CutEdgeRect(new CutEdgeConfig
            { Width = 224, Height = 150, CutX = 6, CutY = 6, Cuts = CutEdges.All, Color = 0x242222 });
        AddChild(bg);

        string[] IndexName = { "ATK", "DEF", "SPD", "DEX", "VIT", "WIS" };
        int[] IndexValue = { p.Attack, p.Defense, p.Speed, p.Dexterity, p.Vitality, p.Wisdom };
        int[] IndexBoostValue = { p.AttackBoost, p.DefenseBoost, p.SpeedBoost, p.DexterityBoost, p.VitalityBoost, p.WisdomBoost };

        for (var i = 0; i < IndexValue.Length; i++) {
            var even = i == 0 || i == 2 || i == 4;
            var extraInfo = IndexBoostValue[i] > 0 || IndexBoostValue[i] < 0;
            uint boostColor = 0xFFFF8F;

            if (extraInfo) {
                if (IndexBoostValue[i] > 0) 
                    boostColor = 0x5EB531;
                
                else if (IndexBoostValue[i] < 0) 
                    boostColor = 0xFF3838;
            }

            var StatName = new SimpleText(new TextConfig {
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
            
            var StatValue = new SimpleText(new TextConfig {
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

            if (extraInfo) {
                var StatBoostValue = new SimpleText(new TextConfig {
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