using System;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public sealed class ThrownProjectileEffect : ParticleEffect, IDrawableParticleEffect {
    private const float LifetimeMs = 1500f;

    private readonly Vector2 _start;
    private readonly Vector2 _end;
    private readonly ushort _objectType;
    private readonly Common.Structs.AtlasData _texture;
    private float _elapsedMs;
    private float _spin;
    private bool _needsDraw;
    private Vector3 _drawPosition;

    public ThrownProjectileEffect(uint objectType, Vector2 start, Vector2 end) {
        _objectType = (ushort)objectType;
        _start = start;
        _end = end;
        _position = start;

        if (ObjectLibrary.TypeToTextureData.TryGetValue(_objectType, out var textureData))
            _texture = textureData.GetTexture(true);
    }

    public override bool Update(double time, double dt) {
        _elapsedMs += (float)dt;
        if (_elapsedMs >= LifetimeMs) return false;

        var t = Math.Clamp(_elapsedMs / LifetimeMs, 0f, 1f);
        _position = Vector2.Lerp(_start, _end, t);

        var z = MathF.Sin((1f - t) * MathF.PI) * 2f;
        _drawPosition = new Vector3(_position.X, _position.Y, z);

        _spin += (float)(dt / 1000.0) * 12f;
        _needsDraw = true;
        return true;
    }

    public void Draw() {
        if (!_needsDraw) return;
        _needsDraw = false;
        TexturedParticleDraw.DrawBillboard(_texture, _drawPosition, 0.8f, 1f, _spin + Camera.CameraAngle);
    }
}
