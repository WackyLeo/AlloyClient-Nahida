using System;
using AlloyClient.Engine.Common;
using AlloyClient.Game;
using AlloyClient.Rendering;
using AlloyClient.Rendering.VertexData;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class RayCastParticle : ParticleEffect, IDrawableParticleEffect {
    private readonly AtlasData _texture;
    private readonly float _angle;
    private readonly int _size;
    private float _angleCorrection = 1f;
    private readonly Vector2 _pos;
    private readonly float _alphaDecayPerMs;

    private bool _needsDraw;

    public RayCastParticle(AtlasData texture, Vector2 pos, float angle, float angleCorrection, int size, float alphaDecay) {
        _texture = texture;
        _pos = pos;
        _position = pos;
        _angle = angle;
        _angleCorrection = angleCorrection;
        _size = size;
        _alphaDecayPerMs = (alphaDecay / 100f) / 16f;
    }

    public override bool Update(double time, double dt) {
        Alpha -= (float)(dt * _alphaDecayPerMs);
        if (Alpha <= 0) return false;
        _needsDraw = true;
        return true;
    }

    public void Draw() {
        if (!_needsDraw) return;
        _needsDraw = false;

        var uv = _texture.ToVector4();
        var w = _texture.RawW() - AtlasConfig.Padding * 2;
        var h = _texture.RawH() - AtlasConfig.Padding * 2;
        var padW = 1.0f + AtlasConfig.Padding * 3 / _texture.RawW();
        var padH = 1.0f + AtlasConfig.Padding * 3 / _texture.RawH();
        var widthScale  = 0.75f * (w / 8f) * padW;
        var heightScale = 0.75f * (h / 8f) * padH;
        var scale = new Vector4(widthScale, heightScale, 0f, 0f);

        var drawAngle = _angle + Camera.CameraAngle + _angleCorrection;
        var s = MathF.Sin(-drawAngle);
        var c = MathF.Cos(-drawAngle);
        var k = _size / 100f;
        var rotation = new Vector4(s, c, k, -1f);

        var extra = new ExtraData(RenderConfig.TypeGameObject, RenderConfig.Shade);
        extra.Alpha = Alpha;

        var pos3 = new Vector3(_pos.X, _pos.Y, 0.5f);
        
        // Pass Alpha into the Color struct so the shader correctly fades the outline
        var color = new Color( (byte)0,  (byte)0,  (byte)0, (byte)(Alpha * 255));
        Render.DrawEntity(new VertexObject(pos3, uv, scale, rotation, extra.Data, color));
    }
}
