using System;
using AlloyClient.Assets;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Rendering.Types.SubTypes;
using AlloyClient.Rendering.VertexData;
using Common;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public sealed class TypeGameObject : RenderBase {
    
    public override ModelType ModelType => ModelType.PbObject;
    public override bool HasShadow => true;

    private readonly TypeName _name;
    private readonly TypeHpBar _hpBar;
    private readonly TypeEffects _effects;
    private readonly TypeLootPreview _lootPreview;

    public TypeGameObject(Entity entity) {
        Entity = entity;
        SetTexture(entity.GetTexture());
        Extra = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade);
        
        _name = new TypeName(this, entity);
        _hpBar = new TypeHpBar(this, entity);
        _effects = new TypeEffects(this, entity);
        
        if (entity.Properties.Container) {
            _lootPreview = new TypeLootPreview(this, entity);
            if (IsBigBag(entity.Type)) {
                entity.Size = 120;
            }
        }

        Color = Color.Black;
    }
    
    public override void SetPosition(float x, float y, float z = 0) {
        Position.X = x; Position.Y = y; Position.Z = z;
    }
    
    public override void SetVisibility(bool visible) => Visible = visible;

    public override void SetDepth(float depth) {
        Extra.SortId = depth;
        _name.SetDepth(depth);
        _hpBar.SetDepth(depth);
        _effects.SetDepth(depth);
        _lootPreview?.SetDepth(depth);
    }
    
    public override void SetAlpha(float alpha) {
        Extra.Alpha = alpha;
        _name.SetAlpha(alpha);
        _hpBar.SetAlpha(alpha);
        _effects.SetAlpha(alpha);
        _lootPreview?.SetAlpha(alpha);
    }

    public override void SetName(string name) { }

    public override void Draw() {
        var s = MathF.Sin(-Entity.Rotation);
        var c = MathF.Cos(-Entity.Rotation);
        var k = Entity.Size / 100f;
        var f = Entity.Flipped ? 1f : -1f;
        Rotation = new Vector4(s, c, k, f);
        
        Entity.HeightOffset = -1 * Scale.Y * k + Scale.W * k;
        
        Render.DrawEntity(new VertexObject(Position, UV, Scale, Rotation, Extra.Data, Color));
        
        _lootPreview?.Draw(Entity.HeightOffset);

        if (Entity.Properties.Static) return;
        
        if (Entity.HasConditionEffect(ConditionEffects.Invincible) ||
            Entity.HasConditionEffect(ConditionEffects.Invisible) ||
            Entity.HasConditionEffect(ConditionEffects.Stasis) ||
            Entity.HasConditionEffect(ConditionEffects.Paused)) {
            return;
        }
        
        var y = 0.1f;

        if (Entity.Properties.ShowName) {
            _name.Draw(y);
            y += _name.Height;
        }
        
        if (Entity.MaxHp != 0) {
            _hpBar.Draw(y + 0.05f);
        }
        
        _effects.Draw(Entity.HeightOffset);
    }

    public override void DrawShadow() {
        if (Entity.Size == 0) return;
        Render.DrawShadow(new ShadowData(Position.Xy, 1f, Color.Black));
    }
    
    private static bool IsBigBag(ushort type) {
        return type switch {
            0x0509 or 0x050B or 0x050C or 0x050E or 0x050F or 0x0510 or 
                0x040D or 0x1591 or 0x1595 or 0x15AB or 0x1606 or 0x04F4 or 
                0x04F5 or 0x042D or 0x0427 or 0x04F6 or 0x04F7 or 0x04F8 or 
                0x1596 or 0x1597 or 0x158F or 0x15AC or 0x1607 => true,
            _ => false
        };
    }
}