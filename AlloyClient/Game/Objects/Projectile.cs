using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Objects.Util;
using AlloyClient.ParticleEffects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.Ui.Character;
using AlloyClient.Utils;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public sealed class Projectile {
    private const double HitTestDelayMs = 16;

    private readonly HashSet<int> _hitEntities = [];

    private readonly float _jitter = Random.Shared.NextSingle() * 0.00002f - 0.00001f;
    private float _accel;
    private float _addLifetime;
    private float _angle;
    private float _angleCorrection;

    private int _bulletId;
    private bool _checkRayCasted;
    private int _damage;
    private bool _damagePlayers;
    private double _lastHitTest;
    private float _lifetime;
    private ObjectProperties _objDesc;
    public Entity _owner;
    private int _ownerId;
    private Vector2 _position;
    private int _projDelayMs;
    private ProjectileProperties _projDesc;

    private bool _rayCasted;
    private bool _rayCastFailed;
    private bool _rayCastHitDone;

    private int _ricochetsLeft;
    private Vector2 _savePos;

    private float _spinAngle;
    private float _spinDistance;
    private Vector2 _startPosition;
    private float _startSpinAngle;
    private double _startTime;
    public bool Clear;
    public RenderBase RenderBaseType;

    public float Rotation;
    public int Size;

    public void Reset(int id, int dmg, float angle, Entity entity, ObjectProperties objDesc,
                      ProjectileProperties projDesc) {
        _bulletId = id;
        _damage = dmg;
        _angle = angle;
        _ownerId = entity.ObjectId;
        _owner = entity;
        _damagePlayers = entity is not Player;
        _objDesc = objDesc;
        _projDesc = projDesc;
        _rayCasted = false;
        _checkRayCasted = false;
        _rayCastHitDone = false;
        _rayCastFailed = false;
        Clear = false;
        _savePos = Vector2.Zero;
        _lifetime = _projDesc.LifetimeMs;
        _addLifetime = 0;
        _projDelayMs = _projDesc.ProjDelayMs;

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
        _ricochetsLeft = _projDesc.Ricochet;

        _hitEntities.Clear();
        RenderBaseType = new TypeProjectile();
        RenderBaseType.SetSize(Size);
        RenderBaseType.SetTexture(GetTexture());
    }

    public void SetStartPosition(Vector2 pos) {
        _startPosition = pos;
        _position = pos;

        RenderBaseType.SetPosition(pos.X, pos.Y, 0.5f);
    }

    public void AddProjDelay(int ms) {
        _projDelayMs += ms;
    }

    public AtlasData GetTexture(float curLifetime = 0) {
        var textureData = ObjectLibrary.TypeToTextureData[_objDesc.ObjectType];

        if (_projDesc.Animated && _projDesc.AnimationTime > 0) {
            float altAnim = _bulletId % 2 == 0 ? _projDesc.AltAnimation : 0;
            var frameIndex = (int)((curLifetime + altAnim) / _projDesc.AnimationTime);

            if (textureData.HasAnimationData && textureData.AnimatedTextures.FaceRight != null) {
                var frames = textureData.AnimatedTextures.FaceRight;
                return frames[frameIndex % frames.Length];
            }

            if (textureData.RandomTextures != null && textureData.RandomTextures.Length > 0) {
                var randomSeq = textureData.RandomTextures;
                return randomSeq[frameIndex % randomSeq.Length].GetTexture();
            }
        }

        return textureData.GetTexture();
    }

    public AtlasData GetTexture() {
        var textureData = ObjectLibrary.TypeToTextureData[_objDesc.ObjectType];
        return textureData.HasAnimationData ? textureData.AnimatedTextures.FaceRight[0] : textureData.GetTexture();
    }

    public bool Update(double time, double dt, DepthMatrix matrix) {
        if (Clear) return false;

        var elapsed = (float)(time - _startTime);

        if (_projDesc.CheckRayCast && !_checkRayCasted) {
            _rayCastFailed = !RayCastCheck();
            _checkRayCasted = true;
        }

        if (_rayCastFailed) return false;

        if (elapsed < _projDelayMs) {
            RenderBaseType.SetVisibility(false);
            return true;
        }

        var curLifetime = elapsed - _projDelayMs;

        if (curLifetime > _lifetime) return false;

        if (_projDesc.RayCast && !_rayCastHitDone) {
            _rayCastHitDone = true;
            RayCastHit(time);
            return false;
        }

        UpdateVisibility(matrix);

        var newPos = PositionAt(curLifetime + _addLifetime);

        if (_objDesc.Rotation != 0) {
            Rotation = elapsed / _objDesc.Rotation;
        } else if (_projDesc.ForceOrbiting || _projDesc.Orbiting) {
            Rotation = _spinAngle + Camera.CameraAngle + _angleCorrection;
        } else if (_projDesc.DynamicAngle) {
            if (_projDesc.Orbiting || _projDesc.ForceOrbiting) {
                Rotation = _spinAngle + Camera.CameraAngle + _angleCorrection;
            } else {
                if (_savePos == Vector2.Zero) _savePos = PositionAt(curLifetime - 20f);
                Rotation = -MathF.Atan2(newPos.X - _savePos.X, newPos.Y - _savePos.Y)
                           + (_objDesc.Rotation != 0 ? elapsed / _objDesc.Rotation : 0f)
                           + Camera.CameraAngle
                           + _angleCorrection
                           + 2f * MathF.PI / 4f;

                _savePos = _position;
            }
        } else if (!_projDesc.NoRotation) {
            var direction = newPos - _position;
            var angle = direction.LengthSquared > 0.000001f ? MathF.Atan2(direction.Y, direction.X) : _angle;
            Rotation = angle + Camera.CameraAngle + _angleCorrection;
        }

        var doHitTest = time - _lastHitTest >= HitTestDelayMs;

        bool blocked;
        if (_projDesc.Falldown || _projDesc.Missile)
            blocked = false;
        else
            blocked = !MoveTo(newPos);

        if (blocked || (doHitTest && HitTest(time)))
            //TriggerSplit(time);
            return false;

        if (!blocked) {
            _position = newPos;
            RenderBaseType.SetPosition(newPos.X, newPos.Y, 0.5f);
            RenderBaseType.SetRotation(Rotation);
            RenderBaseType.SetTexture(GetTexture(curLifetime));
        }

        //Render.DebugCircle(_position, 0.25f + _projDesc.BonusSize, new Vector3(0, 1, 0), 0.05f);
        //float worldAngle = Rotation - Camera.CameraAngle;
        //Render.DebugArrow(_position, worldAngle, 1f, new Vector3(1, 0, 0), 0.05f);

        // Projectile debug + hit pos

        if (_damage > 0) {
            var hitPos = Camera.WorldToDebug2D(new Vector3(_position.X, _position.Y, 0.5f));

            if (Render.DebugEnabled) {
                Render.DebugCircle(hitPos, 0.25f + _projDesc.BonusSize, new Vector3(0, 1, 0), 0.05f);
                var worldAngle = Rotation - Camera.CameraAngle;
                Render.DebugArrow(hitPos, worldAngle, 1f, new Vector3(1, 0, 0), 0.05f);
            }
        }

        if (_projDesc.ParticleTrail && doHitTest)
            Map.AddParticleEffect(new SparkEffect(100, _projDesc.ParticleTrailColor, _projDesc.ParticleTrailLifetime,
                                                  0.5f,
                                                  MathUtils.RandomPlusMinus(3), MathUtils.RandomPlusMinus(3), newPos.X,
                                                  newPos.Y));

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
            if ((!obj.IsEnemy || _damagePlayers) &&
                (obj.EnemyOccupySquare || (!_projDesc.PassesCover && obj.OccupySquare)))
                return false;
        }

        _position = pos;
        RenderBaseType.SetPosition(pos.X, pos.Y, 0.5f);
        return true;
    }

    public void MarkActive(double time) {
        _startTime = time;
    }

    private bool HitTest(double time) {
        _lastHitTest = time;
        var hitRadius = 0.5f + _projDesc.BonusSize;
        var checkPos = Camera.WorldToDebug2D(new Vector3(_position.X, _position.Y, 0.5f));

        if (_damagePlayers) {
            var target = EntityUtils.GetClosestPlayer(checkPos, hitRadius);
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

        var enemy = EntityUtils.GetClosestEnemy(checkPos, hitRadius);
        if (enemy == null || _hitEntities.Contains(enemy.ObjectId))
            return false;

        var hitRadiusSqr = hitRadius * hitRadius;
        var hitSomething = false;

        foreach (var kvp in Map.Entities) {
            var go = kvp.Value;

            if (go.Properties == null) continue;
            if (_damagePlayers && !go.Properties.IsPlayer) continue;
            if (!_damagePlayers && !go.Properties.IsEnemy) continue;
            if (_hitEntities.Contains(go.ObjectId)) continue;

            var goPos = Camera.WorldToDebug2D(new Vector3(go.Position.X, go.Position.Y, 0.5f));
            var distSqr = (goPos - checkPos).LengthSquared;

            if (distSqr > hitRadiusSqr) continue;

            if (_ricochetsLeft > 0) {
                _ricochetsLeft--;
                var chainTarget = GetChainTarget(go);

                if (chainTarget != null) {
                    _startPosition = _position;
                    var dx = chainTarget.Position.X - _position.X;
                    var dy = chainTarget.Position.Y - _position.Y;
                    _angle = MathF.Atan2(dy, dx);

                    _startTime = time;
                    _hitEntities.Clear();
                } else {
                    Clear = true;
                }

                return false;
            }

            if (enemy.HasConditionEffect(ConditionEffects.Invincible) ||
                enemy.HasConditionEffect(ConditionEffects.Stasis) ||
                enemy.HasConditionEffect(ConditionEffects.Paused))
                return false;

            var actualDamage = DamageWithDefense(_damage, go.Defense, _projDesc.ArmorPiercing, enemy);

            //go.Hp -= actualDamage; 
            var killed = go.Hp - actualDamage <= 0;

            if (actualDamage > 0) {
                Map.AddParticleEffect(new HitEffect(go, 0xFF0000));
                NotificationLayer.AddStatusText(go, $"-{actualDamage}", 0xFF0000, 1000, 0);
            }

            if (_damagePlayers)
                Map.PendingPlayerHit.AddHit(_bulletId, _ownerId);
            else
                Map.PendingEnemyHit.AddHit((int)time, _bulletId, go.ObjectId, false);

            _hitEntities.Add(go.ObjectId);
            hitSomething = true;

            if (!_projDesc.MultiHit) return true;
        }

        return !hitSomething && !_projDesc.MultiHit;
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
            phase = (_bulletId + 2) % 3 == 0 ? 0 : MathF.PI;
        else
            phase = _bulletId % 2 * MathF.PI;

        if (_projDesc.Wavy) {
            var period = 6 * MathF.PI;
            var amp = MathF.PI / 64f;
            var theta = _angle + amp * MathF.Sin(phase + period * curLifetime / 1000f);
            pos.X += baseDistance * MathF.Cos(theta);
            pos.Y += baseDistance * MathF.Sin(theta);
        } else if (_projDesc.Parametric) {
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
        } else if (_projDesc.Boomerang) {
            var halfDist = _projDesc.LifetimeMs * _projDesc.Speed / 2f;
            var d = baseDistance > halfDist ? halfDist - (baseDistance - halfDist) : baseDistance;
            pos.X += d * MathF.Cos(_angle);
            pos.Y += d * MathF.Sin(_angle);
            if (_projDesc.Amplitude != 0) {
                var deflection = _projDesc.Amplitude *
                                 MathF.Sin(phase + curLifetime / _projDesc.LifetimeMs * _projDesc.Frequency * 2 *
                                           MathF.PI);
                pos.X += deflection * MathF.Cos(_angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(_angle + MathF.PI / 2);
            }
        } else {
            pos.X += baseDistance * MathF.Cos(_angle);
            pos.Y += baseDistance * MathF.Sin(_angle);
            if (_projDesc.Amplitude != 0) {
                var deflection = _projDesc.Amplitude * MathF.Sin(
                                                                 phase + (curLifetime + _projDesc.LifetimeMs *
                                                                          (_projDesc.StartFrequency /
                                                                           _projDesc.Frequency))
                                                                 / _projDesc.LifetimeMs * _projDesc.Frequency * 2 *
                                                                 MathF.PI);
                pos.X += deflection * MathF.Cos(_angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(_angle + MathF.PI / 2);
            }
        }

        if (_projDesc.ForceOrbiting && _owner != null) {
            _spinDistance = curLifetime * (_projDesc.RadiusIncreaseSpeed / 200f) + _projDesc.StartRotationRadius;
            if (_spinDistance > _projDesc.MaxRotationRadius && _projDesc.MaxRotationRadius > 0)
                _spinDistance = _projDesc.MaxRotationRadius;

            if ((_bulletId % 2 == 0 || _projDesc.OnlyClockwise) && !_projDesc.OnlyCounterClockwise)
                _spinAngle = curLifetime * (_projDesc.RotationSpeed / 200f) + _startSpinAngle;
            else
                _spinAngle = -(curLifetime * (_projDesc.RotationSpeed / 200f)) + _startSpinAngle;

            pos.X = _owner.Position.X + _spinDistance * MathF.Cos(_spinAngle);
            pos.Y = _owner.Position.Y + _spinDistance * MathF.Sin(_spinAngle);
        } else if (_projDesc.Orbiting) {
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

    private int DamageWithDefense(int damage, int defense, bool nodef, Entity entity, int damageMulti = 0,
                                  bool isPlayer = false) {
        if (entity.HasConditionEffect(ConditionEffects.Invulnerable)) return 0;

        if (nodef || entity.HasConditionEffect(ConditionEffects.ArmorBroken))
            defense = 0;
        else if (entity.HasConditionEffect(ConditionEffects.Armored)) defense = defense * 2;

        var limit = damage * 0.15f; // Magic Number
        var ret = 0;

        if (damage - defense < limit)
            ret = (int)limit;
        else
            ret = damage - defense;

        if (entity.HasConditionEffect(ConditionEffects.Exposed)) ret += 20;
        if (entity.HasConditionEffect(ConditionEffects.Petrify)) ret = (int)(ret * 0.9f);
        if (entity.HasConditionEffect(ConditionEffects.Curse)) ret = (int)(ret * 1.2f);
        if (entity.HasConditionEffect(ConditionEffects.Protected)) ret = (int)(ret * (isPlayer ? 0.5f : 0.25f));
        return ret;
    }

    private bool RayCastCheck() {
        var life = _projDesc.RCSpeed > 0 ? _projDesc.RCLifetimeMs : _lifetime + _projDelayMs;

        for (var t = 0; t <= life; t += 25) {
            Vector2 pos;
            if (_projDesc.RCSpeed > 0)
                pos = PositionAtRayCastLine(t);
            else
                pos = PositionAt(t);

            var tile = Map.GetTile(pos);
            if (tile == null || tile.Type == 0xFF)
                return false;

            if (tile.OccupiedObject != null && !_projDesc.PassAll) {
                var obj = tile.OccupiedObject.Properties;
                if (!obj.IsEnemy && !obj.IsPlayer &&
                    (obj.EnemyOccupySquare || (!_projDesc.PassesCover && obj.OccupySquare))) {
                    if (_projDesc.ReturnRayCast) {
                        var trimmed = _lifetime - (t - 25);
                        _projDelayMs -= (int)(trimmed * (_projDelayMs / _projDesc.LifetimeMs));
                        _addLifetime = trimmed;
                        _lifetime -= trimmed;
                        return true;
                    }

                    return false;
                }
            }
        }

        return true;
    }

    private Vector2 PositionAtRayCastLine(float t) {
        var pos = _startPosition;
        var speed = _projDesc.RCSpeed;
        var dist = t * (speed / 10000f);
        pos.X += dist * MathF.Cos(_angle);
        pos.Y += dist * MathF.Sin(_angle);
        return pos;
    }

    private void RayCastHit(double time) {
        var texture = GetTexture();

        for (var t = 0; t <= _projDesc.LifetimeMs; t += 50) {
            var pos = PositionAt(t);

            var tile = Map.GetTile(pos);
            if (tile == null || tile.Type == 0xFF) {
                if (_damagePlayers) break;
                break;
            }

            if (tile.OccupiedObject != null && !_projDesc.PassAll) {
                var obj = tile.OccupiedObject.Properties;
                if (obj.EnemyOccupySquare || (!_projDesc.PassesCover && obj.OccupySquare))
                    break;
            }

            var angle = t == 0 ? _angle : MathF.Atan2(pos.Y - _startPosition.Y, pos.X - _startPosition.X);
            var projPos = Camera.WorldToDebug2D(new Vector3(pos.X, pos.Y, 0.5f));

            Map.AddParticleEffect(new RayCastParticle(texture, pos, angle, _angleCorrection, Size));
            if (_damagePlayers) {
                var target = EntityUtils.GetClosestPlayer(projPos, 0.5f);
                if (target != null && !_hitEntities.Contains(target.ObjectId)) {
                    Map.AddParticleEffect(new HitEffect(target, 0xFF0000));
                    NotificationLayer.AddStatusText(target, $"-{_damage}", 0xFF0000, 1000, 0);
                    Map.PendingPlayerHit.AddHit(_bulletId, _ownerId);
                    if (!_projDesc.MultiHit) return;
                    _hitEntities.Add(target.ObjectId);
                }
            } else {
                var enemy = EntityUtils.GetClosestEnemy(projPos, 0.5f);
                if (enemy != null && !_hitEntities.Contains(enemy.ObjectId)) {
                    Map.PendingEnemyHit.AddHit((int)time, _bulletId, enemy.ObjectId, enemy.Hp <= _damage);
                    Map.AddParticleEffect(new HitEffect(enemy, 0xFF0000));
                    NotificationLayer.AddStatusText(enemy, $"-{_damage}", 0xFF0000, 1000, 0);
                    if (!_projDesc.MultiHit) return;
                    _hitEntities.Add(enemy.ObjectId);
                }
            }
        }
    }

    private Entity GetChainTarget(Entity currentTarget) {
        Entity closest = null;
        var closestDistSqr = float.MaxValue;

        var maxChainDistSqr = _projDesc.Ricochet * 2f * (_projDesc.Ricochet * 2f);

        foreach (var kvp in Map.Entities) {
            var go = kvp.Value;
            if (go == currentTarget || go.Properties == null) continue;
            if (_damagePlayers && !go.Properties.IsPlayer) continue;
            if (!_damagePlayers && !go.Properties.IsEnemy) continue;

            var distSqr = (go.Position - _position).LengthSquared;

            if (distSqr <= maxChainDistSqr && distSqr < closestDistSqr) {
                closestDistSqr = distSqr;
                closest = go;
            }
        }

        return closest;
    }

    // Debuggin Tools
}