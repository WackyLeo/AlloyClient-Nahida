using System;
using System.Collections.Generic;
using AlloyClient.AppEngine;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.Game;
using AlloyClient.Screens.Components;
using AlloyClient.Screens.Components.CharacterList;
using AlloyClient.Screens.Components.Containers;
using AlloyClient.State;
using AlloyClient.Ui.Components.Buttons;
using AlloyClient.Ui.Components.Scrollbars;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Extra;
using AlloyClient.Utils;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Screens;

public class CharacterListScreen : TitleScreenBase {
    private const int PlayFontSize = 55;
    private const int FontSize = 35;
    
    private readonly MenuBarButton _playButton;
    private readonly MenuBarButton _classesButton;
    private readonly MenuBarButton _backButton;
    private readonly ColorRect _lineDivider;
    
    private readonly TextButton _nameText;
    private readonly ObjectRect _goldIcon;
    private readonly SimpleText _goldText;
    private readonly ObjectRect _fameIcon;
    private readonly SimpleText _fameText;

    private readonly TextButton _charactersButton;
    private readonly TextButton _graveyardButton;

    private readonly Container _scrollContainer;
    private readonly Container _characterListContainer;
    private readonly Container _graveyardContainer;
    
    private readonly List<CharacterRect> _characterListRects = [];
    private CharacterRect _newCharacterRect;
    private List<CharacterRect> _graveyardCharacterRects = [];
    
    private VerticalScrollBar _scrollBar;
    private int _selectedCharacterId = -1;

    private const int CharListVisibleHeight = Settings.DefaultScreenHeight - 185;
    
    private static readonly Logger Log = new(typeof(Settings));

    public CharacterListScreen() {
        #region Title Buttons

        _playButton = new MenuBarButton("play", PlayFontSize, () => {
            var data = GlobalData.Get<CharacterListData>();
            var charList = data.Characters;
            if (charList == null || charList.Length <= 0)
            {
                ShowCharacterCreate();
                return;
            }
            GlobalData.SelectedCharacterId = _selectedCharacterId;
            ScreenManager.FadeToScreen(new GameScreen(), Easing.SineInOut, 1000, 0x0);
        }, true);
        _playButton.SetAnchor(UiAnchor.Middle);
        AddChild(_playButton);

        _classesButton = new MenuBarButton("classes", FontSize, ShowCharacterCreate);
        _classesButton.SetAnchor(UiAnchor.MiddleLeft);
        AddChild(_classesButton);

        _backButton = new MenuBarButton("back", FontSize, () => {
            ScreenManager.FadeToScreen(new TitleScreen(), Easing.SineInOut, 1000, 0x0);
        });
        _backButton.SetAnchor(UiAnchor.MiddleRight);
        AddChild(_backButton);

        #endregion

        #region Decoration
        _lineDivider = new ColorRect(new ColorRectConfig {
            Width = Settings.DefaultScreenWidth,
            Height = 5,
            Color = 0x2B2B2B,
        });
        AddChild(_lineDivider);

        #endregion

        #region Name and Currency

        var account = GlobalData.Get<AccountData>();

        _nameText = new TextButton(new TextButtonConfig() 
        {
            Text = account.Name,
            FontSize = 32,
            FontType = FontType.Bold,
            ActiveColor = 0xB3B3B3,
            InactiveColor = 0xB3B3B3,
            OnClicked = () => 
            {
                OverlayManager.Set(new ChooseNameContainer(this));
            },
            Anchor = UiAnchor.Middle,
        });
        AddChild(_nameText);

        _goldIcon = new ObjectRect(new ObjectRectConfig {
            Texture = TextureHelper.FromGameAtlas("lofiObj3", 0xE1),
            Width = 16,
            Height = 16,
            Anchor = UiAnchor.RightBottom,
        });
        AddChild(_goldIcon);

        _goldText = new SimpleText(new TextConfig {
            Text = account.Stats.Credits.ToString(),
            FontSize = 24,
            FontType = FontType.Normal,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.RightBottom,
        });
        AddChild(_goldText);

        _fameIcon = new ObjectRect(new ObjectRectConfig {
            Texture = TextureHelper.FromGameAtlas("lofiObj3", 0xE0),
            Width = 16,
            Height = 16,
            Anchor = UiAnchor.RightBottom,
        });
        AddChild(_fameIcon);

        _fameText = new SimpleText(new TextConfig {
            Text = account.Stats.Fame.ToString(),
            FontSize = 24,
            FontType = FontType.Normal,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.RightBottom,
        });
        AddChild(_fameText);

        #endregion

        #region Containers
        _scrollContainer = new Container(new ContainerConfig { 
            Width = Settings.DefaultScreenWidth,
            Height = Settings.DefaultScreenHeight - 185,
            EnableClip = true 
        });
        AddChild(_scrollContainer);

        _characterListContainer = new Container();
        _scrollContainer.AddChild(_characterListContainer);

        _graveyardContainer = new Container();
        _scrollContainer.AddChild(_graveyardContainer);
        _graveyardContainer.Visible = false;

        #endregion

        #region Tab Buttons

        _charactersButton = new TextButton(new TextButtonConfig {
            Text = "Characters",
            FontSize = 24,
            OnClicked = () => {
                _characterListContainer.Visible = true;
                _graveyardContainer.Visible = false;
                _graveyardButton.Alpha = 0.6f;
                _charactersButton.Alpha = 1f;
            }
        });
        AddChild(_charactersButton);

        _graveyardButton = new TextButton(new TextButtonConfig {
            Text = "Graveyard",
            FontSize = 24,
            OnClicked = () => {
                _graveyardContainer.Visible = true;
                _characterListContainer.Visible = false;
                _charactersButton.Alpha = 0.6f;
                _graveyardButton.Alpha = 1f;
            }
        });
        _graveyardButton.Alpha = 0.6f;
        AddChild(_graveyardButton);

        #endregion

        MouseEnabled = true;

        AddEventListener(AppRequests.GetCharList(), () => {
            LoadCharacterList();
            LoadGraveyardList();
        });
    }

    protected override void OnResize(ResizeEvent args) {
        base.OnResize(args);

        float uniformScale = Math.Min(Stage.ScreenScale.X, Stage.ScreenScale.Y);
        _nameText.Scale = new Vector2(uniformScale);
        _nameText.X = args.Width / 2;
        _nameText.Y = (int)(50 * uniformScale);

        int rightPadding = (int)(15 * uniformScale);
        int topPadding = (int)(88 * uniformScale);
        int iconSpacing = (int)(5 * uniformScale);
        int statSpacing = (int)(10 * uniformScale);

        _goldIcon.Scale = new Vector2(uniformScale);
        _goldIcon.X = args.Width - rightPadding;
        _goldIcon.Y = topPadding;

        _goldText.Scale = new Vector2(uniformScale);
        _goldText.X = _goldIcon.X - _goldIcon.Width - iconSpacing;
        _goldText.Y = (int)(93 * uniformScale);

        _fameIcon.Scale = new Vector2(uniformScale);
        _fameIcon.X = _goldText.X - _goldText.Width - statSpacing;
        _fameIcon.Y = topPadding;

        _fameText.Scale = new Vector2(uniformScale);
        _fameText.X = _fameIcon.X - _fameIcon.Width - iconSpacing;
        _fameText.Y = (int)(93 * uniformScale);

        _lineDivider.Resize(args.Width, (int)Math.Max(1, 5 * uniformScale));
        _lineDivider.X = 0;
        _lineDivider.Y = (int)(100 * uniformScale);

        _scrollContainer.Scale = new Vector2(uniformScale);
        float gridLeftEdge = (args.Width - (Settings.DefaultScreenWidth * uniformScale)) / 2f;
        _scrollContainer.X = (int)gridLeftEdge;
        _scrollContainer.Y = (int)(105 * uniformScale);

        _charactersButton.Scale = new Vector2(uniformScale);
        _charactersButton.X = (int)(gridLeftEdge + 15 * uniformScale); 
        _charactersButton.Y = _lineDivider.Y - _charactersButton.Height / 2 - (int)(15 * uniformScale);

        _graveyardButton.Scale = new Vector2(uniformScale);
        _graveyardButton.X = _charactersButton.X + _charactersButton.Width + (int)(25 * uniformScale);
        _graveyardButton.Y = _charactersButton.Y;
        
        _playButton.Scale = new Vector2(uniformScale);
        _classesButton.Scale = new Vector2(uniformScale);
        _backButton.Scale = new Vector2(uniformScale);

        int bottomY = args.Height - (int)(50 * uniformScale);
        int gap = (int)(50 * uniformScale);

        _playButton.X = args.Width / 2;
        _playButton.Y = bottomY;

        _classesButton.X = _playButton.X + _playButton.Width / 2 + gap;
        _classesButton.Y = bottomY;

        _backButton.X = _playButton.X - _playButton.Width / 2 - gap;
        _backButton.Y = bottomY;
        UpdateGridLayout();
    }


    public void SetFame(string fame)
    {
        _fameText.SetText(fame);
    }

    public async void RefreshCharacterList() 
    {
        var account = GlobalData.Get<AccountData>();
        
        try 
        {
            await AppRequests.GetCharList();
        } catch (Exception e) 
        {
            Log.Error($"Failed to retrieve character list for account {account.AccountId}: {e.Message}");
            return;
        }
        
        if (account.Stats.Fame.ToString() != _fameText.Text)
            _fameText.SetText(account.Stats.Fame.ToString());
        
        _characterListContainer.RemoveChildren();
        _characterListRects.Clear();
        LoadCharacterList();
        LoadGraveyardList();
    }
    
    private void LoadCharacterList() {
        var charModel = GlobalData.Get<CharacterListData>();
        if (charModel == null) return;
        
        var characters = charModel.Characters;
        if (characters != null) {
            foreach (var character in characters) {
                if (_selectedCharacterId == -1) {
                    _selectedCharacterId = character.Id;
                }
                
                var charRect = new CharacterRect(this);
                charRect.EnableClipRect = true;
                charRect.Initialize(CharacterRectType.Character, character);
                _characterListContainer.AddChild(charRect);
                _characterListRects.Add(charRect);
            }
        }
        
        _characterListRects.Sort((a, b) => {
            var aSortValue = a.ComputeSortValue();
            var bSortValue = b.ComputeSortValue();
            return bSortValue.CompareTo(aSortValue);
        });

        var remainingSlots = charModel.MaxNumChars - _characterListRects.Count;
        _newCharacterRect = new CharacterRect(this);
        _newCharacterRect.Initialize(CharacterRectType.NewCharacter, remainingSlots: remainingSlots);
        _characterListContainer.AddChild(_newCharacterRect);
        
        UpdateGridLayout();
    }

    private void UpdateGridLayout() {
        float cardWidth = 210;
        float cardHeight = 210;

        int cols = Settings.DefaultScreenWidth / (int)cardWidth;
        if (cols < 1) cols = 1;

        float gridTotalWidth = cols * cardWidth;
        float startX = (Settings.DefaultScreenWidth - gridTotalWidth) / 2f;
        float startY = 15;

        int index = 0;
        foreach (var rect in _characterListRects) {
            rect.X = (int)(startX + (index % cols) * cardWidth);
            rect.Y = (int)(startY + (index / cols) * cardHeight);
            index++;
        }
        
        if (_newCharacterRect != null) {
            _newCharacterRect.X = (int)(startX + (index % cols) * cardWidth);
            _newCharacterRect.Y = (int)(startY + (index / cols) * cardHeight);
            index++;
        }

        int totalRows = (int)Math.Ceiling((float)index / cols);
        float totalContentHeight = startY + (totalRows * cardHeight) + 15;

        if (_scrollBar != null) {
            _scrollContainer.RemoveChild(_scrollBar);
        }

        _scrollBar = new VerticalScrollBar(_scrollContainer, new VerticalScrollBarConfig {
            X = Settings.DefaultScreenWidth - 15,
            Y = 5,
            Width = 10,
            Height = CharListVisibleHeight - 5,
            TotalContentHeight = (int)totalContentHeight,
            VisibleContentHeight = CharListVisibleHeight,
            OnValueChanged = value => {
                _characterListContainer.Y = -value;
            }
        });
        _scrollContainer.AddChild(_scrollBar);
    }
    
    public void SetName(string newName)
    {
        _nameText.SetText(newName);
    }

    private void LoadGraveyardList() {
        // TODO: Implement graveyard
    }

    public void ShowCharacterCreate() {
        OverlayManager.Set(new ClassContainer());
    }
    
    public void HideCharacterCreate() {
    }
}