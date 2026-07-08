using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Engine.Common;
using AlloyClient.Rendering;
using AlloyClient.Rendering.VertexData;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class XMLParticle : ParticleEffect, IDrawableParticleEffect {
    private readonly float _dx;
    private readonly float _dy;
    private readonly ParticleProperties _particleProps;
    private readonly EffectProperties _props;
    private readonly AtlasData _texture;

    private readonly float _z;
    private float _durationLeft;

    public XMLParticle(Vector2 startPos, EffectProperties props) {
        _position = startPos;
        _props = props;
        _durationLeft = props.Life;
        _z = props.ZOffset;

        var angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        _dx = MathF.Cos(angle) * 0.5f * props.MoveSpeed;
        _dy = MathF.Sin(angle) * 0.5f * props.MoveSpeed;

        if (!string.IsNullOrEmpty(props.Particle) &&
            ParticleLibrary.TryGet(props.Particle, out var particleProps)) {
            _particleProps = particleProps;
            _texture = Main.Atlas.GetAtlasData(particleProps.TextureFile, particleProps.TextureIndex);
            _durationLeft = particleProps.Duration;
            _z = particleProps.Z;
        }
    }

    public override bool Update(double time, double dt) {
        var dtSec = (float)(dt / 1000.0);
        _durationLeft -= dtSec;
        if (_durationLeft <= 0) return false;

        _position.X += _dx * dtSec;
        _position.Y += _dy * dtSec;
        return true;
    }

    public void Draw() {
        if (_texture.RawW() == 0) return;

        var uv = _texture.ToVector4();
        var w = _texture.RawW() - AtlasConfig.Padding * 2;
        var h = _texture.RawH() - AtlasConfig.Padding * 2;

        var padW = 1.0f + AtlasConfig.Padding * 3 / _texture.RawW();
        var padH = 1.0f + AtlasConfig.Padding * 3 / _texture.RawH();

        var sizeMult = _particleProps != null && _particleProps.Size > 0 ? _particleProps.Size / 100f : 1f;
        var widthScale = 0.75f * (w / 8f) * padW * sizeMult;
        var heightScale = 0.75f * (h / 8f) * padH * sizeMult;
        var scale = new Vector4(widthScale, heightScale, 0f, 0f);

        var rotation = new Vector4(0f, 1f, 0f, -1f);
        var extra = new ExtraData(RenderConfig.TypeEffect, RenderConfig.Shade) { Alpha = 1f };

        var pos3 = new Vector3(_position.X, _position.Y, _z);
        Render.DrawEntity(new VertexObject(pos3, uv, scale, rotation, extra.Data, Color.Black));
    }
}
