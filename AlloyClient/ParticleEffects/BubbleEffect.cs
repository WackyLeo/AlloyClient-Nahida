using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct BubbleData {
    public readonly double StartTime;
    public readonly float Speed;
    public readonly float FreqX;
    public readonly float FreqY;
    public double TimeLeft;

    public BubbleData(double startTime, float speed, float freqX, float freqY, double timeLeft) {
        StartTime = startTime;
        Speed = speed;
        FreqX = freqX;
        FreqY = freqY;
        TimeLeft = timeLeft;
    }
}

public class BubbleEffect : ParticleEffect {
    private const float SpreadDamper = 0.0025f;
    private const int Buffer = 50;
    private readonly Vector4 _color;
    private readonly BubbleData[] _data = new BubbleData[Buffer];
    private readonly Entity _entity;

    private readonly ParticleData[] _particles = new ParticleData[Buffer];

    private readonly EffectProperties _props;
    private readonly float _rate;
    private int _count;
    private double _lastUpdate = -1;

    public BubbleEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _props = props;
        // rate_ = (1 - rate) * 400 + 1  (maps 0-1 normalised rate → 1-401 ms period)
        _rate = (1f - props.Rate) * 400f + 1f;
        _color = GetColor(props.Color);
    }

    public override bool Update(double time, double dt) {
        if (_lastUpdate < 0) _lastUpdate = time;

        _position = _entity.Position;
        var newBucket = (int)(time / _rate);
        var oldBucket = (int)(_lastUpdate / _rate);

        for (var b = oldBucket; b < newBucket; b++) {
            if (_count == Buffer) break;
            var r = Random.Shared.NextSingle();
            var speed = (_props.Speed - _props.Speed * (r * (1f - _props.SpeedVariance))) * 10f;

            var freqX = 0f;
            var freqY = 0f;
            if (_props.Spread > 0) {
                freqX = Random.Shared.NextSingle() * _props.Spread - 0.1f;
                freqY = Random.Shared.NextSingle() * _props.Spread - 0.1f;
            }

            var lifeMs = _props.Life * 1000f - _props.Life * 1000f * (r * _props.LifeVariance);
            var angle = Random.Shared.NextSingle() * MathF.PI;
            var dist = Random.Shared.NextSingle() * 0.4f;
            var px = _position.X + dist * MathF.Cos(angle);
            var py = _position.Y + dist * MathF.Sin(angle);
            var size = (75f + Random.Shared.NextSingle() * 50f) / 100f;

            _data[_count] = new BubbleData(time, speed, freqX, freqY, lifeMs);
            _particles[_count] = new ParticleData(new Vector3(px, py, 0f), _color);
            _particles[_count].Position.W = size;
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

            var t = (float)((time - data.StartTime) / 1000.0);
            particle.Position.Z = data.Speed * t;

            if (_props.Spread > 0) {
                particle.Position.X += data.FreqX * (float)dt * SpreadDamper;
                particle.Position.Y += data.FreqY * (float)dt * SpreadDamper;
            }
        }

        Map.AddParticles(_particles, _count);
        _lastUpdate = time;
        return true;
    }
}