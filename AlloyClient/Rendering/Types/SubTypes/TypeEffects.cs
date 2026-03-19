using System;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Rendering.VertexData;
using AlloyClient.State;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types.SubTypes;

public class TypeEffects : SubRenderBase {

    public override float Height => Size * 2.5f;

    private const float Size = 0.28f;

    public TypeEffects(RenderBase parent, Entity entity) {
        Parent   = parent;
        Entity   = entity;
        UV       = new Vector4();
        Scale    = new Vector4(Size, Size, 0, 0);
        Rotation = new Vector4(0, 1, 1f, -1);
        Extra    = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade);
    }

    public override void Draw(float yOffset) {
        var effects = Entity.ConditionEffects & ~ConditionEffectUtil.IconlessEffects;
        if (effects == 0) return;

        var pos   = Parent.Position;
        var count = System.Numerics.BitOperations.PopCount((ulong)effects);
        var totalWidth = (count - 1) * Size * 1.35f;
        var num = totalWidth / 2f;

        var s = MathF.Sin(-Settings.CameraAngle);
        var c = MathF.Cos(-Settings.CameraAngle);

        for (var i = 0; i < 64; i++) {
            var bit = (ConditionEffects)(1ul << i);
            if ((effects & bit) == 0) continue;
            if (!ConditionEffectUtil.EffectIcons.TryGetValue(bit, out var icons)) {
                num -= Size * 2f;
                continue;
            }
            
            var wx = num * c;
            var wy = num * s;

            var p = new Vector3(pos.X - wx, pos.Y + wy, pos.Z);
        
            float lift = 0.15f;
            var sc = new Vector4(Size, Size, 0, yOffset - lift);

            Render.DrawEntity(new VertexObject(p, icons[0], sc, Rotation, Extra.Data, Color.Black));
            num -= Size * 1.35f;
        }
    }
}