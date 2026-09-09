using System;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.ParticleEffects;

internal struct LevelUpParticleData
{
    public readonly float RingOffset;
    public readonly float PhaseOffset;

    public LevelUpParticleData(float ringOffset, float phaseOffset)
    {
        RingOffset = ringOffset;
        PhaseOffset = phaseOffset;
    }
}

public class LevelUpEffect : ParticleEffect
{
    private const double LifetimeMs = 2000.0;
    private const int CountPerRing = 100;
    private const float SizeScale = 0.4f;

    private readonly uint _color;
    private readonly Entity _entity;

    private readonly LevelUpParticleData[] _dataRing1 = new LevelUpParticleData[CountPerRing];
    private readonly LevelUpParticleData[] _dataRing2 = new LevelUpParticleData[CountPerRing];
    private readonly ParticleData[] _particles = new ParticleData[CountPerRing * 2];

    private double _elapsed;
    private bool _initialized;

    public LevelUpEffect(Entity entity, uint color)
    {
        _entity = entity;
        _color = color;
    }

    public override bool Update(double time, double dt)
    {
        if (_entity == null || !Map.Entities.ContainsKey(_entity.ObjectId))
        {
            return false;
        }

        if (!_initialized)
        {
            for (var i = 0; i < CountPerRing; i++)
            {
                var ringOffset = i / (float)CountPerRing;
                _dataRing1[i] = new LevelUpParticleData(ringOffset, 0f);
                _dataRing2[i] = new LevelUpParticleData(ringOffset, MathF.PI);

                _particles[i] = new ParticleData(Vector3.Zero, GetColor(_color)) { Position = { W = SizeScale } };
                _particles[CountPerRing + i] = new ParticleData(Vector3.Zero, GetColor(_color)) { Position = { W = SizeScale } };
            }
            _initialized = true;
        }

        _elapsed += dt;
        var progress = (float)(_elapsed / LifetimeMs);
        if (progress >= 1f)
        {
            return false;
        }

        var liveCount = UpdateSwirl(_dataRing1, 0, progress);
        liveCount += UpdateSwirl(_dataRing2, CountPerRing, progress);

        if (liveCount > 0)
        {
            Map.AddParticles(_particles, _particles.Length);
        }

        return true;
    }


    private int UpdateSwirl(LevelUpParticleData[] data, int particleOffset, float progress)
    {
        var liveCount = 0;

        for (var i = 0; i < data.Length; i++)
        {
            ref var d = ref data[i];
            ref var particle = ref _particles[particleOffset + i];

            var z = (progress * 2f - 1f) + d.RingOffset;

            if (z < 0f || z > 1f)
            {
                particle.Color.W = -0.001f;
                continue;
            }

            var theta = (2f * MathF.PI * d.RingOffset) + (2f * MathF.PI * progress) + d.PhaseOffset;
            var x = _entity.Position.X + 0.5f * MathF.Cos(theta);
            var y = _entity.Position.Y + 0.5f * MathF.Sin(theta);

            particle.Position.X = x;
            particle.Position.Y = y;
            particle.Position.Z = z;
            particle.Color.W = -1f;

            liveCount++;
        }

        return liveCount;
    }
}