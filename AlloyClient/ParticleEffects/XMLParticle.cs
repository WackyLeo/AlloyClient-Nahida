using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Engine.Common;
using AlloyClient.Rendering;
using AlloyClient.Rendering.VertexData;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class XMLParticle : ParticleEffect {
    private readonly float _dx;
    private readonly float _dy;

    private readonly EffectProperties _props;
    private readonly int _size;
    private readonly float _z;
    private float _durationLeft;
    private bool _needsDraw;
    private AtlasData _texture;

    public XMLParticle(Vector2 startPos, EffectProperties props) {
        _position = startPos;
        _props = props;

        _durationLeft = props.Life;
        _size = props.Size;
        _z = props.ZOffset;

        var angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        _dx = MathF.Cos(angle) * 0.5f * props.MoveSpeed;
        _dy = MathF.Sin(angle) * 0.5f * props.MoveSpeed;

        var atlasArray = Main.Atlas.GetAtlasData(props.Particle);
        if (atlasArray.Length > 0) _texture = atlasArray[Random.Shared.Next(atlasArray.Length)];
    }

    public override bool Update(double time, double dt) {
        var dtSec = (float)(dt / 1000.0);
        _durationLeft -= dtSec;

        if (_durationLeft <= 0) return false;
        _position.X += _dx * dtSec;
        _position.Y += _dy * dtSec;

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

        var sizeMult = _size / 100f;
        var widthScale = 0.75f * (w / 8f) * padW * sizeMult;
        var heightScale = 0.75f * (h / 8f) * padH * sizeMult;
        var scale = new Vector4(widthScale, heightScale, 0f, 0f);

        var rotation = new Vector4(0f, 1f, 0f, -1f);
        var extra = new ExtraData(RenderConfig.TypeEffect, RenderConfig.Shade) {
            Alpha = 1f
        };

        var pos3 = new Vector3(_position.X, _position.Y, _z);
        Render.DrawEntity(new VertexObject(pos3, uv, scale, rotation, extra.Data, Color.Black));
    }
}