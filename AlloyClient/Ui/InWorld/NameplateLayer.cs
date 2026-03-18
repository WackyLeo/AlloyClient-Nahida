using System.Collections.Generic;
using AlloyClient.Game.Objects;
using AlloyClient.Game;
using AlloyClient.Game.Objects.Util;
using AlloyClient.UiLib.Core;

namespace AlloyClient.Ui.InWorld
{
    public class NameplateLayer : Sprite 
    {
        private readonly Dictionary<int, Nameplate> _activeNameplates = new();

        public NameplateLayer() 
        {
            AddEventListener(Event.EnterFrame, OnFrameEnter);
        }

        private void OnFrameEnter() 
        {
            foreach (var kvp in Map.Entities) 
            {
                var entity = kvp.Value;
                
                if (!_activeNameplates.ContainsKey(entity.ObjectId)) 
                {
                    
                    if(entity.HasConditionEffect(ConditionEffects.Stasis))
                        continue;
                    if(entity.HasConditionEffect(ConditionEffects.Invisible))
                        continue;
                    if(entity.HasConditionEffect(ConditionEffects.Invincible))
                        continue;
                    if(entity.HasConditionEffect(ConditionEffects.Paused))
                        continue;
                    
                    bool drawName = false;
                    if (!(entity is Player)) {
                        if(entity.MinimumHp <= 0 )
                            continue;
                    }
                    
                    bool drawHp = true;
                    if (Map.LocalPlayer != null) {
                        drawName = Map.LocalPlayer != entity && entity is Player;
                    }
                    
                    var nameplate = new Nameplate(entity, drawName, drawHp, false);
                    _activeNameplates[entity.ObjectId] = nameplate;
                    AddChild(nameplate);
                }
            }

            var toRemove = new List<int>();
            foreach (var kvp in _activeNameplates) 
            {
                if (!Map.Entities.ContainsKey(kvp.Key)) 
                {
                    RemoveChild(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove) 
            {
                _activeNameplates.Remove(id);
            }
        }
    }
}