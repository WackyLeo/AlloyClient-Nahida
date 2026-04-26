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
    private readonly TypeEffects _effects;
    private readonly TypeHpBar _hpBar;
    private readonly TypeLootPreview _lootPreview;

    private TypeName _name;

    public TypeGameObject(Entity entity) {
        Entity = entity;
        SetTexture(entity.GetTexture());
        Extra = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade);
        
        _name = new TypeName(this, entity);
        _hpBar = new TypeHpBar(this, entity);
        _effects = new TypeEffects(this, entity);

        if (entity.Properties.Container) _lootPreview = new TypeLootPreview(this, entity);

        Color = Color.Black;
    }

    public override ModelType ModelType => ModelType.PbObject;
    public override bool HasShadow => true;

    public override void SetPosition(float x, float y, float z = 0) {
        Position.X = x;
        Position.Y = y;
        Position.Z = z;
    }

    public override void SetVisibility(bool visible) {
        Visible = visible;
    }

    public override void SetDepth(float depth) {
        base.SetDepth(depth);
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

    public override void SetName(string name) {
        _name = new TypeName(this, Entity, name);
    }

    public override void Draw() {
        var s = MathF.Sin(-Entity.Rotation);
        var c = MathF.Cos(-Entity.Rotation);
        var k = Entity.Size / 100f;
        var f = Entity.Flipped ? 1f : -1f;

        Rotation = new Vector4(s, c, k, f);

        Entity.HeightOffset = Scale.Y * k + Scale.W * k;
        Render.DrawEntity(new VertexObject(Position, UV, Scale, Rotation, Extra.Data, Color));

        var y = 0.1f;

        _lootPreview?.Draw(-Entity.HeightOffset - 0.7f);

        if (Entity.Properties.Static) return;

        if (Entity.HasConditionEffect(ConditionEffects.Invincible) ||
            Entity.HasConditionEffect(ConditionEffects.Invisible) ||
            Entity.HasConditionEffect(ConditionEffects.Stasis) ||
            Entity.HasConditionEffect(ConditionEffects.Paused))
            return;

        if (Entity.Properties.ShowName) {
            _name.Draw(y);
            y += _name.Height;
        }

        if (Entity.MaxHp != 0) _hpBar.Draw(y + 0.05f);

        _effects.Draw(-Entity.HeightOffset * 2);
    }

    public override void DrawShadow() {
        if (Entity.Properties.ShadowSize == 0) return;
        Render.DrawShadow(new ShadowData(Position.Xy, Entity.Properties.ShadowSize / 100.0f, Color.Black));
    }
}