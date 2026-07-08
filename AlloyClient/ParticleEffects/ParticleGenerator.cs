using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

public class ParticleGenerator : ParticleEffect, IDrawableParticleEffect {
    private const int Buffer = 2000;

    private readonly GeneratorParticle[] _data = new GeneratorParticle[Buffer];
    private readonly Entity _parent;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];

    private readonly EffectProperties _props;
    private readonly bool _textured;
    private readonly AtlasData _texture;
    private int _count;

    private double _lastUpdate = -1;
    private double _spawnRemainder;

    public ParticleGenerator(Entity parent, EffectProperties props) {
        _parent = parent;
        _props = props;

        if (!string.IsNullOrEmpty(props.BitmapFile)) {
            _texture = Main.Atlas.GetAtlasData(props.BitmapFile, props.BitmapIndex);
            _textured = _texture.RawW() > 0 && _texture.RawH() > 0;
        }
    }

    public override bool Update(double time, double dt) {
        if (!Map.Entities.ContainsKey(_parent.ObjectId)) return false;

        _position = _parent.Position;
        if (_lastUpdate < 0) _lastUpdate = time;

        var dtSec = dt / 1000.0;
        var toSpawn = _props.Rate * dtSec + _spawnRemainder;
        var spawnCount = (int)toSpawn;
        _spawnRemainder = toSpawn - spawnCount;

        for (var i = 0; i < spawnCount && _count < Buffer; i++) {
            var life = Math.Max(0.01f, _props.Life + _props.LifeVariance * (2f * Random.Shared.NextSingle() - 1f));
            var spdX = _props.Speed + _props.SpeedVariance * (2f * Random.Shared.NextSingle() - 1f);
            var spdY = _props.Speed + _props.SpeedVariance * (2f * Random.Shared.NextSingle() - 1f);
            var rise = _props.Rise + _props.RiseVariance * (2f * Random.Shared.NextSingle() - 1f);

            var spawnX = _position.X + _props.RangeX * (2f * Random.Shared.NextSingle() - 1f);
            var spawnY = _position.Y + _props.RangeY * (2f * Random.Shared.NextSingle() - 1f);
            var spawnZ = _parent.Z + _props.ZOffset + 0.75f;

            _data[_count] = new GeneratorParticle(life, life, spdX, spdY, rise, spawnX, spawnY, spawnZ);

            if (!_textured) {
                var color = GetColor(RandomColor(_props.Color, _props.Color2));
                _particles[_count] = new ParticleData(new Vector3(spawnX, spawnY, spawnZ), color);
                _particles[_count].Position.W = Math.Max(0.05f, _props.Size / 8f);
            }

            _count++;
        }

        for (var i = _count - 1; i >= 0; i--) {
            ref var d = ref _data[i];
            d.TimeLeft -= (float)dtSec;

            if (d.TimeLeft <= 0) {
                _data[i] = _data[_count - 1];
                if (!_textured) _particles[i] = _particles[_count - 1];
                _count--;
                continue;
            }

            d.SpdZ += _props.RiseAcc * (float)dtSec;
            d.X += d.SpdX * (float)dtSec;
            d.Y += d.SpdY * (float)dtSec;
            d.Z += d.SpdZ * (float)dtSec;

            if (!_textured) {
                ref var part = ref _particles[i];
                part.Position.X = d.X;
                part.Position.Y = d.Y;
                part.Position.Z = d.Z;
            }
        }

        if (!_textured)
            Map.AddParticles(_particles, _count);

        _lastUpdate = time;
        return true;
    }

    public void Draw() {
        if (!_textured || _count <= 0) return;

        var sizeMult = _props.Size > 0 ? _props.Size / 100f : 1f;
        sizeMult = Math.Clamp(sizeMult, 0.05f, 4f);

        for (var i = 0; i < _count; i++) {
            ref readonly var d = ref _data[i];
            var ageT = 1f - Math.Clamp(d.TimeLeft / Math.Max(d.Life, 0.001f), 0f, 1f);
            var alpha = 1f - ageT * ageT;
            TexturedParticleDraw.DrawBillboard(_texture, new Vector3(d.X, d.Y, d.Z), sizeMult, alpha);
        }
    }
}

internal struct GeneratorParticle {
    public float TimeLeft;
    public float Life;
    public float SpdX;
    public float SpdY;
    public float SpdZ;
    public float X;
    public float Y;
    public float Z;

    public GeneratorParticle(float timeLeft, float life, float spdX, float spdY, float rise,
        float x, float y, float z) {
        TimeLeft = timeLeft;
        Life = life;
        SpdX = spdX;
        SpdY = spdY;
        SpdZ = rise;
        X = x;
        Y = y;
        Z = z;
    }
}
