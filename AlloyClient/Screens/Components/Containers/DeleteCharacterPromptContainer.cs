using System;
using System.Threading.Tasks;
using AlloyClient.AppEngine;
using AlloyClient.Data;
using AlloyClient.Display;
using AlloyClient.State;
using AlloyClient.Ui.Components.Dialogs;
using AlloyClient.Ui.Components.Panels;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.BuiltIn.Buttons;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using Common;

namespace AlloyClient.Screens.Components.Containers;

public class DeleteCharacterPromptContainer : Overlay 
{
    private static readonly Logger Log = new(typeof(Settings));
    
    CharacterListScreen _characterListScreen;
    
    public DeleteCharacterPromptContainer(CharacterListScreen characterListScreen, Character character) 
    {
        _characterListScreen = characterListScreen;
        
        var cx = Settings.DefaultScreenWidth / 2;
        var cy = Settings.DefaultScreenHeight / 2;
        
        var background = new ColorRect(new ColorRectConfig
            {   
                Width = 800,
                Height = 160,
                Color = 0x363636, 
                X = cx, 
                Y = cy, 
                Anchor = UiAnchor.Middle 
            });
        AddChild(background);
        
        var message = new SimpleText(new TextConfig 
        {
            Text = "Are you SUUUUUUURE you want to delete this PRECIOUS hero??", 
            FontSize = 26, 
            FontType = FontType.Bold, 
            X = background.Width / 2, 
            Y = background.Height / 2 - 40, 
            Color = 0xD12D00,
            Anchor = UiAnchor.Middle
        });
        background.AddChild(message);
        
        var YESButton = new TextButton(new TextButtonConfig 
        {
            Text = "YES YES YES!!!", 
            FontSize = 48,
            OnClicked = () => 
            {
                DeleteCharacter(character);
            }, 
            FontType = FontType.Bold, 
            X = 80,
            Y = background.Height - 30,
            ActiveColor = 0x00FF00,
            Anchor = UiAnchor.MiddleLeft
        });
        background.AddChild(YESButton);
        
        var noButton = new TextButton(new TextButtonConfig 
        {
            Text = "No...", 
            FontSize = 26,
            OnClicked = CloseOverlay, 
            FontType = FontType.Bold, 
            X = background.Width / 2 + 150,
            Y = background.Height - 30, 
            Anchor = UiAnchor.MiddleLeft
        });
        background.AddChild(noButton);
    }

    private async void DeleteCharacter(Character character)
    {
        AppResponse result;
        try 
        {
            result = await AppRequests.DeleteCharacter(character.Id.ToString());
        } catch (Exception e) 
        {
            Log.Error($"Failed to delete character: {character.Id}: {e.Message}");
            return;
        }

        if (result.Success) 
        {
            _characterListScreen.RefreshCharacterList();
        }
        else 
            Console.WriteLine($"Failed to delete character: {result}");
                
        CloseOverlay();
    }
}