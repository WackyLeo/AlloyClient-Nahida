using System;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types.SubTypes;

public class TypeHpBar : SubRenderBase {
    public override float Height => 0.12f * 2;

    public TypeHpBar(RenderBase parent, Entity entity) {
        Parent = parent;
        Entity = entity;
        UV = new Vector4();
        Scale = new Vector4(1.14f, 0.38f, 0, 0); 
        Rotation = new Vector4(0, 1, 1, -1);
        Extra = new ExtraData(RenderConfig.TypeBar, RenderConfig.NoShade);
    }

    public override void Draw(float yOffset) {
        if (Entity.MaxHp <= 0 || Entity.HasConditionEffect(ConditionEffects.Invincible)) return;

        float fill = Math.Clamp(1f * Entity.Hp / Entity.MaxHp, 0f, 1f);
    
        Color = fill < 0.5f ? (fill >= 0.2f ? new Color(255, 128, 16, 255) : new Color(224, 16, 16, 255)) : new Color(16, 255, 0, 255);
    
        Scale.W = yOffset;
        var obj = new VertexObject(Parent.Position, UV, Scale, Rotation, Extra.Data, Color);
    
        obj.Mask1 = new Vector4(40f, 6f, 0.5f, fill); 
        Render.DrawEntity(obj);
    }
}