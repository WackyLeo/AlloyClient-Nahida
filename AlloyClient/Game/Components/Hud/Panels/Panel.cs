using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Signals;

namespace AlloyClient.Game.Components.Hud.Panels;

public abstract class Panel : Sprite {

    public const int PanelWidth = 218;
    public const int PanelHeight = 110;

    public static readonly Signal OnInteract = new(); 
    
    protected Panel() {
        AddEventListener(Event.AddedToStage, () => { OnInteract.Add(OnInteractKey); });
        AddEventListener(Event.RemovedFromStage, () => { OnInteract.Remove(OnInteractKey); });
    }

    protected virtual void OnInteractKey() { }
}