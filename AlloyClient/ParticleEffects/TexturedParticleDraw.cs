using System;
using AlloyClient.Engine.Common;
using AlloyClient.Rendering;
using AlloyClient.Rendering.VertexData;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal static class TexturedParticleDraw {
    public static void DrawBillboard(AtlasData texture, Vector3 position, float sizeMultiplier, float alpha = 1f, float rotation = 0f) {
        if (texture.RawW() == 0 || texture.RawH() == 0 || alpha <= 0f) return;

        var uv = texture.ToVector4();
        var w = texture.RawW() - AtlasConfig.Padding * 2;
        var h = texture.RawH() - AtlasConfig.Padding * 2;

        var padW = 1.0f + AtlasConfig.Padding * 3 / texture.RawW();
        var padH = 1.0f + AtlasConfig.Padding * 3 / texture.RawH();

        var widthScale = 0.75f * (w / 8f) * padW;
        var heightScale = 0.75f * (h / 8f) * padH;
        var scale = new Vector4(widthScale, heightScale, 0f, 0f);

        var s = MathF.Sin(-rotation);
        var c = MathF.Cos(-rotation);
        var k = MathF.Max(0.01f, sizeMultiplier);
        var rot = new Vector4(s, c, k, -1f);

        var extra = new ExtraData(RenderConfig.TypeEffect, RenderConfig.Shade) { Alpha = alpha };
        var color = new Color((byte)0, (byte)0, (byte)0, (byte)Math.Clamp(alpha * 255f, 0f, 255f));
        Render.DrawEntity(new VertexObject(position, uv, scale, rot, extra.Data, color));
    }
}
