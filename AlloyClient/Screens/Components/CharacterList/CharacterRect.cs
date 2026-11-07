using System;
using System.Reflection.Metadata.Ecma335;
using AlloyClient.AppEngine;
using AlloyClient.Assets.Libraries;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.Game;
using AlloyClient.Screens.Components.Containers;
using AlloyClient.Ui.Components.Dialogs;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Extra;
using AlloyClient.Utils;

namespace AlloyClient.Screens.Components.CharacterList;

public enum CharacterRectType
{
    Character,
    GraveyardCharacter,
    NewCharacter
}

public sealed class CharacterRect : Container
{
    private const int NumberStatsMaxed = 8;
    private const int NumberStars = 5;

    private const int NumberCharacters = 15;

    private readonly CharacterListScreen _characterListScreen;

    private int _statsMaxed;
    private int _baseFame;

    private SimpleText _remainingSlotsText;
    private TextButton _textButton;

    public CharacterRect(CharacterListScreen characterListScreen) : base(new ContainerConfig
        { Width = 200, Height = 200, EnableClip = true })
    {
        _characterListScreen = characterListScreen;

        var background = new ColorRect(new ColorRectConfig
        {
            Width = 200,
            Height = 200,
            Color = 0x2B2B2B,
            Alpha = 0.7f,
        });
        AddChild(background);
    }

    public void Initialize(CharacterRectType type, Character character = null, int remainingSlots = 0)
    {
        var charNameText = new SimpleText(new TextConfig
        {
            Text = "New Character",
            FontSize = 18,
            FontType = FontType.Bold,
            Color = 0xFFFFFF,
            Anchor = UiAnchor.Middle,
        });
        charNameText.X = Width / 2;
        charNameText.Y = charNameText.Height / 2 + 10;
        AddChild(charNameText);

        if (type == CharacterRectType.Character) 
        {
            var xBoxLive = new TextButton(new TextButtonConfig 
            {
                Text = "X",
                FontSize = 24,
                Anchor = UiAnchor.Middle,
                ActiveColor = 0xFF0000,
                OnClicked = type switch {
                    CharacterRectType.Character => () => 
                    {
                        if (character == null)
                            return;

                        OverlayManager.Set(new DeleteCharacterPromptContainer(_characterListScreen, character));
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                }
            });

            xBoxLive.X = Width - 10;
            xBoxLive.Y = 15;
            AddChild(xBoxLive);
        }

        var textButton = new TextButton(new TextButtonConfig
        {
            Text = type switch
            {
                CharacterRectType.Character => "Play",
                CharacterRectType.GraveyardCharacter => "View",
                CharacterRectType.NewCharacter => remainingSlots > 0 ? "Create" : "Purchase Slot",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            },
            FontSize = 24,
            Anchor = UiAnchor.Middle,
            OnClicked = type switch
            {
                CharacterRectType.Character => () =>
                {
                    if (character == null)
                        return;

                    GlobalData.SelectedCharacterId = character.Id;
                    ScreenManager.FadeToScreen(new GameScreen(), Easing.SineInOut, 1000, 0x0);
                },
                CharacterRectType.GraveyardCharacter => () =>
                {
                    if (character == null)
                        return;
                },
                CharacterRectType.NewCharacter => () =>
                {
                    if (remainingSlots < 1)
                    {
                        PurchaseSlot();
                        return;
                    }

                    _characterListScreen.ShowCharacterCreate();
                },
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            }
        });
        textButton.X = Width / 2;
        textButton.Y = Height - textButton.Height / 2 - 12;
        _textButton = textButton;
        AddChild(textButton);

        if (character != null)
        {
        #region Character and Graveyard Character

            var props = ObjectLibrary.TypeToObjectProps[character.ObjectType];
            charNameText.SetText(props.ObjectId);

            _statsMaxed = character.Level;
            var charLevelText = new SimpleText(new TextConfig
            {
                Text = $"Level {character.Level}",
                FontSize = 16,
                FontType = FontType.Bold,
                Color = 0xCFCFCF,
                Anchor = UiAnchor.Middle,
            });
            charLevelText.X = Width / 2;
            charLevelText.Y = charNameText.Height + charLevelText.Height / 2 + 13;
            AddChild(charLevelText);

            var textureData = ObjectLibrary.TypeToTextureData.ContainsKey((ushort)character.Skin)
                ? ObjectLibrary.TypeToTextureData[(ushort)character.Skin]
                : ObjectLibrary.TypeToTextureData[character.ObjectType];
            var atlasData = textureData.AnimatedTextures.FaceDown[0];

            var charPortrait = new ObjectRect(new ObjectRectConfig
            {
                Texture = TextureHelper.Create(atlasData, TextureType.GameAtlas),
                Width = 50,
                Height = 50,
                Anchor = UiAnchor.Middle,
            });
            charPortrait.X = Width / 2;
            charPortrait.Y = charLevelText.Y + charPortrait.Height / 2 + 14;
            AddChild(charPortrait);

            var charIdText = new SimpleText(new TextConfig
            {
                Text = $"Id: {character.Id.ToString()}",
                FontSize = 16,
                FontType = FontType.Bold,
                Color = 0xFFFFFF,
                Anchor = UiAnchor.Middle,
            });
            charIdText.X = Width / 2;
            charIdText.Y = charPortrait.Y + charPortrait.Height / 2 + 14;
            AddChild(charIdText);

            /*const int middleStarIndex = NumberStars / 2;
            const int starWidth = 16;
            var startX = Width / 2 - middleStarIndex * starWidth;
            var numStars = GetStars(character);
            for (var i = 0; i < NumberStars; i++)
            {
                var star = new ObjectRect(new ObjectRectConfig
                {
                    Texture = TextureHelper.FromUiAtlas("CharacterList/StarGraphic"),
                    Width = starWidth,
                    Height = starWidth,
                    Anchor = UiAnchor.Middle,
                    X = startX + i * starWidth,
                    Y = fameText.Y + starWidth / 2 + 12,
                });
                AddChild(star);

                if (i >= numStars)
                {
                    star.ColorTransformation = new ColorTransform(0.5f, 0.5f, 0.5f, 1);
                }
            }*/

        #endregion
        }
        else
        {
        #region New Character

            var index = Random.Shared.Next(0, NumberCharacters);
            var frames = Main.Atlas.GetAnimationAtlasData("players", index);
            var charPortrait = new ObjectRect(new ObjectRectConfig
            {
                Texture = TextureHelper.Create(frames.FaceDown[0], TextureType.GameAtlas),
                Width = 50,
                Height = 50,
                Anchor = UiAnchor.Middle,
                OutlineEnabled = false,
                GlowEnabled = false
            });
            charPortrait.X = Width / 2;
            charPortrait.Y = charNameText.Height + charPortrait.Height / 2 + 25;
            charPortrait.ColorTransformation = new ColorTransform(0, 0, 0, 0.5f);
            AddChild(charPortrait);

            var plural = remainingSlots != 1 ? "s" : "";
            var remainingSlotsText = new SimpleText(new TextConfig
            {
                Text = $"{remainingSlots} Character Slot{plural}",
                FontSize = 18,
                FontType = FontType.Bold,
                Color = 0xFFFFFF,
                Anchor = UiAnchor.Middle,
            });
            remainingSlotsText.X = Width / 2;
            remainingSlotsText.Y = charPortrait.Y + charPortrait.Height / 2 + 25;
            _remainingSlotsText = remainingSlotsText;
            AddChild(remainingSlotsText);

            var remainingSlotsText2 = new SimpleText(new TextConfig
            {
                Text = "Remaining",
                FontSize = 18,
                FontType = FontType.Bold,
                Color = 0xFFFFFF,
                Anchor = UiAnchor.Middle,
            });
            remainingSlotsText2.X = Width / 2;
            remainingSlotsText2.Y = remainingSlotsText.Y + remainingSlotsText.Height / 2 + 12;
            AddChild(remainingSlotsText2);

        #endregion
        }
    }

    public int ComputeSortValue()
    {
        return _statsMaxed * 1000 + _baseFame;
    }

    private static int GetStatsMaxed(Character characterModel)
    {
        var player = ObjectLibrary.TypeToObjectProps[characterModel.ObjectType];
        var playerProps = player.PlayerProperties;

        var numStatsMaxed = 0;
        if (characterModel.MaxHitPoints >= playerProps.MaxHp)
        {
            numStatsMaxed++;
        }

        if (characterModel.MaxMagicPoints >= playerProps.MaxMp)
        {
            numStatsMaxed++;
        }

        if (characterModel.Attack >= playerProps.MaxAttack)
        {
            numStatsMaxed++;
        }

        if (characterModel.Defense >= playerProps.MaxDefense)
        {
            numStatsMaxed++;
        }

        if (characterModel.Speed >= playerProps.MaxSpeed)
        {
            numStatsMaxed++;
        }

        if (characterModel.Dexterity >= playerProps.MaxDexterity)
        {
            numStatsMaxed++;
        }

        if (characterModel.Vitality >= playerProps.MaxVitality)
        {
            numStatsMaxed++;
        }

        if (characterModel.Wisdom >= playerProps.MaxWisdom)
        {
            numStatsMaxed++;
        }

        return numStatsMaxed;
    }

    private async void PurchaseSlot()
    {
        var result = await AppRequests.PurchaseCharSlot();

        if (result.Success) 
        {
            _characterListScreen.RefreshCharacterList();
            return;
        }
                        
        var dialog = new Dialog("Purchase Error", result.Message, new DialogOption("Ok"));
        DialogManager.Enqueue(dialog);
    }

    private static int GetStars(Character characterModel)
    {
        return 0;
        //TODO: stars
        /*var fame = characterModel.CurrentFame;
        var accountModel = Account.Model;
        if (accountModel.Stats == null || accountModel.Stats.ClassStats == null)
        {
            Logger.Fatal("accountModel.Stats.ClassStats is null! At CharacterRect.cs");
            return 0;
        }
        foreach (var classStatModel in accountModel.Stats.ClassStats)
        {
            var objTypeStr = classStatModel.ObjectType;
            var objectType = Convert.ToUInt16(objTypeStr, Common.Utils.GetBase(objTypeStr));
            if (objectType != characterModel.ObjectType)
            {
                continue;
            }

            if (fame < classStatModel.BestFame)
            {
                fame = classStatModel.BestFame;
            }
        }

        return fame switch
        {
            < 20 => 0,
            < 150 => 1,
            < 400 => 2,
            < 800 => 3,
            < 2000 => 4,
            _ => 5,
        };*/
    }
}