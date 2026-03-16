using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.ParticleEffects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.Ui.Character;
using AlloyClient.Utils;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public sealed class Projectile {

    private const double HitTestDelayMs = 16;

    private readonly float _jitter = Random.Shared.NextSingle() * 0.00002f - 0.00001f;

    private int _bulletId;
    private int _damage;
    private float _angle;
    private int _ownerId;
    private bool _damagePlayers;
    private ObjectProperties _objDesc;
    private ProjectileProperties _projDesc;
    private double _startTime;
    private float _angleCorrection;
    private Vector2 _startPosition;
    private Vector2 _position;

    private float _spinAngle;
    private float _startSpinAngle;
    private float _spinDistance;
    private float _accel;
    
    public float Rotation;
    public int Size;
    public bool Clear;

    private readonly HashSet<int> _hitEntities = [];
    public RenderBase RenderBaseType;
    private double _lastHitTest;
    
    private bool _rayCasted;
    private bool _rayCastFailed;
    public void Reset(int id, int dmg, float angle, Entity entity, ObjectProperties objDesc, ProjectileProperties projDesc) {
        _bulletId = id;
        _damage = dmg;
        _angle = angle;
        _ownerId = entity.ObjectId;
        _damagePlayers = entity is not Player;
        _objDesc = objDesc;
        _projDesc = projDesc;
        Clear = false;

        Size = _projDesc.Size > 0 ? _projDesc.Size : 100;

        _startTime = Main.GameTime.TotalMs;
        _lastHitTest = 0;
        _accel = 0;
        _angleCorrection = _objDesc.AngleCorrection * MathF.PI / 4;
        _position = _startPosition = entity.Position;

        _spinDistance = _projDesc.StartRotationRadius;
        _startSpinAngle = _projDesc.DynamicRotationAngle ? _angle : 0f;
        if (_projDesc.FixedRotationAngle != 0) {
            if (_bulletId % 2 == 0 && !(_projDesc.OnlyClockwise || _projDesc.OnlyCounterClockwise))
                _startSpinAngle -= _projDesc.FixedRotationAngle * MathF.PI / 180f;
            else
                _startSpinAngle += _projDesc.FixedRotationAngle * MathF.PI / 180f;
        }
        _spinAngle = 0f;

        _hitEntities.Clear();
        RenderBaseType = new TypeProjectile();
        RenderBaseType.SetSize(Size);
        RenderBaseType.SetTexture(GetTexture());
    }
    
    public void SetStartPosition(Vector2 pos) {
        _startPosition = pos;
        _position = pos;
        RenderBaseType.SetPosition(pos.X, pos.Y);
    }

    public AtlasData GetTexture() {
        var textureData = ObjectLibrary.TypeToTextureData[_objDesc.ObjectType];
        return textureData.HasAnimationData ? textureData.AnimatedTextures.FaceRight[0] : textureData.GetTexture();
    }

    public bool Update(double time, double dt, DepthMatrix matrix) {
        if (Clear) return false;

        var elapsed = (float)(time - _startTime);
        var curLifetime = elapsed - _projDesc.ProjDelayMs;
        if (_projDesc.CheckRayCast && !_rayCasted) {
            _rayCastFailed = !RayCastCheck();
            _rayCasted = true;
        }
        if (_rayCastFailed) return false;

        if (elapsed < _projDesc.ProjDelayMs) {
            RenderBaseType.SetVisibility(false);
            return true;
        }
        if (curLifetime > _projDesc.LifetimeMs) return false;
        if (_projDesc.RayCast && !_rayCasted) {
            _rayCasted = true;
            RayCastHit(time);
            return false;
        }

        if (curLifetime > _projDesc.LifetimeMs)
            return false;

        UpdateVisibility(matrix);

        var newPos = PositionAt(curLifetime);

        if (_objDesc.Rotation != 0) {
            Rotation = elapsed / _objDesc.Rotation;
        } else if (!_projDesc.NoRotation) {
            var direction = newPos - _position;
            var angle = MathF.Atan2(direction.Y, direction.X);
            Rotation = angle + Camera.CameraAngle + _angleCorrection;
        }

        var doHitTest = time - _lastHitTest >= HitTestDelayMs;

        bool blocked;
        if (_projDesc.Falldown || _projDesc.Missile)
            blocked = false;
        else
            blocked = !MoveTo(newPos);

        if (blocked || (doHitTest && HitTest(time)))
            return false;

        if (!blocked) {
            _position = newPos;
            RenderBaseType.SetPosition(newPos.X, newPos.Y);
            RenderBaseType.SetRotation(Rotation);
        }

        if (_projDesc.ParticleTrail && doHitTest) {
            Map.AddParticleEffect(new SparkEffect(100, _projDesc.ParticleTrailColor, _projDesc.ParticleTrailLifetime, 0.5f,
                MathUtils.RandomPlusMinus(3), MathUtils.RandomPlusMinus(3), newPos.X, newPos.Y));
        }

        return true;
    }

    
    private void UpdateVisibility(DepthMatrix matrix) {
        var dx = _position.X - Camera.Position.X;
        var dy = _position.Y + Camera.Position.Y;
        var distanceSquared = dx * dx + dy * dy;
        const int r2 = Map.TileRenderDistance * Map.TileRenderDistance;
        RenderBaseType.SetVisibility(distanceSquared <= r2);
        var sort = _position.X * matrix.M12 + _position.Y * matrix.M22 + matrix.M42;
        RenderBaseType.SetDepth(0.5f + 0.4f * sort + _jitter);
    }

    private bool MoveTo(Vector2 pos) {
        var tile = Map.GetTile(pos);
        if (tile == null || tile.Type == 0xFF)
            return false;

        if (tile.OccupiedObject != null && !_projDesc.PassAll) {
            var obj = tile.OccupiedObject.Properties;
            if ((!obj.IsEnemy || _damagePlayers) && (obj.EnemyOccupySquare || !_projDesc.PassesCover && obj.OccupySquare))
                return false;
        }

        _position = pos;
        RenderBaseType.SetPosition(pos.X, pos.Y);
        return true;
    }

    private bool HitTest(double time) {
        _lastHitTest = time;

        if (_damagePlayers) {
            var target = EntityUtils.GetClosestPlayer(_position, 0.5f);
            if (target == null || _hitEntities.Contains(target.ObjectId))
                return false;

            Map.AddParticleEffect(new HitEffect(target, 0xFF0000));
            NotificationLayer.AddStatusText(target, $"-{_damage}", 0xFF0000, 1000, 0);
            Map.PendingPlayerHit.AddHit(_bulletId, _ownerId);

            if (!_projDesc.MultiHit)
                return true;

            _hitEntities.Add(target.ObjectId);
            return false;
        }

        var enemy = EntityUtils.GetClosestEnemy(_position, 0.5f);
        if (enemy == null || _hitEntities.Contains(enemy.ObjectId))
            return false;

        enemy.Hp -= _damage; 
        bool killed = enemy.Hp <= 0;

        Map.AddParticleEffect(new HitEffect(enemy, 0xFF0000));
        NotificationLayer.AddStatusText(enemy, $"-{_damage}", 0xFF0000, 1000, 0);
    
        Map.PendingEnemyHit.AddHit((int)time, _bulletId, enemy.ObjectId, killed);
        Logger.Info($"AddHit called: bulletId={_bulletId}, targetId={enemy.ObjectId}, killed={killed}");
    
        if (!_projDesc.MultiHit)
            return true;

        _hitEntities.Add(enemy.ObjectId);
        return false;
    }

    private Vector2 PositionAt(float curLifetime) {
        var pos = _startPosition;

        var accelTime = curLifetime - _projDesc.AccelerationDelay;
        if (accelTime >= 0)
            _accel = accelTime * (_projDesc.Acceleration / 5f);
        _accel = Math.Clamp(_accel, _projDesc.MinAcceleration, _projDesc.MaxAcceleration);

        var baseDistance = curLifetime * _projDesc.Speed;
        baseDistance += accelTime >= 0 ? accelTime * (_accel / 10000f) : 0f;

        float phase;
        if (_projDesc.Asyncrone)
            phase = ((_bulletId + 2) % 3) == 0 ? 0 : MathF.PI;
        else
            phase = (_bulletId % 2) * MathF.PI;

        if (_projDesc.Wavy) {
            var period = 6 * MathF.PI;
            var amp = MathF.PI / 64f;
            var theta = _angle + amp * MathF.Sin(phase + period * curLifetime / 1000f);
            pos.X += baseDistance * MathF.Cos(theta);
            pos.Y += baseDistance * MathF.Sin(theta);
        }
        else if (_projDesc.Parametric) {
            var t = curLifetime / _projDesc.LifetimeMs * 2 * MathF.PI;
            float x, y;
            if (_projDesc.NoAlt) {
                x = MathF.Sin(2 * t) * -1f;
                y = MathF.Sin(2 * t) * -1f;
            } else {
                x = MathF.Sin(t) * (_bulletId % 2 == 0 ? -1 : 1);
                y = MathF.Sin(2 * t) * (_bulletId % 4 < 2 ? 1 : -1);
            }
            pos.X += (x * MathF.Cos(_angle) - y * MathF.Sin(_angle)) * _projDesc.Magnitude;
            pos.Y += (x * MathF.Sin(_angle) + y * MathF.Cos(_angle)) * _projDesc.Magnitude;
        }
        else if (_projDesc.Boomerang) {
            var halfDist = _projDesc.LifetimeMs * _projDesc.Speed / 2f;
            var d = baseDistance > halfDist ? halfDist - (baseDistance - halfDist) : baseDistance;
            pos.X += d * MathF.Cos(_angle);
            pos.Y += d * MathF.Sin(_angle);
            if (_projDesc.Amplitude != 0) {
                var deflection = _projDesc.Amplitude * MathF.Sin(phase + curLifetime / _projDesc.LifetimeMs * _projDesc.Frequency * 2 * MathF.PI);
                pos.X += deflection * MathF.Cos(_angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(_angle + MathF.PI / 2);
            }
        }
        else {
            pos.X += baseDistance * MathF.Cos(_angle);
            pos.Y += baseDistance * MathF.Sin(_angle);
            if (_projDesc.Amplitude != 0) {
                var deflection = _projDesc.Amplitude * MathF.Sin(
                    phase + (curLifetime + _projDesc.LifetimeMs * (_projDesc.StartFrequency / _projDesc.Frequency))
                    / _projDesc.LifetimeMs * _projDesc.Frequency * 2 * MathF.PI);
                pos.X += deflection * MathF.Cos(_angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(_angle + MathF.PI / 2);
            }
        }

        if (_projDesc.Orbiting) {
            _spinDistance = curLifetime * (_projDesc.RadiusIncreaseSpeed / 200f) + _projDesc.StartRotationRadius;
            if (_spinDistance > _projDesc.MaxRotationRadius && _projDesc.MaxRotationRadius > 0)
                _spinDistance = _projDesc.MaxRotationRadius;

            if ((_bulletId % 2 == 0 || _projDesc.OnlyClockwise) && !_projDesc.OnlyCounterClockwise)
                _spinAngle = curLifetime * (_projDesc.RotationSpeed / 200f) + _startSpinAngle;
            else
                _spinAngle = -(curLifetime * (_projDesc.RotationSpeed / 200f)) + _startSpinAngle;

            pos.X += _spinDistance * MathF.Cos(_spinAngle);
            pos.Y += _spinDistance * MathF.Sin(_spinAngle);
        }

        return pos;
    }
    
    private bool RayCastCheck() {
    for (var t = 0; t <= _projDesc.LifetimeMs + _projDesc.ProjDelayMs; t += 25) {
        var pos = PositionAt(t);
        var tile = Map.GetTile(pos);
        if (tile == null || tile.Type == 0xFF)
            return false;
        if (tile.OccupiedObject != null && !_projDesc.PassAll) {
            var obj = tile.OccupiedObject.Properties;
            if (obj.EnemyOccupySquare || (!_projDesc.PassesCover && obj.OccupySquare))
                return false;
        }
    }
    return true;
}

private void RayCastHit(double time) {
    for (var t = 0; t <= _projDesc.LifetimeMs; t += 50) {
        var pos = PositionAt(t);

        var tile = Map.GetTile(pos);
        if (tile == null || tile.Type == 0xFF) {
            if (_damagePlayers)
            break;
        }

        if (tile.OccupiedObject != null && !_projDesc.PassAll) {
            var obj = tile.OccupiedObject.Properties;
            if (obj.EnemyOccupySquare || (!_projDesc.PassesCover && obj.OccupySquare))
                break;
        }
        
        Map.AddParticleEffect(new SparkEffect(100, 0x00D100, 500, 0.5f, 0, 0, pos.X, pos.Y));

        if (_damagePlayers) {
            var target = EntityUtils.GetClosestPlayer(pos, 0.5f);
            if (target != null && !_hitEntities.Contains(target.ObjectId)) {
                Map.PendingPlayerHit.AddHit(_bulletId, _ownerId);
                if (!_projDesc.MultiHit) return;
                _hitEntities.Add(target.ObjectId);
            }
        } else {
            var enemy = EntityUtils.GetClosestEnemy(pos, 0.5f);
            if (enemy != null && !_hitEntities.Contains(enemy.ObjectId)) {
                Map.PendingEnemyHit.AddHit((int)time, _bulletId, enemy.ObjectId, enemy.Hp <= _damage);
                Logger.Info($"AddHit called: bulletId={_bulletId}, targetId={enemy.ObjectId}");
                if (!_projDesc.MultiHit) return;
                _hitEntities.Add(enemy.ObjectId);
            }
        }
    }
}
}