using System.Collections.Generic;
using AlloyClient.Game.Objects;
using AlloyClient.UiLib.Core;
using Common;

namespace AlloyClient.Ui.Character;

public class NotificationLayer : Sprite {
    private static readonly Queue<CharacterStatusText> TextQueue = new();

    public NotificationLayer() {
        TextQueue.Clear();
        AddEventListener(Event.AddedToStage, () => { AddEventListener(Event.EnterFrame, OnFrameEnter); });
        AddEventListener(Event.RemovedFromStage, () => { RemoveEventListener(Event.EnterFrame, OnFrameEnter); });
    }

    public static void AddStatusText(Entity en, string text, uint color, int lifetime, int offsetTime) {
        var data = new CharacterStatusText(en, text, color, lifetime, offsetTime);
        TextQueue.Enqueue(data);
    }
    
    public static int Green2Red(int hpPercentage) {
        if (hpPercentage > 50)
            return 0xFF00 + (327680 * (100 - hpPercentage));
        return 0xFFFF00 - (0x0500 * (50 - hpPercentage));
    }

    public void Update(GameTime gameTime) {
        while (TextQueue.TryDequeue(out var characterStatusText)) AddChild(characterStatusText);
    }

    private void OnFrameEnter() {
        while (TextQueue.TryDequeue(out var characterStatusText)) AddChild(characterStatusText);
    }
}