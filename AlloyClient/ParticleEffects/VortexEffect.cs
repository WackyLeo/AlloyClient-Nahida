using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct VortexParticleData {
    public float InitAccelX;
    public float InitAccelY;
    public float MaxSize;
    public float Size;

    public VortexParticleData(float initAccelX, float initAccelY, float maxSize) {
        InitAccelX = initAccelX;
        InitAccelY = initAccelY;
        MaxSize = maxSize;
        Size = 0f;
    }
}

public class VortexEffect : ParticleEffect {
    private const int Buffer = 180;
    private const float G = 4f;
    private readonly uint _color;
    private readonly VortexParticleData[] _data = new VortexParticleData[Buffer];
    private readonly Entity _entity;
    private readonly ParticleData[] _particles = new ParticleData[Buffer];
    private readonly float _spawnRate;
    private int _count;
    private double _spawnRemainder;

    public VortexEffect(Entity entity, EffectProperties props) {
        _entity = entity;
        _color = props.Color == 0 ? 0x270028u : props.Color;
        _spawnRate = props.Amount > 0 ? props.Amount : 100f;
    }

    public override bool Update(double time, double dt) {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
            return false;

        _position = _entity.Position;
        var toSpawn = _spawnRate * (dt / 1000.0) + _spawnRemainder;
        var spawnCount = (int)toSpawn;
        _spawnRemainder = toSpawn - spawnCount;

        for (var i = 0; i < spawnCount && _count < Buffer; i++) {
            var angle = Random.Shared.NextSingle() * MathF.Tau;
            var x = _position.X + MathF.Cos(angle) * 6f;
            var y = _position.Y + MathF.Sin(angle) * 6f;

            var tangent = MathF.Atan2(_position.X - x, _position.Y - y) + MathF.PI / 2f - MathF.PI / 6f;
            var accel = 2.5f + 0.5f * Random.Shared.NextSingle();
            var initX = MathF.Sin(tangent) * accel;
            var initY = MathF.Cos(tangent) * accel;

            _data[_count] = new VortexParticleData(initX, initY, 3.5f + 2f * Random.Shared.NextSingle());
            _particles[_count] = new ParticleData(new Vector3(x, y, 1f), GetColor(_color)) {
                Position = { W = 0f }
            };
            _count++;
        }

        var dtScale = (float)(dt * 0.0006);
        for (var i = _count - 1; i >= 0; i--) {
            ref var d = ref _data[i];
            ref var p = ref _particles[i];

            var angleToCenter = MathF.Atan2(_position.X - p.Position.X, _position.Y - p.Position.Y);
            var pullX = MathF.Sin(angleToCenter) * G;
            var pullY = MathF.Cos(angleToCenter) * G;

            if (d.Size < d.MaxSize)
                d.Size += (float)dt * 0.01f;

            p.Position.X += (pullX + d.InitAccelX) * dtScale;
            p.Position.Y += (pullY + d.InitAccelY) * dtScale;
            p.Position.Z -= 0.5f * dtScale;
            p.Position.W = ParticleW((int)(d.Size * 100f));

            if (p.Position.Z <= 0f) {
                _particles[i] = _particles[_count - 1];
                _data[i] = _data[_count - 1];
                _count--;
            }
        }

        if (_count > 0)
            Map.AddParticles(_particles, _count);

        return true;
    }
}
