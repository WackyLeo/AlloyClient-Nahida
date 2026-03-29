using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct GasData {
    public readonly float Dx, Dy;
    public readonly float Noise;
    public readonly float InitialSize;
    public readonly double LifeTime;
    public double TimeLeft;

    public GasData(float dx, float dy, float noise, float initialSize, double lifeTime) {
        Dx = dx;
        Dy = dy;
        Noise = noise;
        InitialSize = initialSize;
        LifeTime = lifeTime;
        TimeLeft = lifeTime;
    }
}

public class GasEffect : ParticleEffect {
    private const int Buffer = 60;
    private readonly Vector4 _color;
    private readonly GasData[] _data = new GasData[Buffer];
    private readonly Entity _entity;

    private readonly ParticleData[] _particles = new ParticleData[Buffer];

    private readonly EffectProperties _props;

    private int _count;
    private double _spawnRemainder;

    public GasEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _props = props;
        _color = GetColor(props.Color);
    }

    public override bool Update(double time, double dt) {
        _position = _entity.Position;

        var toSpawn = 50 * (dt / 1000.0) + _spawnRemainder;
        var intSpawn = (int)toSpawn;
        _spawnRemainder = toSpawn - intSpawn;

        for (var s = 0; s < intSpawn; s++) {
            if (_count == Buffer) break;

            var r = Random.Shared.NextSingle();
            var size = (r + 0.3f) * 200f;
            var velMag = _props.Speed - _props.Speed * (r * (1f - _props.SpeedVariance));
            var dx = (Random.Shared.NextSingle() * 2f - 1f) * velMag;
            var dy = (Random.Shared.NextSingle() * 2f - 1f) * velMag;
            var lifeMs = _props.Life * 1000.0 - _props.Life * 1000.0 * (r * _props.LifeVariance);

            _data[_count] = new GasData(dx, dy, _props.Spread, size / 100f, lifeMs);
            _particles[_count] = new ParticleData(new Vector3(_position, 0.75f), _color);
            _particles[_count].Position.W = size / 100f;
            _count++;
        }

        for (var i = _count - 1; i >= 0; i--) {
            ref var data = ref _data[i];
            ref var particle = ref _particles[i];

            data.TimeLeft -= dt;
            if (data.TimeLeft <= 0) {
                _particles[i] = _particles[_count - 1];
                _data[i] = _data[_count - 1];
                _count--;
                continue;
            }

            var noiseX = Random.Shared.NextSingle() * data.Noise;
            var noiseY = Random.Shared.NextSingle() * data.Noise;
            particle.Position.X += data.Dx * noiseX * (float)(dt / 1000.0);
            particle.Position.Y += data.Dy * noiseY * (float)(dt / 1000.0);
            particle.Position.W = (float)(data.TimeLeft / data.LifeTime) * data.InitialSize;
        }

        Map.AddParticles(_particles, _count);
        return true;
    }
}