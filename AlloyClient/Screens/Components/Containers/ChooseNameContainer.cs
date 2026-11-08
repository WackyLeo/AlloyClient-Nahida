
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

namespace AlloyClient.Screens.Components.Containers;

public class ChooseNameContainer : Overlay 
{
    private readonly TextInput _newNameInput;
    private CharacterListScreen _characterListScreen;
    
    public ChooseNameContainer(CharacterListScreen characterListScreen) 
    {
        _characterListScreen = characterListScreen;
        
        var cx = Settings.DefaultScreenWidth / 2;
        var cy = Settings.DefaultScreenHeight / 2;

        var background = new ColorRect(new ColorRectConfig
        {
            Width = 475, 
            Height = 200, 
            Color = 0x363636, 
            X = cx, 
            Y = cy, 
            Anchor = UiAnchor.Middle
        });
        AddChild(background);

        var titleBackground = new ColorRect(new ColorRectConfig
        {
            Width = 475, 
            Height = 50, 
            Color = 0x4d4d4d, 
            X = 0, 
            Y = 0, 
            Anchor = UiAnchor.LeftTop
        });
        background.AddChild(titleBackground);

        var title = new SimpleText(new TextConfig 
        {
            Text = "Choose Name", 
            FontSize = 22, 
            FontType = FontType.Bold, 
            X = titleBackground.Width / 2, 
            Y = titleBackground.Height / 2, 
            Color = 0xFFFFFF,
            Anchor = UiAnchor.Middle
        });
        titleBackground.AddChild(title);

        _newNameInput = new TextInput(new InputConfig 
        {
            X = background.Width / 2,
            Y = titleBackground.Height + 50, 
            FontSize = 24, 
            FontType = FontType.Bold, 
            Color = 0xFFFFFF, 
            Width = 350,
            DefaultText = "Name", 
            Anchor = UiAnchor.Middle
        });
        background.AddChild(_newNameInput);

        var registerButton = new TextButton(new TextButtonConfig 
        {
            Text = "Confirm", 
            FontSize = 28, 
            OnClicked = OnNameChosen, 
            FontType = FontType.Bold, 
            X = background.Width - 10, 
            Y = background.Height - 10,
            Anchor = UiAnchor.RightBottom
        });
        background.AddChild(registerButton);

        var cancelButton = new TextButton(new TextButtonConfig 
        {
            Text = "Cancel", 
            FontSize = 28, 
            OnClicked = CloseOverlay, 
            FontType = FontType.Bold, 
            X = 10,
            Y = background.Height - 10, 
            Anchor = UiAnchor.LeftBottom
        });
        background.AddChild(cancelButton);
    }
    
    private void OnNameChosen() 
    {
        AddEventListener(AppRequests.RenameAccount(_newNameInput.Text.Trim()), OnRenameResponse);
    }
    
    private void OnRenameResponse(AppResponse response) 
    {
        if (!response.Success) 
        {
            var dialog = new Dialog("Naming Error", response.Message, new DialogOption("Ok"));
            DialogManager.Enqueue(dialog);
            return;
        }
        _characterListScreen.SetName(_newNameInput.Text.Trim());
        CloseOverlay();
        DispatchEvent(new Event("renameSuccess"));
    }
}