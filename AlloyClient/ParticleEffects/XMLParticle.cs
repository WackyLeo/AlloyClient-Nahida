using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class XMLParticle : ParticleEffect, IDrawableParticleEffect {
    private readonly float _dx;
    private readonly float _dy;
    private readonly ParticleProperties _particleProps;
    private readonly EffectProperties _props;
    private readonly Common.Structs.AtlasData _baseTexture;

    private readonly float _z;
    private float _durationLeft;
    private float _elapsed;
    private bool _needsDraw;

    public XMLParticle(Vector2 startPos, EffectProperties props) {
        _position = startPos;
        _props = props;
        _durationLeft = Math.Max(0.01f, props.Life);
        _z = props.ZOffset;

        if (!string.IsNullOrEmpty(props.Particle) &&
            ParticleLibrary.TryGet(props.Particle, out var particleProps)) {
            _particleProps = particleProps;
            _baseTexture = Main.Atlas.GetAtlasData(particleProps.TextureFile, particleProps.TextureIndex);
            _durationLeft = particleProps.Duration > 0 ? particleProps.Duration : _durationLeft;
            _z = particleProps.Z;
        }

        var angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        var moveSpeed = props.MoveSpeed > 0 ? props.MoveSpeed : 0.5f;
        _dx = MathF.Cos(angle) * 0.5f * moveSpeed;
        _dy = MathF.Sin(angle) * 0.5f * moveSpeed;
    }

    public override bool Update(double time, double dt) {
        var dtSec = (float)(dt / 1000.0);
        _elapsed += dtSec;
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

        var texture = GetTexture();
        var sizeMult = _particleProps != null && _particleProps.Size > 0 ? _particleProps.Size / 100f : 1f;
        TexturedParticleDraw.DrawBillboard(texture, new Vector3(_position.X, _position.Y, _z), sizeMult);
    }

    private Common.Structs.AtlasData GetTexture() {
        if (_particleProps == null || _particleProps.Frames.Count == 0)
            return _baseTexture;

        var total = 0f;
        foreach (var frame in _particleProps.Frames)
            total += Math.Max(0.001f, frame.Time);

        var t = total > 0f ? _elapsed % total : 0f;
        foreach (var frame in _particleProps.Frames) {
            var frameTime = Math.Max(0.001f, frame.Time);
            if (t <= frameTime)
                return Main.Atlas.GetAtlasData(frame.File, frame.Index);
            t -= frameTime;
        }

        return _baseTexture;
    }
}
