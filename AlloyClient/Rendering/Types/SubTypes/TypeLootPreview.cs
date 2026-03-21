using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using AlloyClient.State;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types.SubTypes;

public class TypeLootPreview : SubRenderBase {

    public override float Height => Size * 2.0f;
    private const float Size = 0.35f;
    
    private const ushort EmptySlotType = 0xCFC; 

    public TypeLootPreview(RenderBase parent, Entity entity) {
        Parent = parent;
        Entity = entity;
        UV = new Vector4();
        Scale = new Vector4(Size, Size, 0, 0);
        Rotation = new Vector4(0, 1, 1f, -1);
        Extra = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade); 
    }

    public override void Draw(float yOffset) {
        var items = Entity.Equipment;
        if (items == null) return;

        var pos = Parent.Position;
        
        var s = MathF.Sin(-Settings.CameraAngle);
        var c = MathF.Cos(-Settings.CameraAngle);

        for (var i = 0; i < 8; i++) {
            if (i >= items.Length) break;
            
            var item = items[i];
            
            ushort renderType = item?.ObjectType ?? EmptySlotType;
            if (item?.ObjectType == 0) {
                renderType = EmptySlotType;
            }

            if (renderType == 0 || !ObjectLibrary.TypeToTextureData.TryGetValue(renderType, out var texData)) {
                continue; 
            }
            
            var icon = texData.GetTexture();

            int col = i % 4;
            int row = i / 4;

            float spacingX = Size * 1.25f; 
            float numX = (1.5f - col) * spacingX; 

            var wx = numX * c;
            var wy = numX * s;

            var p = new Vector3(pos.X - wx, pos.Y + wy, pos.Z);
            float spacingY = Size * 1.25f;
            float rowOffset = (0.5f - row) * spacingY; 
            float currentLift = yOffset - rowOffset;
            
            var sc = new Vector4(Size, Size, 0, currentLift);

            Render.DrawEntity(new VertexObject(p, icon.ToVector4(), sc, Rotation, Extra.Data, Color.Black));
        }
    }
}