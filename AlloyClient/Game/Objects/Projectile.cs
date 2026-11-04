using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game.Components.Hud.Chat;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking.Packets.Incoming;
using AlloyClient.ParticleEffects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.State;
using AlloyClient.Ui.Character;
using AlloyClient.Utils;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public sealed class Projectile : IResettable
{
    private const double HitTestDelayMs = 16;
    private static readonly Logger Log = new(nameof(Projectile));

    private readonly HashSet<int> _hitEntities = [];

    private readonly float _jitter = Random.Shared.NextSingle() * 0.00002f - 0.00001f;
    private float _accel;
    private float _addLifetime;
    private float _angle;
    private float _angleCorrection;

    private int _projectileId;
    private bool _checkRayCasted;
    private int _damage;
    private bool _damagePlayers;
    private float _dirX;
    private float _dirY;
    private bool _fadeOff;
    private double _lastHitTest;
    private float _lifetime;
    private ObjectProperties _objDesc;
    public Entity _owner;
    private int _ownerId;
    private Vector2 _position;
    private int _projDelayMs;
    private ProjectileProperties _projProp;
    private int _textureId;
    private float _amplitudeVariance;

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
    
    public Dictionary<Entity, bool> _multiHitDict;
    public float _hitCoolDown = 0;
    public float hitCoolDownReset = 0;
    public float _saveCoolDown = 0;
    private int _lastHitCycle = 0;

    public float Rotation;
    public int Size;

    public void Reset()
    {
        _projectileId = 0;
        _damage = 0;
        _angle = 0f;
        _ownerId = 0;
        _owner = null;
        _damagePlayers = false;
        _objDesc = null;
        _projProp = null;
        _rayCasted = false;
        _checkRayCasted = false;
        _rayCastHitDone = false;
        _rayCastFailed = false;
        _fadeOff = false;
        Clear = false;
        _savePos = Vector2.Zero;
        _lifetime = 0;
        _addLifetime = 0;
        _projDelayMs = 0;
        Size = 0;
        _startTime = 0;
        _lastHitTest = 0;
        _accel = 0;
        _angleCorrection = 0f;
        _startPosition = Vector2.Zero;
        _position = Vector2.Zero;
        _spinDistance = 0f;
        _startSpinAngle = 0f;
        _spinAngle = 0f;
        _ricochetsLeft = 0;
        Rotation = 0f;
        _hitEntities.Clear();
        RenderBaseType = null;
        _multiHitDict = null;
        _textureId = 0;
    }

    public bool IsInPool { get; set; }

    public void Reset(int id, int dmg, float angle, Entity entity, ObjectProperties objDesc,
        ProjectileProperties projProp)
    {
        _projectileId = id;
        _damage = dmg;
        _angle = angle;
        _dirX = MathF.Cos(_angle);
        _dirY = MathF.Sin(_angle);
        _ownerId = entity.ObjectId;
        _owner = entity;
        _damagePlayers = entity is not Player;
        _objDesc = objDesc;
        _projProp = projProp;
        _rayCasted = false;
        _checkRayCasted = false;
        _rayCastHitDone = false;
        _rayCastFailed = false;
        _fadeOff = false;
        Clear = false;
        _savePos = Vector2.Zero;
        _lifetime = _projProp.LifetimeMs;
        _addLifetime = 0;
        _projDelayMs = _projProp.ProjDelayMs;
        Size = _projProp.Size != -1 ? _projProp.Size : 100;
        _amplitudeVariance = (float)(Random.Shared.NextDouble() * 2 - 1) * projProp.AmplitudeVariance;

        _startTime = Main.GameTime.TotalMs;
        _lastHitTest = 0;
        _accel = 0;
        _saveCoolDown = 0;
        _hitCoolDown = 0;
        _angleCorrection = _objDesc.AngleCorrection * MathF.PI / 4;
        _position = _startPosition = entity.Position;
        _multiHitDict = _projProp.MultiHit ? new Dictionary<Entity, bool>() : null;

        _spinDistance = _projProp.StartRotationRadius;
        _startSpinAngle = _projProp.Orbiting ? _angle : 0f;
        if (_projProp.FixedRotationAngle != 0)
        {
            if (_projectileId % 2 == 0 && !(_projProp.OnlyClockwise || _projProp.OnlyCounterClockwise))
                _startSpinAngle -= _projProp.FixedRotationAngle * MathF.PI / 180f;
            else
                _startSpinAngle += _projProp.FixedRotationAngle * MathF.PI / 180f;
        }

        _spinAngle = 0f;
        _ricochetsLeft = _projProp.Ricochet;

        _hitEntities.Clear();
        if (RenderBaseType is not TypeProjectile) RenderBaseType = ObjectPools.TypeProjectiles.Pop();

        ((TypeProjectile)RenderBaseType).Reset(_projProp.StaticFallAngle);
        ((TypeProjectile)RenderBaseType).IsFalldown = _projProp.Falldown;
        ((TypeProjectile)RenderBaseType).IsMissile = _projProp.Missile;
        ((TypeProjectile)RenderBaseType).SpawnPosition = entity.Position;

        ((TypeProjectile)RenderBaseType).RandDir = Random.Shared.NextSingle() * 360f;
        ((TypeProjectile)RenderBaseType).TempLt = 0f;
        ((TypeProjectile)RenderBaseType).Lifetime = _lifetime;
        
        // Get RandomTexture
        var textureData = ObjectLibrary.TypeToTextureData[_objDesc.ObjectType];
        _textureId = textureData.RandomTextures != null && textureData.RandomTextures.Length > 0
            ? _projectileId % textureData.RandomTextures.Length
            : 0;

        RenderBaseType.SetSize(Size);
        RenderBaseType.SetTexture(GetTexture());
    }

    public void SetStartPosition(Vector2 pos)
    {
        _startPosition = pos;
        _position = pos;

        RenderBaseType.SetPosition(pos.X, pos.Y, 0.5f);
    }

    public void AddProjDelay(int ms)
    {
        _projDelayMs += ms;
    }

    public AtlasData GetTexture(float curLifetime = 0)
    {
        var textureData = ObjectLibrary.TypeToTextureData[_objDesc.ObjectType];

        if (_projProp.Animated && _projProp.AnimationTime > 0)
        {
            float altAnim = _projectileId % 2 == 0 ? _projProp.AltAnimation : 0;
            var frameIndex = (int)((curLifetime + altAnim) / _projProp.AnimationTime);

            if (textureData.HasAnimationData && textureData.AnimatedTextures.FaceRight != null)
            {
                var frames = textureData.AnimatedTextures.FaceRight;
                return frames[frameIndex % frames.Length];
            }

            if (textureData.RandomTextures != null && textureData.RandomTextures.Length > 0)
            {
                var randomSeq = textureData.RandomTextures;
                return randomSeq[frameIndex % randomSeq.Length].GetTexture();
            }
        }

        return textureData.GetTexture(false, _textureId);
    }

    public AtlasData GetTexture()
    {
        var textureData = ObjectLibrary.TypeToTextureData[_objDesc.ObjectType];
        return textureData.HasAnimationData ? textureData.AnimatedTextures.FaceRight[0] : textureData.GetTexture(false, _textureId);
    }

    public bool Update(double time, double dt, DepthMatrix matrix)
    {
        if (Clear) return false;

        var elapsed = (float)(time - _startTime);

        if (_projProp.CheckRayCast && !_checkRayCasted)
        {
            _rayCastFailed = !RayCastCheck();
            _checkRayCasted = true;
        }

        if (_rayCastFailed) return false;

        if (elapsed < _projDelayMs)
        {
            RenderBaseType.SetVisibility(false);
            return true;
        }

        if (_projProp.RayCast)
        {
            if (!_rayCastHitDone)
            {
                RayCastHit(time);
                _rayCastHitDone = true;
            }

            return false;
        }

        var curLifetime = elapsed - _projDelayMs;

        if (curLifetime > _lifetime) return false;
        
        if (_projProp.MultiHit && _projProp.HitAmount > 0)
        {
            var hitInterval = _lifetime / _projProp.HitAmount;
            var hitCycle = (int)(curLifetime / hitInterval);
        
            if (hitCycle != _lastHitCycle)
            {
                _hitEntities.Clear();
                _lastHitCycle = hitCycle;
            }
        }
        
        float targetAlpha = (!_damagePlayers && _ownerId != Map.LocalPlayerId) ? Settings.AllyProjOpacity : 1.0f;
        if (targetAlpha <= 0f) return false;
        if (_projProp.Fade > 0)
        {
            if (!_fadeOff)
            {
                RenderBaseType.SetAlpha(0.2f * targetAlpha);
                if (curLifetime >= _projProp.Fade)
                {
                    _fadeOff = true;
                    RenderBaseType.SetAlpha(1.0f * targetAlpha);
                }
            }
        }

        /*
        if (_projProp.Fade > 0) {
            if (curLifetime < _projProp.Fade) {
                float fadeRatio = Math.Clamp(curLifetime / _projProp.Fade, 0f, 1f);
                RenderBaseType.SetAlpha(targetAlpha * fadeRatio);
            } else {
                _fadeOff = true;
                RenderBaseType.SetAlpha(targetAlpha);
            }
        } else {
            RenderBaseType.SetAlpha(targetAlpha);
        }*/

        if (_projProp.Fade > 0)
            if (!_fadeOff)
            {
                RenderBaseType.SetAlpha(0.2f);
                if (curLifetime >= _projProp.Fade)
                {
                    _fadeOff = true;
                    RenderBaseType.SetAlpha(1.0f);
                }
            }

        if (_projProp.Falldown)
        {
            ((TypeProjectile)RenderBaseType).Height = 40f - curLifetime / _lifetime * 40f;
        }
        else if (_projProp.Missile)
        {
            ((TypeProjectile)RenderBaseType).Height =
                MathF.Abs(MathF.Sin(curLifetime / _lifetime * MathF.PI)) * 3f
                + (0.8f - curLifetime / _lifetime * 0.8f);
            ((TypeProjectile)RenderBaseType).TempLt = curLifetime;
        }

        UpdateVisibility(ref matrix);

        var newPos = PositionAt(curLifetime + _addLifetime);
        if (_objDesc.Rotation != 0)
        {
            Rotation = elapsed / _objDesc.Rotation;
        }
        else if (_projProp.VanillaAngle)
        {
            Rotation = _angle + Camera.CameraAngle + _angleCorrection;
        }
        else if (_projProp.DynamicAngle)
        {
            if (_savePos == Vector2.Zero) _savePos = PositionAt(curLifetime - 20f);

            var dir = newPos - _savePos;
            Rotation = MathF.Atan2(dir.Y, dir.X)
                       + Camera.CameraAngle
                       + _angleCorrection;

            _savePos = _position;
        }
        else if (_projProp.ForceOrbiting || _projProp.Orbiting)
        {
            Rotation = _spinAngle + Camera.CameraAngle + _angleCorrection;
        }
        else if (!_projProp.NoRotation)
        {
            var direction = newPos - _position;
            var angle = direction.LengthSquared > 0.000001f ? MathF.Atan2(direction.Y, direction.X) : _angle;
            Rotation = angle + Camera.CameraAngle + _angleCorrection;
        }

        var doHitTest = time - _lastHitTest >= HitTestDelayMs;
        bool blocked;
        if (_projProp.Falldown || _projProp.Missile)
            blocked = false;
        else
            blocked = !MoveTo(newPos);

        var canHit = _projProp.Fade == 0 || _fadeOff;
        if (blocked || (canHit && doHitTest && HitTest(time)))
            return false;

        if (!blocked)
        {
            _position = newPos;
            RenderBaseType.SetPosition(newPos.X, newPos.Y, 0.5f);
            RenderBaseType.SetRotation(Rotation);
            RenderBaseType.SetTexture(GetTexture(curLifetime));
        }
        // Projectile debug + hit pos

        if (_damagePlayers && _damage <= 0 &&
            (_projProp.Effects == null || _projProp.Effects.Length == 0))
        {
            RenderBaseType.SetVisibility(false);
            return true;
        }

        var hitPos = Camera.WorldToDebug2D(new Vector3(_position.X, _position.Y, 0.5f));
        if (Render.DebugEnabled && _damagePlayers)
        {
            Render.DebugCircle(hitPos, 0.25f + _projProp.BonusSize, new Vector3(1, 0, 0), 0.05f);
            var worldAngle = Rotation - Camera.CameraAngle - _angleCorrection;

            if (_objDesc.Rotation != 0)
                worldAngle = _angle;
        }

        if (_projProp.ParticleTrail && doHitTest)
        {
            var spark = new SparkEffect(100, _projProp.ParticleTrailColor, _projProp.ParticleTrailLifetime, 0.5f,
                MathUtils.RandomPlusMinus(3), MathUtils.RandomPlusMinus(3), newPos.X, newPos.Y)
            {
                Alpha = targetAlpha
            };
            Map.AddParticleEffect(spark);
        }

        return true;
    }


    public void UpdateVisibility(ref DepthMatrix matrix)
    {
        var dx = _position.X - Camera.Position.X;
        var dy = _position.Y + Camera.Position.Y;
        var distanceSquared = dx * dx + dy * dy;
        var playerSightRadiusSquared = Map.LocalPlayer.UpdateRadius * Map.LocalPlayer.UpdateRadius;

        var visible = distanceSquared <= playerSightRadiusSquared;
        RenderBaseType.SetVisibility(visible);

        var sort = _position.X * matrix.M12 + _position.Y * matrix.M22 + matrix.M42;
        RenderBaseType.SetDepth(0.5f + 0.4f * sort + _jitter);
    }

    private bool MoveTo(Vector2 pos)
    {
        var tile = Map.GetTile(pos);
        if (tile == null || tile.Type == 0xFF)
            return false;

        if (tile.OccupiedObject != null && !_projProp.PassAll)
        {
            var obj = tile.OccupiedObject.Properties;
            if ((!obj.IsEnemy || _damagePlayers) &&
                (obj.EnemyOccupySquare || (!_projProp.PassesCover && obj.OccupySquare)))
                return false;
        }

        _position = pos;
        RenderBaseType.SetPosition(pos.X, pos.Y, 0.5f);
        return true;
    }

    public void MarkActive(double time)
    {
        _startTime = time;
    }

    private bool HitTest(double time)
    {
        _lastHitTest = time;
        var hitRadius = 0.5f + _projProp.BonusSize;
        var checkPos = Camera.WorldToDebug2D(new Vector3(_position.X, _position.Y, 0.5f));

        if (_damagePlayers)
        {
            var target = EntityUtils.GetPlayerHit(checkPos, hitRadius);
            if (target == null || _hitEntities.Contains(target.ObjectId))
                return false;

            if (target.HasConditionEffect(ConditionEffects.Invincible) ||
                target.HasConditionEffect(ConditionEffects.Stasis) ||
                target.HasConditionEffect(ConditionEffects.Paused))
                return false;

            var actualDamage = Entity.DamageWithDefense(_damage, target.Defense, _projProp.ArmorPiercing, target,
                _projProp.Penetration);
            if (actualDamage > 0 || (_projProp.Effects != null && _projProp.Effects.Length > 0))
            {
                target.Damage(actualDamage, ConditionEffectIndex.None, _projProp.ArmorPiercing);
            }

            Map.PendingPlayerHit.AddHit(_projectileId, _ownerId);

            if (!_projProp.MultiHit)
                return true;

            _hitEntities.Add(target.ObjectId);
            return false;
        }

        var enemy = EntityUtils.GetClosestEnemy(checkPos, hitRadius);
        if (enemy == null || _hitEntities.Contains(enemy.ObjectId))
            return false;

        var hitRadiusSqr = hitRadius * hitRadius;
        var hitSomething = false;

        foreach (var kvp in Map.Entities)
        {
            var go = kvp.Value;

            if (go.Properties == null) continue;
            if (_damagePlayers && !go.Properties.IsPlayer) continue;
            if (!_damagePlayers && !go.Properties.IsEnemy) continue;

            if (_hitEntities.Contains(go.ObjectId)) continue;

            var goPos = Camera.WorldToDebug2D(new Vector3(go.Position.X, go.Position.Y, 0.5f));
            var distSqr = (goPos - checkPos).LengthSquared;

            if (distSqr > hitRadiusSqr) continue;
            if (_ricochetsLeft > 0)
            {
                _ricochetsLeft--;
                var chainTarget = GetChainTarget(go);

                if (chainTarget != null)
                {
                    _startPosition = _position;
                    var dx = chainTarget.Position.X - _position.X;
                    var dy = chainTarget.Position.Y - _position.Y;
                    _angle = MathF.Atan2(dy, dx);

                    _startTime = time;
                    _hitEntities.Clear();
                }
                else
                {
                    Clear = true;
                }
            }

            if (go.HasConditionEffect(ConditionEffects.Invincible) ||
                go.HasConditionEffect(ConditionEffects.Stasis) ||
                go.HasConditionEffect(ConditionEffects.Paused))
                continue;

            bool isAllyShot = (!_damagePlayers && _ownerId != Map.LocalPlayerId);

            if (!isAllyShot)
            {
                var scaledDamage = _damage;
                if (!_damagePlayers && Map.LocalPlayer != null)
                    scaledDamage = scaledDamage * (Map.LocalPlayer.AllDamageMultiplier + 100) / 100;

                var actualDamage = Entity.DamageWithDefense(scaledDamage, go.Defense, _projProp.ArmorPiercing, go,
                    _projProp.Penetration);

                if (actualDamage > 0 || (_projProp.Effects != null && _projProp.Effects.Length > 0))
                {
                    go.Damage(actualDamage, ConditionEffectIndex.None, _projProp.ArmorPiercing);
                }

                if (_damagePlayers)
                    Map.PendingPlayerHit.AddHit(_projectileId, _ownerId);
                else
                    Map.PendingEnemyHit.AddHit((int)time, _projectileId, go.ObjectId, go.PredictedHp <= 0);
                
            }


            ChatBox.AddChatLine.Dispatch(new ChatBoxLineData(0, "*Error*", -1, null, _projectileId.ToString()));
            _hitEntities.Add(go.ObjectId);
            hitSomething = true;

            if (_projProp.MultiHit)
                _multiHitDict[go] = true;
            
            if (_ricochetsLeft == 0 && _projProp.Ricochet > 0) return true;
            if (!_projProp.MultiHit) return true;
        }

        return false;
    }

    private Vector2 PositionAt(float curLifetime)
    {
        var pos = _startPosition;

        var accelTime = curLifetime - _projProp.AccelerationDelay;

        if (curLifetime >= _projProp.AccelerationDelay)
            _accel = (_projProp.RealSpeed * accelTime * _projProp.Acceleration) / 200f;

        _accel = Math.Clamp(_accel, _projProp.MinAcceleration, _projProp.MaxAcceleration);

        var baseDistance = curLifetime * (_projProp.Speed);
        baseDistance += accelTime >= 0 ? accelTime * (_accel / 10000f) : 0f;

        float phase;
        if (!_projProp.Asyncrone)
        {
            if (!_projProp.OneWayFrequency)
                phase = (float)((_projectileId % 2) == 0 ? 0 : Math.PI);
            else
                phase = 0;
        }
        else
            phase = (_projectileId + 2) % 3 == 0 ? 0 : MathF.PI;
        
        if (_projProp.Wavy)
        {
            var period = 6 * MathF.PI;
            var amp = MathF.PI / 64f;
            var theta = _angle + amp * MathF.Sin(phase + period * curLifetime / 1000f);
            pos.X += baseDistance * MathF.Cos(theta);
            pos.Y += baseDistance * MathF.Sin(theta);
        }
        else if (_projProp.Parametric)
        {
            var t = curLifetime / _projProp.LifetimeMs * 2 * MathF.PI;
            float x, y;
            if (_projProp.NoAlt)
            {
                x = MathF.Sin(2 * t) * -1f;
                y = MathF.Sin(2 * t) * -1f;
            }
            else
            {
                x = MathF.Sin(t) * (_projectileId % 2 == 0 ? -1 : 1);
                y = MathF.Sin(2 * t) * (_projectileId % 4 < 2 ? 1 : -1);
            }

            pos.X += (x * MathF.Cos(_angle) - y * MathF.Sin(_angle)) * _projProp.Magnitude;
            pos.Y += (x * MathF.Sin(_angle) + y * MathF.Cos(_angle)) * _projProp.Magnitude;
        }
        else if (_projProp.Boomerang)
        {
            var halfDist = _projProp.LifetimeMs * _projProp.Speed / 2f;
            var d = baseDistance > halfDist ? halfDist - (baseDistance - halfDist) : baseDistance;
            pos.X += d * MathF.Cos(_angle);
            pos.Y += d * MathF.Sin(_angle);
            if (_projProp.Amplitude != 0)
            {
                var amplitude = _projProp.Amplitude + _amplitudeVariance;
                
                var deflection = amplitude *
                                 MathF.Sin(phase + curLifetime / _projProp.LifetimeMs * _projProp.Frequency * 2 *
                                     MathF.PI);
                pos.X += deflection * MathF.Cos(_angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(_angle + MathF.PI / 2);
            }
        }
        else
        {
            pos.X += baseDistance * _dirX;
            pos.Y += baseDistance * _dirY;
            if (_projProp.Amplitude != 0 && (!_projProp.Asyncrone || (_projectileId % 3) != 0))
            {
                var amplitude = _projProp.Amplitude + _amplitudeVariance;

                var deflection = (amplitude * MathF.Sin((phase + (((((curLifetime + (_projProp.LifetimeMs * (_projProp.StartFrequency * 4))) /
                                                                     (_projProp.LifetimeMs + (_projProp.LifetimeMs * (_projProp.StartFrequency * 4)))) *
                    _projProp.Frequency + _projProp.StartFrequency) * 2) * MathF.PI))));
                
                pos.X += deflection * MathF.Cos(_angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(_angle + MathF.PI / 2);
            }
        }

        if (_projProp.Orbiting)
        {
            _spinDistance = (curLifetime / 1000f) * _projProp.RadiusIncreaseSpeed + _projProp.StartRotationRadius;
            if (_spinDistance > _projProp.MaxRotationRadius && _projProp.MaxRotationRadius > 0)
                _spinDistance = _projProp.MaxRotationRadius;

            float radiansPerSecond = _projProp.RotationSpeed * (MathF.PI / 180f);
            float orbitSeconds = (curLifetime - _projProp.OrbitDelay) / 1000f;
            float rotation = orbitSeconds * radiansPerSecond;

            if ((_projectileId % 2 == 0 || _projProp.OnlyClockwise) && !_projProp.OnlyCounterClockwise)
                _spinAngle = curLifetime > _projProp.OrbitDelay ? rotation + _startSpinAngle : _angle;
            else
                _spinAngle = curLifetime > _projProp.OrbitDelay ? -rotation + _startSpinAngle : _angle;

            if (_projProp.ForceOrbiting && _owner != null)
            {
                pos.X = _owner.Position.X + _spinDistance * MathF.Cos(_spinAngle);
                pos.Y = _owner.Position.Y + _spinDistance * MathF.Sin(_spinAngle);
            }
            else
            {
                pos.X += _spinDistance * MathF.Cos(_spinAngle);
                pos.Y += _spinDistance * MathF.Sin(_spinAngle);
            }
        }

        if (_projProp.HitAmount > 1)
        {
            _hitCoolDown += curLifetime - _projDelayMs - _saveCoolDown;
            
            _saveCoolDown += curLifetime - _projDelayMs - _saveCoolDown;
            hitCoolDownReset = _projProp.LifetimeMs / _projProp.HitAmount;

            if (_hitCoolDown > hitCoolDownReset)
            {
                _multiHitDict = null;
                _multiHitDict = new Dictionary<Entity, bool>();
                _hitCoolDown %= hitCoolDownReset;
            }
        }
        
        return pos;
    }

    private bool RayCastCheck()
    {
        var life = _projProp.RCSpeed > 0 ? _projProp.RCLifetimeMs : _lifetime + _projDelayMs;

        for (var t = 0; t <= life; t += 25)
        {
            Vector2 pos;
            if (_projProp.RCSpeed > 0)
                pos = PositionAtRayCastLine(t);
            else
                pos = PositionAt(t - _projProp.ProjDelayMs);

            var tile = Map.GetTile(pos);
            if (tile == null || tile.Type == 0xFF)
                return false;

            if (tile.OccupiedObject != null && !_projProp.PassAll)
            {
                var obj = tile.OccupiedObject.Properties;

                if (obj.IsEnemy || tile.OccupiedObject.Hp > 0)
                    continue;

                if (obj.EnemyOccupySquare || (!_projProp.PassesCover && obj.OccupySquare))
                {
                    if (_projProp.ReturnRayCast)
                    {
                        var trimmed = _lifetime - (t - 25);
                        _projDelayMs -= (int)(trimmed * (_projDelayMs / _projProp.LifetimeMs));
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

    private Vector2 PositionAtRayCastLine(float t)
    {
        var pos = _startPosition;
        var speed = _projProp.RCSpeed;
        var dist = t * (speed / 10000f);
        pos.X += dist * MathF.Cos(_angle);
        pos.Y += dist * MathF.Sin(_angle);
        return pos;
    }
    private void RayCastHit(double time)
    {
        var texture = GetTexture();
        var hitRadius = 0.5f + _projProp.BonusSize;

        var life = time - _startTime - _projDelayMs;
        var first = true;
        var prevPos = Vector2.Zero;

        for (var t = 0;  life <= _projProp.LifetimeMs; life += 50)
        {
            Vector2 pos = _projProp.RCSpeed > 0
                ? PositionAtRayCastLine((float)life)
                : PositionAt((float)life);

            var tile = Map.GetTile(pos);
            if (tile == null || tile.Type == 0xFF)
                break;

            if (tile.OccupiedObject != null && !_projProp.PassAll)
            {
                var obj = tile.OccupiedObject.Properties;
                var isHittable = obj.IsEnemy || tile.OccupiedObject.Hp > 0;
                if (!isHittable && (obj.EnemyOccupySquare || (!_projProp.PassesCover && obj.OccupySquare)))
                    break;
            }
            
            var angle = first
                ? _angle
                : MathF.Atan2(pos.Y - prevPos.Y, pos.X - prevPos.X);
            prevPos = pos;
            first = false;

            var projPos = Camera.WorldToDebug2D(new Vector3(pos.X, pos.Y, 0.5f));

            var rcp = new RayCastParticle(texture, pos, angle, _angleCorrection, Size, _projProp.AlphaDecay)
            {
                Alpha = (!_damagePlayers && _ownerId != Map.LocalPlayerId) ? Settings.AllyProjOpacity : 1f
            };
            Map.AddParticleEffect(rcp);

            if (_damagePlayers)
            {
                var target = EntityUtils.GetPlayerHit(projPos, hitRadius);
                if (target != null && !_hitEntities.Contains(target.ObjectId))
                {
                    var actualDamage =
                        Entity.DamageWithDefense(_damage, target.Defense, _projProp.ArmorPiercing, target);
                    if (actualDamage > 0 || (_projProp.Effects != null && _projProp.Effects.Length > 0))
                    {
                        target.Damage(actualDamage, ConditionEffectIndex.None, _projProp.ArmorPiercing);
                    }

                    if (!_projProp.MultiHit) return;

                    _hitEntities.Add(target.ObjectId);
                }
            }
            else
            {
                var target = EntityUtils.GetClosestEnemy(projPos, hitRadius);
                if (target != null && target.Properties != null && !_hitEntities.Contains(target.ObjectId))
                {
                    var isEnemy = target.Properties.IsEnemy;
                    var isDestructible = target.Hp > 0;

                    if (isEnemy || isDestructible)
                    {
                        if (target.IsPredictedDead) continue;

                        if (target.HasConditionEffect(ConditionEffects.Invincible) ||
                            target.HasConditionEffect(ConditionEffects.Stasis) ||
                            target.HasConditionEffect(ConditionEffects.Paused))
                            continue;

                        bool isAllyShot = (!_damagePlayers && _ownerId != Map.LocalPlayerId);

                        if (!isAllyShot)
                        {
                            var actualDamage = Entity.DamageWithDefense(_damage, target.Defense,
                                _projProp.ArmorPiercing, target);
                            Map.PendingEnemyHit.AddHit((int)time, _projectileId, target.ObjectId, false);

                            if (actualDamage > 0 || (_projProp.Effects != null && _projProp.Effects.Length > 0))
                            {
                                target.Damage(actualDamage, ConditionEffectIndex.None, _projProp.ArmorPiercing);
                            }
                        }

                        if (!_projProp.MultiHit) return;
                        _hitEntities.Add(target.ObjectId);
                    }
                }
            }
        }
    }

    private Entity GetChainTarget(Entity currentTarget)
    {
        Entity closest = null;
        var closestDistSqr = float.MaxValue;

        var maxChainDistSqr = _projProp.Ricochet * 2f * (_projProp.Ricochet * 2f);

        foreach (var kvp in Map.Entities)
        {
            var go = kvp.Value;
            if (go == currentTarget || go.Properties == null) continue;
            if (_damagePlayers && !go.Properties.IsPlayer) continue;
            if (!_damagePlayers && !go.Properties.IsEnemy) continue;

            var distSqr = (go.Position - _position).LengthSquared;

            if (distSqr <= maxChainDistSqr && distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = go;
            }
        }

        return closest;
    }

    public void SetDamage(int dmg)
    {
        _damage = dmg;
    }
}