using System;
using AlloyClient.Assets;
using AlloyClient.Engine.Common;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public sealed class TypeProjectile : RenderBase {
    public float RandDir;
    public float TempLt;
    public float Lifetime;
    public float Height;
    public float FallAngle;
    public bool IsFalldown;
    public bool IsMissile;

    public Vector2 SpawnPosition;
    private Vector2 _missileLastPos;

    public TypeProjectile() {
        Extra = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade);
    }

    public void Reset(float staticFallAngle) {
        Extra = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade);
        Visible = false;
        InvisProj = false;
        Position = Vector3.Zero;
        UV = Vector4.Zero;
        Scale = Vector4.Zero;
        Rotation = Vector4.Zero;
        Color = Color.Transparent;
        RotationAngle = 0f;
        Size = 100;
        Entity = null;
        Height = 40f;;
        FallAngle = staticFallAngle > -9999f ? staticFallAngle : Random.Shared.NextSingle() * 2f - 1f;
        IsFalldown = false;
        IsMissile = false;
        SpawnPosition = Vector2.Zero;
        _missileLastPos = Vector2.Zero;
    }

    public override ModelType ModelType => ModelType.PbObject;
    public override bool HasShadow => true;

    public override void SetPosition(float x, float y, float z = 0) {
        Position.X = x;
        Position.Y = y;
        Position.Z = z;
    }

    public override void SetVisibility(bool visible) => Visible = visible;
    public override void SetDepth(float depth) => Extra.SortId = depth;
    public override void SetAlpha(float alpha) => Extra.Alpha = alpha;
    public override void SetName(string name) { }

    public override void Draw() {
        if (IsFalldown || IsMissile) {
            DrawWithHeight();
            return;
        }

        var s = MathF.Sin(-RotationAngle);
        var c = MathF.Cos(-RotationAngle);
        var k = Size / 100f;
        Rotation = new Vector4(s, c, k, -1f);
        Render.DrawEntity(new VertexObject(Position, UV, Scale, Rotation, Extra.Data, Color));
    }

    private void DrawWithHeight() {
    float offsetX = 0f, offsetY = 0f;
    float tiltAngle;

    if (IsMissile) {
        var ranAimExtra = MathF.Abs(MathF.Sin(TempLt / Lifetime * MathF.PI)) * 3f;
        var dxSpawn = Position.X - SpawnPosition.X;
        var dySpawn = Position.Y - SpawnPosition.Y;
        var dir = -MathF.Atan2(dxSpawn, dySpawn) - MathF.PI / 2f;

        var dist = MathF.Sqrt(dxSpawn * dxSpawn + dySpawn * dySpawn);
        offsetX = dist * (1f - TempLt / Lifetime) * MathF.Cos(dir)
                  + ranAimExtra * MathF.Cos(MathF.PI * (RandDir / 360f) * 2f);
        offsetY = dist * (1f - TempLt / Lifetime) * MathF.Sin(dir)
                + ranAimExtra * MathF.Sin(MathF.PI * (RandDir / 360f) * 2f);

        var renderPos = new Vector3(
            Position.X + Height * 0.40f * FallAngle * MathF.Cos(Camera.CameraAngle) + offsetX,
            Position.Y + Height * 0.40f * FallAngle * MathF.Sin(Camera.CameraAngle) + offsetY,
            Height
        );
        
        var tiltX = renderPos.X + MathF.Cos(Camera.CameraAngle - MathF.PI / 2f) * Height;
        var tiltY = renderPos.Y + MathF.Sin(Camera.CameraAngle - MathF.PI / 2f) * Height;
        tiltAngle = -(MathF.Atan2(tiltX - _missileLastPos.X, tiltY - _missileLastPos.Y))
                    + MathF.PI
                    - RotationAngle
                    - Camera.CameraAngle;

        _missileLastPos = new Vector2(tiltX, tiltY);

        var s = MathF.Sin(-tiltAngle);
        var c = MathF.Cos(-tiltAngle);
        var k = Size / 100f;
        Render.DrawEntity(new VertexObject(renderPos, UV, Scale, new Vector4(s, c, k, -1f), Extra.Data, Color));
    } else {
        var renderPos = new Vector3(
            Position.X + Height * FallAngle * MathF.Cos(Camera.CameraAngle),
            Position.Y + Height * FallAngle * MathF.Sin(Camera.CameraAngle),
            Position.Z
        );

        tiltAngle = 0.35f * FallAngle - Camera.CameraAngle;

        var s = MathF.Sin(-tiltAngle);
        var c = MathF.Cos(-tiltAngle);
        var k = Size / 100f;
        Render.DrawEntity(new VertexObject(renderPos, UV, Scale, new Vector4(s, c, k, -1f), Extra.Data, Color));
    }
}

    public override void DrawShadow() {
        Render.DrawShadow(new ShadowData(Position.Xy, 0.5f, Color.Black));
    }

    public override void SetTexture(AtlasData texture, bool attackFrame) {
        UV = texture.ToVector4();
        var padW = 1.0f + AtlasConfig.Padding * 3 / texture.RawW();
        var padH = 1.0f + AtlasConfig.Padding * 3 / texture.RawH();
        Scale = new Vector4(0.75f * padW, 0.75f * padH, 0f, 0f);
    }
}