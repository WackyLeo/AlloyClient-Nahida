using AlloyClient.Assets;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public sealed class TypeGroundObject : RenderBase {

    public override ModelType ModelType => ModelType.PbGroundObject; 
    public override bool HasShadow => false;

    public TypeGroundObject(Entity entity) {
        Entity = entity;
        Extra = new ExtraData(0f, 0f);
        SetTexture(entity.GetTexture());
    }

    public override void SetPosition(float x, float y, float z = 0) {
        Position.X = (int)x;
        Position.Y = (int)y;
        Position.Z = 0f;
    }

    public override void SetTexture(AtlasData texture, bool attackFrame) {
        texture.RemovePadding();
        UV = texture.ToVector4();
    }

    public override void SetVisibility(bool visible) => Visible = visible;

    public override void SetDepth(float depth) {
        
    }

    public override void SetAlpha(float alpha) {
        
    }

    public override void SetName(string name) {
        
    }

    public override void Draw() {
        // Position.xy = tile corner, zw = 0 (no uv scroll)
        // Animate = zero (no wave/flow)
        // Mask.x = -1 (no mask blending)
        Render.DrawGroundObject(new TileData(
            new Vector4(Position.X, Position.Y, 0f, 0f),
            UV,
            Vector4.Zero,
            new Vector4(-1f, 0f, 0f, 0f)
        ));
    }
}