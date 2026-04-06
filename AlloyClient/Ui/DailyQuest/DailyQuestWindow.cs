using System;
using System.Collections.Generic;
using AlloyClient.Game;
using AlloyClient.Game.Components;
using AlloyClient.Game.Components.Hud;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.State;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;

namespace AlloyClient.Ui.DailyQuest;

public class DailyQuestWindow : Sprite {
    private const int BgWidth = 860;
    private const int BgHeight = 460;
    private const int Pad = 20;
    private const int TabIslandH = 35;
    private const int TabIslandGap = 18;

    private readonly SdfRect _background;
    private readonly Container _cardsContainer;
    private readonly Container _screenContainer;
    private readonly SdfRect _tabIsland;

    public DailyQuestWindow() {
        _screenContainer = new Container();
        AddChild(_screenContainer);

        _tabIsland = new SdfRect(new SdfRectConfig {
            Width = BgWidth, Height = TabIslandH, Cut = 8,
            Color = 0xFF363636u, OutlineColor = 0xFF676767u, OutlineSize = 5f,
            ShadowRadius = 3f, ShadowAlpha = 0.3f
        });
        _tabIsland.SetAnchor(UiAnchor.LeftTop);
        _tabIsland.X = Pad;
        _tabIsland.Y = Pad;
        _screenContainer.AddChildAt(_tabIsland, 0);

        var title = new SimpleText(new TextConfig {
            FontSize = 13, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        title.SetText("The Tinkerer");
        title.X = BgWidth / 2;
        title.Y = TabIslandH / 2;
        _tabIsland.AddChild(title);

        var closeBtn = new SdfRect(new SdfRectConfig {
            Width = 21, Height = 21, Cut = 4,
            Color = 0xFF555555u, OutlineColor = 0xFF888888u, OutlineSize = 2f, ShadowRadius = 0f
        });
        closeBtn.SetAnchor(UiAnchor.RightTop);
        closeBtn.X = BgWidth - 6;
        closeBtn.Y = (_tabIsland.Height - 21) / 2;
        closeBtn.MouseEnabled = true;
        closeBtn.AddEventListener(MouseEvent.LeftClick, OnCloseClicked);
        _tabIsland.AddChild(closeBtn);

        var closeX = new SimpleText(new TextConfig {
            FontSize = 11, Color = 0xFFFFFFFFu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        closeX.SetText("X");
        closeX.X = 21 / 2;
        closeX.Y = 21 / 2;
        closeBtn.AddChild(closeX);

        _background = new SdfRect(new SdfRectConfig {
            Width = BgWidth, Height = BgHeight, Cut = 8,
            Color = 0xFF363636u, OutlineColor = 0xFF676767u, OutlineSize = 5f,
            ShadowRadius = 3f, ShadowAlpha = 0.3f
        });
        _background.SetAnchor(UiAnchor.LeftTop);
        _background.X = Pad;
        _background.Y = Pad + TabIslandH + TabIslandGap;
        _screenContainer.AddChildAt(_background, 1);

        _cardsContainer = new Container();
        _background.AddChild(_cardsContainer);

        var refreshText = new SimpleText(new TextConfig {
            FontSize = 10, Color = 0xFFAAAAAAu, Anchor = UiAnchor.LeftTop
        });
        refreshText.SetText("Quests refresh every completion");
        refreshText.X = Pad;
        refreshText.Y = _background.Height - Pad;
        _background.AddChild(refreshText);

        SetPosition(Settings.ScreenWidth, Settings.ScreenHeight);
        AddEventListener(Event.AddedToStage, OnStageEnter);
        AddEventListener(Event.RemovedFromStage, OnStageExit);
    }

    public void LoadQuests(List<QuestData> quests) {
        while (_cardsContainer.NumChildren > 0) _cardsContainer.RemoveChildAt(0);

        var sideCardW = 240;
        var sideCardH = 340;
        var midCardW = 290;
        var midCardH = 390;
        var spacing = 20;

        var totalCardsWidth = sideCardW * 2 + midCardW + spacing * 2;
        var currentX = (_background.Width - totalCardsWidth) / 2;
        var baseStartY = 30;

        for (var i = 0; i < Math.Min(3, quests.Count); i++) {
            var isMiddle = i == 1;
            var cW = isMiddle ? midCardW : sideCardW;
            var cH = isMiddle ? midCardH : sideCardH;
            var cY = baseStartY + (isMiddle ? 0 : (midCardH - sideCardH) / 2);

            var index = i == 0 ? 1 : i == 1 ? 0 : 2;
            BuildQuestCard(quests[index], currentX, cY, cW, cH, isMiddle);
            currentX += cW + spacing;
        }
    }

    private void BuildQuestCard(QuestData quest, int x, int y, int cW, int cH, bool isMiddle) {
        var cardBg = new SdfRect(new SdfRectConfig {
            Width = cW, Height = cH, Cut = 8,
            Color = 0xFF151515u, OutlineColor = 0xFF121212u, OutlineSize = 3f,
            ShadowRadius = 2f, ShadowAlpha = 0.3f
        });
        cardBg.X = x;
        cardBg.Y = y;
        _cardsContainer.AddChild(cardBg);

        var cardTitle = new SimpleText(new TextConfig {
            Text = quest.Name, FontSize = isMiddle ? 18 : 16, Color = 0xFFFFCC00u, FontType = FontType.Bold,
            Anchor = UiAnchor.MiddleTop
        });
        cardTitle.X = cW / 2;
        cardTitle.Y = 15;
        cardBg.AddChild(cardTitle);

        var hasAllItems = CheckInventory(quest.Requirements, out var slotsToConsume, out var fulfilled);
        var selectedReward = quest.ItemOfChoice ? -1 : 0;

        var reqY = 55;
        DrawSlots(cardBg, reqY, quest.Requirements, cW, false, -1, null, fulfilled);

        var dividerY = cH / 2 + 10;
        var line = new DiamondDivider(new DiamondDividerConfig {
            LineLength = cW - 40, LineThickness = 2, DiamondSize = 5, Color = 0xFF353535u, Anchor = UiAnchor.Middle
        });
        line.X = cW / 2;
        line.Y = dividerY;
        cardBg.AddChild(line);

        var rewardsPill = new SdfRect(new SdfRectConfig {
            Width = 80, Height = 22, Color = 0xFF151515u, OutlineColor = 0xFF353535u, OutlineSize = 2f, Cut = 11,
            ShadowRadius = 0, Anchor = UiAnchor.Middle
        });
        rewardsPill.X = cW / 2;
        rewardsPill.Y = dividerY;
        cardBg.AddChild(rewardsPill);

        var rewardsText = new SimpleText(new TextConfig {
            Text = "Rewards", FontSize = 12, Color = 0xFFDDDDDDu, FontType = FontType.Bold, Anchor = UiAnchor.Middle
        });
        rewardsText.X = rewardsPill.Width / 2;
        rewardsText.Y = rewardsPill.Height / 2;
        rewardsPill.AddChild(rewardsText);

        var rewY = dividerY + 30;
        Action updateButtonState = null;

        Action<int> onRewardClicked = index => {
            if (!quest.ItemOfChoice || quest.Completed) return;
            selectedReward = index;
            DrawSlots(cardBg, rewY, quest.Rewards, cW, true, selectedReward, null);
            updateButtonState?.Invoke();
        };

        DrawSlots(cardBg, rewY, quest.Rewards, cW, quest.ItemOfChoice, selectedReward, onRewardClicked);

        if (quest.ItemOfChoice) {
            var choiceHint = new SimpleText(new TextConfig {
                Text = "Choose one item", FontSize = 10, Color = 0xFF888888u, Anchor = UiAnchor.MiddleTop
            });
            choiceHint.X = cW / 2;
            choiceHint.Y = dividerY + 15;
            cardBg.AddChild(choiceHint);
        }

        var canComplete = !quest.Completed && hasAllItems && selectedReward != -1;

        var completeBtn = new SdfRect(new SdfRectConfig {
            Width = 140, Height = 34,
            Color = quest.Completed ? 0xFF444444u : canComplete ? 0xFF4A9A4Au : 0xFF555555u,
            OutlineColor = 0xFF353535u, OutlineSize = 1.5f, Cut = 4, ShadowRadius = 0,
            Anchor = UiAnchor.MiddleBottom
        });
        completeBtn.X = cW / 2;
        completeBtn.Y = cH - 20;
        cardBg.AddChild(completeBtn);

        var btnText = new SimpleText(new TextConfig {
            Text = quest.Completed ? "Completed" : "Complete!",
            FontSize = 14,
            Color = canComplete || quest.Completed ? 0xFFFFFFFFu : 0xFFAAAAAAu,
            FontType = FontType.Bold,
            Anchor = UiAnchor.Middle
        });
        btnText.X = completeBtn.Width / 2;
        btnText.Y = completeBtn.Height / 2;
        completeBtn.AddChild(btnText);

        updateButtonState = () => {
            canComplete = !quest.Completed && hasAllItems && selectedReward != -1;
            completeBtn.SetColor(canComplete ? 0xFF4A9A4Au : 0xFF555555u);
            completeBtn.SetColorSecondary(canComplete ? 0xFF6BD06Bu : 0xFF777777u);
            btnText.SetColor(canComplete ? 0xFFFFFFFFu : 0xFFAAAAAAu);
            completeBtn.MouseEnabled = canComplete;
        };

        completeBtn.MouseEnabled = canComplete;
        completeBtn.AddEventListener(MouseEvent.LeftClick, () => {
            if (!canComplete) return;
            completeBtn.MouseEnabled = false;
            SendRedeemPacket(quest.Id, slotsToConsume, quest.ItemOfChoice ? quest.Rewards[selectedReward] : -1);
            quest.Completed = true;
            btnText.SetText("Completed");
            updateButtonState();
        });
    }

    private void DrawSlots(Sprite parent, int startY, List<int> items, int cardWidth, bool selectable,
        int selectedIndex, Action<int> onClick, List<bool> fulfilled = null) {
        var iconSize = 40;
        var spacing = 6;
        var maxPerRow = 4;
        var rows = (int)Math.Ceiling(items.Count / (float)maxPerRow);

        for (var r = 0; r < rows; r++) {
            var itemsInRow = Math.Min(maxPerRow, items.Count - r * maxPerRow);
            var rowW = itemsInRow * iconSize + (itemsInRow - 1) * spacing;
            var startX = (cardWidth - rowW) / 2;
            var rowY = startY + r * (iconSize + spacing);

            for (var i = 0; i < itemsInRow; i++) {
                var itemIndex = r * maxPerRow + i;
                var itemId = items[itemIndex];
                var isSelected = selectable && itemIndex == selectedIndex;
                var isFulfilled = fulfilled != null && itemIndex < fulfilled.Count && fulfilled[itemIndex];

                var bg = new SdfRect(new SdfRectConfig {
                    Width = iconSize,
                    Height = iconSize,
                    Cut = 4,
                    Color = isSelected ? 0xFF4A6A9Au : isFulfilled ? 0xFF3A6A3Au : 0xFF2D2D2Du,
                    OutlineColor = isSelected ? 0xFF7090C0u : isFulfilled ? 0xFF6BD06Bu : 0xFF484848u,
                    OutlineSize = 2f,
                    ShadowRadius = 0
                });
                bg.X = startX + i * (iconSize + spacing);
                bg.Y = rowY;
                parent.AddChild(bg);

                var icon = new ObjectRect(new ObjectRectConfig {
                    Texture = TextureHelper.FromGameAtlas((ushort)itemId),
                    Width = iconSize - 8,
                    Height = iconSize - 8
                });
                icon.X = bg.X + 4;
                icon.Y = bg.Y + 4;
                parent.AddChild(icon);

                if (selectable && onClick != null) {
                    bg.MouseEnabled = true;
                    var capturedIndex = itemIndex;
                    bg.AddEventListener(MouseEvent.LeftClick, () => onClick(capturedIndex));
                }
            }
        }
    }

    private bool CheckInventory(List<int> requirements, out List<ObjectSlot> slotsToConsume, out List<bool> fulfilled) {
        slotsToConsume = new List<ObjectSlot>();
        fulfilled = new List<bool>();

        if (Map.LocalPlayer == null) return false;

        var availableItems = new List<(int slotId, int objType)>();
        for (var i = 4; i < 20; i++) {
            if (i >= Map.LocalPlayer.Equipment.Length) continue;
            var item = Map.LocalPlayer.Equipment[i];
            if (item != null && item.ObjectType != 0xFFFF)
                availableItems.Add((i, item.ObjectType));
        }

        var allMet = true;

        foreach (var req in requirements) {
            var foundIdx = availableItems.FindIndex(x => x.objType == req);
            if (foundIdx != -1) {
                fulfilled.Add(true);
                slotsToConsume.Add(new ObjectSlot {
                    ObjectId = Map.LocalPlayer.ObjectId,
                    SlotId = (byte)availableItems[foundIdx].slotId,
                    ObjectType = (ushort)req
                });
                availableItems.RemoveAt(foundIdx);
            } else {
                fulfilled.Add(false);
                allMet = false;
            }
        }

        return allMet;
    }

    private void SendRedeemPacket(string questId, List<ObjectSlot> slots, int itemOfChoiceReward) {
        var pkt = QuestRedeem.CreatePacket();
        pkt.QuestId = questId;
        pkt.Item = -1;
        pkt.Slots = slots;
        Client.QueuePacket(pkt);

        var reqPkt = QuestFetchAsk.CreatePacket();
        Client.QueuePacket(reqPkt);
    }

    private void OnStageEnter() {
        Stage.AddEventListener(ResizeEvent.Resize, OnResize);
        OnResize(new ResizeEvent("", Stage.StageWidth, Stage.StageHeight));
    }

    private void OnStageExit() {
        Stage.RemoveEventListener(ResizeEvent.Resize, OnResize);
    }

    private void OnCloseClicked() {
        UserInput.SetInputDisabled(false);
        GameSprite.DailyQuestWindow = null;
        Parent?.RemoveChild(this);
    }

    private void OnResize(ResizeEvent args) {
        SetPosition(args.Width, args.Height);
    }

    private void SetPosition(int width, int height) {
        _screenContainer.SetAnchor(UiAnchor.LeftTop);
        _screenContainer.Scale = Stage.ScreenScale;

        var screenWidth = width - HudView.Width * Stage.ScreenScale.X;
        var totalH = (TabIslandH + TabIslandGap + BgHeight + Pad * 2) * Stage.ScreenScale.Y;
        var knownW = (BgWidth + Pad * 2) * Stage.ScreenScale.X;

        _screenContainer.X = (int)(screenWidth / 2f - knownW / 2f);
        _screenContainer.Y = (int)(height / 2f - totalH / 2f);
    }
}