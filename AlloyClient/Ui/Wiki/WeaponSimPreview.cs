using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Game;
using AlloyClient.Game.Objects.Util;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.Utils;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Wiki;

public class WeaponSimPreview : Container {
    private const float BaseScale = 30f;
    private const int MaxProjectiles = 256;

    private readonly SdfRect _background;
    private readonly Container _gridLayer;
    private readonly SimpleText _hintText;
    private readonly Vector2 _origin;
    private readonly int _previewH;

    private readonly int _previewW;

    private readonly List<LiveProjectile> _projectiles = new();
    private readonly Container _projLayer;
    private readonly List<RaycastDot> _raycastDots = new();
    private readonly List<TrailDot> _trailDots = new();
    private readonly Container _trailLayer;
    private float _aimAngle;
    private bool _isHolding;
    private float _lastMouseX;
    private float _lastMouseY;
    private long _lastTickTime;
    private float _simTimeMs;
    private float _timeSinceLastShot;

    private ItemDesc _weapon;

    private float _zoom = 1.0f;

    public WeaponSimPreview(int width, int height) {
        _previewW = width;
        _previewH = height;
        _origin = new Vector2(width / 2f, height / 2f);

        SelfContentWidth = width;
        SelfContentHeight = height;
        EnableClipRect = true;
        MouseEnabled = true;

        _background = new SdfRect(new SdfRectConfig {
            Width = width,
            Height = height,
            Cut = 8,
            Color = 0xFF1A1A1Au,
            OutlineColor = 0xFF404040u,
            OutlineSize = 2f,
            ShadowRadius = 0f
        });
        _background.MouseEnabled = false;
        AddChild(_background);

        _gridLayer = new Container();
        AddChild(_gridLayer);

        _trailLayer = new Container();
        AddChild(_trailLayer);

        _projLayer = new Container();
        AddChild(_projLayer);

        RedrawGrid();

        AddEventListener(MouseEvent.LeftDown, new Action<MouseEvent>(OnMouseDown));
        AddEventListener(MouseEvent.LeftUp, OnMouseUp);
        AddEventListener(MouseEvent.MouseMove, new Action<MouseEvent>(OnMouseMove));
        AddEventListener(MouseEvent.ScrollVertical, new Action<MouseEvent>(OnMouseScroll));
        AddEventListener(Event.AddedToStage, OnAddedToStage);
        AddEventListener(Event.RemovedFromStage, OnRemovedFromStage);
    }

    
    private void OnAddedToStage() {
        _lastTickTime = Environment.TickCount64;
        AddEventListener(Event.EnterFrame, OnTick);
    }

    private void OnRemovedFromStage() {
        RemoveEventListener(Event.EnterFrame, OnTick);
        ClearProjectiles();
        _isHolding = false;
    }

    public void LoadWeapon(ItemDesc weapon) {
        _weapon = weapon;

        ClearProjectiles();
        _timeSinceLastShot = GetFireRateMs();
        _simTimeMs = 0f;
        _lastTickTime = Environment.TickCount64;
    }

    private void OnMouseDown(MouseEvent e) {
        _isHolding = true;
        _lastMouseX = e.Coords.X;
        _lastMouseY = e.Coords.Y;
        _lastTickTime = Environment.TickCount64;
        UpdateAimAngle();
    }

    private void OnMouseUp() {
        _isHolding = false;
    }

    private void OnMouseMove(MouseEvent e) {
        _lastMouseX = e.Coords.X;
        _lastMouseY = e.Coords.Y;
        UpdateAimAngle();
    }

    private void OnMouseScroll(MouseEvent e) {
        _zoom = Math.Clamp(_zoom + e.Delta * 0.05f, 0.25f, 3.0f);
        RedrawGrid();
    }

    private Vector2 GetGlobalPos(Vector2 localPos) {
        var pos = localPos;
        Sprite curr = this;
        while (curr != null) {
            pos.X *= curr.Scale.X;
            pos.Y *= curr.Scale.Y;
            pos.X += curr.X;
            pos.Y += curr.Y;
            curr = curr.Parent;
        }

        return pos;
    }

    private void UpdateAimAngle() {
        var globalCenter = GetGlobalPos(_origin);
        var dx = _lastMouseX - globalCenter.X;
        var dy = _lastMouseY - globalCenter.Y;
        _aimAngle = MathF.Atan2(dy, dx);
    }


    private void OnTick() {
        var now = Environment.TickCount64;
        float dt = now - _lastTickTime;
        _lastTickTime = now;

        if (dt <= 0) return;
        if (dt > 100f) dt = 16f;

        _simTimeMs += dt;
        var effectiveScale = BaseScale * _zoom;

        for (var i = _projectiles.Count - 1; i >= 0; i--) {
            var p = _projectiles[i];
            p.ElapsedMs += dt;

            if (p.ElapsedMs < p.DelayMs) {
                p.Sprite.Visible = false;
                continue;
            }

            var curLifetime = p.ElapsedMs - p.DelayMs;

            if (curLifetime > p.Def.LifetimeMs) {
                _projLayer.RemoveChild(p.Sprite);
                _projectiles.RemoveAt(i);
                continue;
            }

            p.Sprite.Visible = true;

            if (p.Def.Fade > 0) p.Sprite.Alpha = curLifetime >= p.Def.Fade ? 1.0f : 0.3f;

            var zHeight = 0f;
            if (p.Def.Falldown)
                zHeight = 40f - curLifetime / p.Def.LifetimeMs * 40f;
            else if (p.Def.Missile)
                zHeight = MathF.Abs(MathF.Sin(curLifetime / p.Def.LifetimeMs * MathF.PI)) * 3f +
                          (0.8f - curLifetime / p.Def.LifetimeMs * 0.8f);
            var zScale = 1.0f + zHeight / 40f * 1.5f;

            var accel = p.Accel;
            var spinDist = p.SpinDist;
            var spinAngle = p.SpinAngle;

            var pos = PositionAt(p.Def, curLifetime, p.Angle, p.DirX, p.DirY, p.BulletId, ref accel, ref spinDist,
                ref spinAngle, p.StartSpinAngle, Vector2.Zero);

            var dir = pos - p.LastPos;

            var spriteSpin = 0f;
            if (p.ObjProps != null && p.ObjProps.Rotation != 0) spriteSpin = curLifetime / p.ObjProps.Rotation;

            var baseAngle = p.Angle;

            if (p.Def.ForceOrbiting || p.Def.Orbiting) {
                baseAngle = spinAngle;
            } else if (p.Def.DynamicAngle) {
                if (dir.LengthSquared > 0.000001f) p.CalculatedAngle = -MathF.Atan2(dir.X, dir.Y) + MathF.PI / 2f;
                baseAngle = p.CalculatedAngle;
            } else if (!p.Def.VanillaAngle && !p.Def.NoRotation) {
                if (dir.LengthSquared > 0.000001f) p.CalculatedAngle = MathF.Atan2(dir.Y, dir.X);
                baseAngle = p.CalculatedAngle;
            }

            p.LastPos = pos;

            var angleCorrection = p.ObjProps != null ? p.ObjProps.AngleCorrection * MathF.PI / 4f : 0f;
            p.Sprite.Rotation = baseAngle + spriteSpin + angleCorrection;

            p.Accel = accel;
            p.SpinDist = spinDist;
            p.SpinAngle = spinAngle;

            var uiX = (int)(_origin.X + pos.X * effectiveScale);
            var uiY = (int)(_origin.Y + pos.Y * effectiveScale);

            p.Sprite.Scale = new Vector2(_zoom * p.BaseSizeScale * zScale, _zoom * p.BaseSizeScale * zScale);
            p.Sprite.X = uiX;
            p.Sprite.Y = uiY;

            if (p.Def.ParticleTrail) {
                p.TrailTimer -= dt;
                if (p.TrailTimer <= 0) {
                    p.TrailTimer = 40f;
                    SpawnTrailDot(pos, p.Def.ParticleTrailColor, p.Def.ParticleTrailLifetime);
                }
            }

            var outOfBounds = uiX < 0 || uiX > _previewW || uiY < 0 || uiY > _previewH;
            if (outOfBounds) p.Sprite.Visible = false;
        }

        for (var i = _trailDots.Count - 1; i >= 0; i--) {
            var dot = _trailDots[i];
            dot.TimeLeft -= dt;
            if (dot.TimeLeft <= 0) {
                _trailLayer.RemoveChild(dot.Sprite);
                _trailDots.RemoveAt(i);
                continue;
            }

            dot.Sprite.Alpha = dot.TimeLeft / dot.MaxTime;

            var uiX = (int)(_origin.X + dot.Pos.X * effectiveScale);
            var uiY = (int)(_origin.Y + dot.Pos.Y * effectiveScale);

            dot.Sprite.Scale = new Vector2(_zoom, _zoom);
            dot.Sprite.X = uiX;
            dot.Sprite.Y = uiY;
        }

        for (var i = _raycastDots.Count - 1; i >= 0; i--) {
            var dot = _raycastDots[i];
            dot.TimeLeft -= dt;
            if (dot.TimeLeft <= 0) {
                _projLayer.RemoveChild(dot.Sprite);
                _raycastDots.RemoveAt(i);
                continue;
            }

            dot.Sprite.Visible = true;
            dot.Sprite.Alpha = dot.TimeLeft / dot.MaxTime;

            var uiX = (int)(_origin.X + dot.Pos.X * effectiveScale);
            var uiY = (int)(_origin.Y + dot.Pos.Y * effectiveScale);

            dot.Sprite.Scale = new Vector2(_zoom * dot.BaseSizeScale, _zoom * dot.BaseSizeScale);
            dot.Sprite.X = uiX;
            dot.Sprite.Y = uiY;
        }

        if (_isHolding && _weapon != null) {
            _timeSinceLastShot += dt;

            var fireRate = GetFireRateMs();
            if (_timeSinceLastShot >= fireRate && _projectiles.Count < MaxProjectiles) {
                SpawnVolley(_aimAngle);
                _timeSinceLastShot -= fireRate;
            }
        }
    }

    private float GetFireRateMs() {
        var rof = _weapon?.RateOfFire ?? 1f;
        if (rof <= 0f) rof = 1f;

        var dex = Map.LocalPlayer?.Dexterity ?? 75;
        var minAttackFreq = 0.0015f;
        var maxAttackFreq = 0.0065f;
        var attackFreq = minAttackFreq + (dex / 250f) * minAttackFreq;

        if (Map.LocalPlayer != null) {
            if (Map.LocalPlayer.HasConditionEffect(ConditionEffects.Berserk)) attackFreq *= 1.25f;
            if (Map.LocalPlayer.HasConditionEffect(ConditionEffects.Dazed)) attackFreq = minAttackFreq;
        }

        return 1f / attackFreq * (1f / rof);
    }

    private void SpawnVolley(float aimAngle) {
        ProjectileDesc[] defs;
        if (_weapon.AllProjectiles is { Length: > 0 })
            defs = _weapon.AllProjectiles;
        else if (_weapon.Projectiles != null)
            defs = new[] { _weapon.Projectiles };
        else return;

        var numProj = Math.Max(1, _weapon.NumProjectiles);
        var arcGapRad = _weapon.ArcGap * MathF.PI / 180f;
        var totalArc = arcGapRad * (numProj - 1);
        var startAngle = aimAngle - totalArc / 2f;

        ObjectLibrary.TypeToObjectProps.TryGetValue(_weapon.ObjectType, out var weaponProps);

        for (var i = 0; i < numProj; i++) {
            if (_projectiles.Count >= MaxProjectiles) break;

            var angle = startAngle + i * arcGapRad;
            var desc = defs[Math.Min(i, defs.Length - 1)];

            ProjectileProperties projProp = null;
            if (weaponProps != null)
                if (!weaponProps.ProjectilesDict.TryGetValue(desc.BulletType, out projProp))
                    if (weaponProps.Projectiles.Count > 0)
                        projProp = weaponProps.Projectiles[0];

            if (projProp != null) SpawnProjectile(projProp, angle, i);
        }
    }

    private void SpawnProjectile(ProjectileProperties def, float angle, int bulletId) {
        var baseSizeScale = (def.Size > 0 ? def.Size : 100) / 100f;
        var spriteSizePx = 24;

        ObjectProperties projObjProps = null;
        if (!string.IsNullOrEmpty(def.ObjectId))
            if (ObjectLibrary.IdToObjectType.TryGetValue(def.ObjectId, out var projObjType))
                ObjectLibrary.TypeToObjectProps.TryGetValue(projObjType, out projObjProps);

        if (def.RayCast) {
            var life = def.RCSpeed > 0 ? def.RCLifetimeMs : def.LifetimeMs + def.ProjDelayMs;

            for (float t = 0; t <= life; t += 25f) {
                var pos = def.RCSpeed > 0
                    ? PositionAtRayCastLine(def, t, angle, Vector2.Zero)
                    : PositionAt(def, t, angle, MathF.Cos(angle), MathF.Sin(angle), bulletId, ref _simTimeMs,
                        ref _simTimeMs, ref _simTimeMs, 0, Vector2.Zero);

                Sprite dotSprite = new Ellipse(new EllipseConfig {
                    DiameterX = 6, DiameterY = 6,
                    Color = 0xFFFFAA44u, OutlineSize = 0, Anchor = UiAnchor.Middle
                });
                dotSprite.Visible = false;
                _projLayer.AddChild(dotSprite);

                _raycastDots.Add(new RaycastDot {
                    Sprite = dotSprite,
                    Pos = pos,
                    TimeLeft = 200f,
                    MaxTime = 200f,
                    BaseSizeScale = baseSizeScale
                });
            }

            return;
        }

        Sprite sprite;
        if (projObjProps != null)
            sprite = new ObjectRect(new ObjectRectConfig {
                Texture = TextureHelper.FromGameAtlas(projObjProps.ObjectType),
                Width = spriteSizePx,
                Height = spriteSizePx,
                OutlineEnabled = false,
                GlowEnabled = false,
                Anchor = UiAnchor.Middle
            });
        else
            sprite = new Ellipse(new EllipseConfig {
                DiameterX = spriteSizePx,
                DiameterY = spriteSizePx,
                Color = 0xFFFFAA44u,
                OutlineSize = 0,
                Anchor = UiAnchor.Middle
            });

        sprite.Visible = false;
        _projLayer.AddChild(sprite);

        var startSpinAngle = def.DynamicRotationAngle ? angle : 0f;
        if (def.FixedRotationAngle != 0) {
            if (bulletId % 2 == 0 && !(def.OnlyClockwise || def.OnlyCounterClockwise))
                startSpinAngle -= def.FixedRotationAngle * MathF.PI / 180f;
            else
                startSpinAngle += def.FixedRotationAngle * MathF.PI / 180f;
        }

        _projectiles.Add(new LiveProjectile {
            Def = def,
            ObjProps = projObjProps,
            Angle = angle,
            DirX = MathF.Cos(angle),
            DirY = MathF.Sin(angle),
            BulletId = bulletId,
            ElapsedMs = 0f,
            Sprite = sprite,
            StartSpinAngle = startSpinAngle,
            DelayMs = def.ProjDelayMs,
            BaseSizeScale = baseSizeScale,
            LastPos = Vector2.Zero
        });
    }

    private void SpawnTrailDot(Vector2 pos, uint color, int lifeTimeMs) {
        var maxTime = lifeTimeMs > 0 ? lifeTimeMs : 600f;

        var dot = new Ellipse(new EllipseConfig {
            DiameterX = 6, DiameterY = 6,
            Color = color | 0xFF000000u, OutlineSize = 0, Anchor = UiAnchor.Middle
        });
        _trailLayer.AddChild(dot);

        _trailDots.Add(new TrailDot {
            Sprite = dot,
            Pos = pos,
            TimeLeft = maxTime,
            MaxTime = maxTime
        });
    }

    private static Vector2 PositionAt(
        ProjectileProperties proj, float curLifetime, float angle, float dirX, float dirY,
        int bulletId, ref float accel, ref float spinDistance, ref float spinAngle,
        float startSpinAngle, Vector2 origin) {
        var pos = origin;

        var accelTime = curLifetime - proj.AccelerationDelay;
        if (accelTime >= 0) accel = accelTime * (proj.Acceleration / 5f);
        accel = Math.Clamp(accel, proj.MinAcceleration, proj.MaxAcceleration);

        var baseDistance = curLifetime * proj.Speed;
        baseDistance += accelTime >= 0 ? accelTime * (accel / 10000f) : 0f;

        var phase = proj.Asyncrone ? (bulletId + 2) % 3 == 0 ? 0 : MathF.PI : bulletId % 2 * MathF.PI;

        if (proj.Wavy) {
            var theta = angle + MathF.PI / 64f * MathF.Sin(phase + 6 * MathF.PI * curLifetime / 1000f);
            pos.X += baseDistance * MathF.Cos(theta);
            pos.Y += baseDistance * MathF.Sin(theta);
        } else if (proj.Parametric) {
            var t = curLifetime / proj.LifetimeMs * 2 * MathF.PI;
            float x, y;
            if (proj.NoAlt) {
                x = MathF.Sin(2 * t) * -1f;
                y = MathF.Sin(2 * t) * -1f;
            } else {
                x = MathF.Sin(t) * (bulletId % 2 == 0 ? -1f : 1f);
                y = MathF.Sin(2 * t) * (bulletId % 4 < 2 ? 1f : -1f);
            }

            pos.X += (x * MathF.Cos(angle) - y * MathF.Sin(angle)) * proj.Magnitude;
            pos.Y += (x * MathF.Sin(angle) + y * MathF.Cos(angle)) * proj.Magnitude;
        } else if (proj.Boomerang) {
            var halfDist = proj.LifetimeMs * proj.Speed / 2f;
            var d = baseDistance > halfDist ? halfDist - (baseDistance - halfDist) : baseDistance;
            pos.X += d * MathF.Cos(angle);
            pos.Y += d * MathF.Sin(angle);
            if (proj.Amplitude != 0) {
                var deflection = proj.Amplitude *
                                 MathF.Sin(phase + curLifetime / proj.LifetimeMs * proj.Frequency * 2 * MathF.PI);
                pos.X += deflection * MathF.Cos(angle + MathF.PI / 2);
                pos.Y += deflection * MathF.Sin(angle + MathF.PI / 2);
            }
        } else {
            pos.X += baseDistance * dirX;
            pos.Y += baseDistance * dirY;
            if (proj.Amplitude != 0 && proj.Frequency > 0) {
                var deflection = proj.Amplitude * MathF.Sin(
                    phase + (curLifetime + proj.LifetimeMs * (proj.StartFrequency / proj.Frequency))
                    / proj.LifetimeMs * proj.Frequency * 2 * MathF.PI);
                pos.X += deflection * -dirY;
                pos.Y += deflection * dirX;
            }
        }

        if (proj.ForceOrbiting || proj.Orbiting) {
            spinDistance = curLifetime * (proj.RadiusIncreaseSpeed / 200f) + proj.StartRotationRadius;
            if (spinDistance > proj.MaxRotationRadius && proj.MaxRotationRadius > 0)
                spinDistance = proj.MaxRotationRadius;

            spinAngle = (bulletId % 2 == 0 || proj.OnlyClockwise) && !proj.OnlyCounterClockwise
                ? curLifetime * (proj.RotationSpeed / 200f) + startSpinAngle
                : -(curLifetime * (proj.RotationSpeed / 200f)) + startSpinAngle;

            pos.X = (proj.ForceOrbiting ? origin.X : pos.X) + spinDistance * MathF.Cos(spinAngle);
            pos.Y = (proj.ForceOrbiting ? origin.Y : pos.Y) + spinDistance * MathF.Sin(spinAngle);
        }

        return pos;
    }

    private static Vector2 PositionAtRayCastLine(ProjectileProperties proj, float t, float angle, Vector2 origin) {
        var dist = t * (proj.RCSpeed / 10000f);
        return new Vector2(origin.X + dist * MathF.Cos(angle), origin.Y + dist * MathF.Sin(angle));
    }

    private void ClearProjectiles() {
        foreach (var p in _projectiles) _projLayer.RemoveChild(p.Sprite);
        foreach (var d in _raycastDots) _projLayer.RemoveChild(d.Sprite);
        foreach (var t in _trailDots) _trailLayer.RemoveChild(t.Sprite);
        _projectiles.Clear();
        _raycastDots.Clear();
        _trailDots.Clear();
    }

    private void RedrawGrid() {
        while (_gridLayer.NumChildren > 0) _gridLayer.RemoveChildAt(0);

        var cx = _origin.X;
        var cy = _origin.Y;
        var effectiveScale = BaseScale * _zoom;

        for (float x = 0; x <= _previewW / 2f; x += effectiveScale)
            if (x > 0) {
                _gridLayer.AddChild(new ColorRect(new ColorRectConfig
                    { Width = 1, Height = _previewH, Color = 0xFF202020u }) { X = (int)(cx + x), Y = 0 });
                _gridLayer.AddChild(new ColorRect(new ColorRectConfig
                    { Width = 1, Height = _previewH, Color = 0xFF202020u }) { X = (int)(cx - x), Y = 0 });
            }

        for (float y = 0; y <= _previewH / 2f; y += effectiveScale)
            if (y > 0) {
                _gridLayer.AddChild(new ColorRect(new ColorRectConfig
                    { Width = _previewW, Height = 1, Color = 0xFF202020u }) { X = 0, Y = (int)(cy + y) });
                _gridLayer.AddChild(new ColorRect(new ColorRectConfig
                    { Width = _previewW, Height = 1, Color = 0xFF202020u }) { X = 0, Y = (int)(cy - y) });
            }

        _gridLayer.AddChild(new ColorRect(new ColorRectConfig { Width = _previewW, Height = 1, Color = 0xFF353535u })
            { X = 0, Y = (int)cy });
        _gridLayer.AddChild(new ColorRect(new ColorRectConfig { Width = 1, Height = _previewH, Color = 0xFF353535u })
            { X = (int)cx, Y = 0 });

        var centerDot = new Ellipse(new EllipseConfig {
            DiameterX = 8, DiameterY = 8, Color = 0xFF88CCFFu, OutlineSize = 0, Anchor = UiAnchor.Middle
        });
        centerDot.X = (int)cx;
        centerDot.Y = (int)cy;
        _gridLayer.AddChild(centerDot);
    }

    private class LiveProjectile {
        public float Accel;
        public float Angle;
        public float BaseSizeScale;
        public int BulletId;
        public float CalculatedAngle;
        public ProjectileProperties Def;
        public int DelayMs;
        public float DirX;
        public float DirY;
        public float ElapsedMs;
        public Vector2 LastPos;
        public ObjectProperties ObjProps;
        public float SpinAngle;
        public float SpinDist;
        public Sprite Sprite;
        public float StartSpinAngle;
        public float TrailTimer;
    }

    private class RaycastDot {
        public float BaseSizeScale;
        public float MaxTime;
        public Vector2 Pos;
        public Sprite Sprite;
        public float TimeLeft;
    }

    private class TrailDot {
        public float MaxTime;
        public Vector2 Pos;
        public Sprite Sprite;
        public float TimeLeft;
    }
}

