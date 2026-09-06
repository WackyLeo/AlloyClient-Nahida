using System;
using AlloyClient.Assets;
using AlloyClient.Assets.Libraries;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects.Enums;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking.Enums;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.ParticleEffects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.Ui.Character;
using AlloyClient.UiLib.Signals;
using AlloyClient.Utils;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public class Entity {
    private static readonly Logger Log = new(nameof(Entity));

    public static float AttackPeriod = 300;

    public readonly Signal<int> InventoryUpdate = new();

    private bool _hpInitialized;

    public float Alpha = 1.0f;
    public float AttackAngle;

    public double AttackStart;

    public ParticleEffect Effect;

    public float FacingAngle;

    public Vector4 FlashColor;
    private EntityFlashDescription _flashDescription;

    public bool Flipped;

    public float HeightOffset;

    public float Jitter;

    public int LastTickId;

    public double LastTickUpdateTime;
    // Merchant Specific

    public int merchandiseType_;

    public Vector2 MovementVector;

    public int ObjectId;
    public Vector2 Position;
    public Vector2 PositionAtTick;
    public int PredictedHp;
    public ObjectProperties Properties;
    
    // Effect multipliers
    public int effectMult;
    public int quietMult;
    public int ArmorBreakMult;
    public int ExposedMult;
    public int CurseMult;
    public int healingMult;

    public RenderBase RenderBaseType;
    public float Rotation;

    public AtlasData Texture;

    public TextureData TextureData;
    public Vector2 TickPosition;

    public MapTile Tile;
    public ushort Type;
    public float Z;
    public bool IsPredictedDead => PredictedHp <= 0 && _hpInitialized;

    public void SetObjectId(int id) {
        ObjectId = id;
        Jitter = Random.Shared.NextSingle() * 0.00002f - 0.00001f;

        if (Properties.EffectProperties != null)
            Effect = ParticleEffect.FromProperties(Properties.EffectProperties, this);

        Defense = Properties.Defense;
    }

    public void SetType(ushort type) {
        Type = type;

        if (!ObjectLibrary.TypeToTextureData.TryGetValue(type, out var textureData)) {
            Log.Warn($"No texture data found for type 0x{type:X4}, using fallback.");
            textureData = ObjectLibrary.TypeToTextureData[ObjectLibrary.FallbackType];
        }

        TextureData = textureData;

        if (TextureData.HasAnimationData
            && TextureData.AnimatedTextures.FaceRight != null
            && TextureData.AnimatedTextures.FaceRight.Length > 0)
            Texture = TextureData.AnimatedTextures.FaceRight[0];
        else
            Texture = TextureData.GetTexture(true);

        RenderBaseType = GetRenderType(type);
    }

    public void StartFlash(uint color, float periodSeconds, float repeats) {
        if (periodSeconds <= 0f || repeats <= 0f) {
            _flashDescription = default;
            FlashColor = Vector4.Zero;
            return;
        }

        _flashDescription = new EntityFlashDescription(
            AlloyClient.Main.GameTime.TotalMs,
            color,
            periodSeconds * 1000f,
            Math.Max(1, (int)MathF.Ceiling(repeats)));
    }

    private void UpdateFlash(double time) {
        if (!_flashDescription.Active) {
            FlashColor = Vector4.Zero;
            return;
        }

        var elapsed = (float)(time - _flashDescription.StartTimeMs);
        var total = _flashDescription.PeriodMs * _flashDescription.Repeats;
        if (elapsed > total) {
            _flashDescription = default;
            FlashColor = Vector4.Zero;
            return;
        }

        var local = elapsed % _flashDescription.PeriodMs;
        var intensity = MathF.Sin(local / _flashDescription.PeriodMs * MathF.PI) * 0.5f;
        FlashColor = new Vector4(
            _flashDescription.R / 255f,
            _flashDescription.G / 255f,
            _flashDescription.B / 255f,
            intensity);
    }

    public void SetTransparency(float alpha) {
        Alpha = Math.Clamp(alpha, 0f, 1f);

        if (RenderBaseType != null)
            RenderBaseType.Extra.Alpha = Alpha;
    }

    public Color GetDominateColor() {
        if (RenderBaseType is TypeWall)
            return TextureData.TopTexture.DominantColor;
        return TextureData.DominantColor;
    }

    protected virtual RenderBase GetRenderType(ushort type) {
        ObjectLibrary.TypeToObjectProps.TryGetValue(type, out var props);

        if (props == null)
            return new TypeNullObject();

        if (props.Size != -1)
            Size = props.Size;

        if (props.MinSize != props.MaxSize) {
            var maxSteps = (props.MaxSize - props.MinSize) / props.SizeStep;
            Size = props.MinSize + (int)(Random.Shared.NextSingle() * maxSteps) * props.SizeStep;
        }

        if (IsBigBag(type))
            Size = 120;

        if (!string.IsNullOrEmpty(props.Model))
            return new TypeModel3D(props.Model, this);

        if (props.DrawOnGround)
            return new TypeGroundObject(this);

        return props.Class switch {
            "Wall" => new TypeWall(this),
            "DoubleWall" => new TypeWall(this, ModelType.PbDoubleWall),
            "DoubleWall2" => new TypeWall(this, ModelType.PbDoubleWall),
            "TripleWall" => new TypeWall(this, ModelType.PbDoubleWall),
            "CaveWall" => new TypeCaveWall(this),
            "ConnectedWall" => new TypeConnectedWall(this, Properties.BaseIndent, Properties.TopIndent,
                Properties.Height),
            _ => new TypeGameObject(this)
        };
    }

    public void SetPos(float x, float y) {
        Position.X = x;
        Position.Y = y;

        Rotation = Properties.Rotation;
        RenderBaseType.SetPosition(x, y);
        Effect?.SetEntityPosition(Position);
    }

    public bool HasConditionEffect(ConditionEffects effect) {
        return (ConditionEffects & effect) != 0;
    }

    public virtual bool Update(double time, double dt) {
        if (MovementVector is not { X: 0, Y: 0 }) {
            if (LastTickId >= Map.LastTickId) {
                var tickDt = time - LastTickUpdateTime;
                var pX = PositionAtTick.X + tickDt * MovementVector.X;
                var pY = PositionAtTick.Y + tickDt * MovementVector.Y;
                MoveTo((float)pX, (float)pY, false);
            }
            else {
                MovementVector.X = 0;
                MovementVector.Y = 0;
                MoveTo(TickPosition.X, TickPosition.Y, false);
            }
        }
        else {
            MovementVector.X = 0;
            MovementVector.Y = 0;
        }

        // add wall support here at some point
        if (TextureData.HasAnimationData)
            AnimateCharacter(time);

        if (AnimationSpeed > 0) {
            if (_animationMinOld != AnimationMin) {
                _animationStart = time;
                _animationMinOld = AnimationMin;
            }

            ClientAnimation(time);
        }

        if (Effect != null && !Effect.Update(time, dt))
            Effect = null;

        UpdateFlash(time);

        RenderBaseType.SetPosition(Position.X, Position.Y, Z);
        return true;
    }

    private void ClientAnimation(double time) {
        if (AnimationMax <= 0)
            return;

        var frame = (int)(Math.Floor((time - _animationStart) / AnimationSpeed) % AnimationMax) + AnimationMin;

        if (frame == _lastAltTex)
            return;
        _lastAltTex = frame;

        SetAltTexture(frame);
    }

    public void UpdateVisibility(ref Matrix4 matrix, float renderRadiusSquared = -1f) {
        if (Map.LocalPlayer != null && this != Map.LocalPlayer && renderRadiusSquared > 0f) {
            var renderDx = Position.X - Map.LocalPlayer.Position.X;
            var renderDy = Position.Y - Map.LocalPlayer.Position.Y;
            if (renderDx * renderDx + renderDy * renderDy > renderRadiusSquared) {
                RenderBaseType.SetVisibility(false);
                return;
            }
        }

        if (Properties.Static) {
            RenderBaseType.SetVisibility(true);
        }
        else {
            var dx = Position.X - Camera.Position.X;
            var dy = Position.Y + Camera.Position.Y;
            var distanceSquared = dx * dx + dy * dy;
            var playerSightRadiusSquared = Map.LocalPlayer.UpdateRadius * Map.LocalPlayer.UpdateRadius;

            var visible = distanceSquared <= playerSightRadiusSquared;
            RenderBaseType.SetVisibility(visible);
        }

        //TODO: double check mg to make sure
        //var sort = Vector3.Transform(new Vector3(Position.X, Position.Y, 0), matrix).Y;
        var sort = Vector3.TransformPerspective(new Vector3(Position.X, Position.Y, 0), matrix).Y;
        RenderBaseType.SetDepth(0.5f + 0.4f * sort + Jitter);
    }

    public bool MoveTo(float x, float y, bool isPlayer) {
        var tile = Map.GetTile((int)x, (int)y);

        if (tile == null)
            return false;

        if (isPlayer && tile.OccupiedObject != null && tile.OccupiedObject.Properties.OccupySquare)
            return false;

        Position.X = x;
        Position.Y = y;
        Z = tile.ZOffset;

        if (Properties.Static) {
            if (Tile != null)
                Tile.OccupiedObject = null;

            tile.OccupiedObject = this;
        }

        Tile = tile;

        return true;
    }

    public void OnTickPosition(float x, float y, double tickTime, int tickId, bool isPlayer, double now) {
        if (!isPlayer && LastTickId < Map.LastTickId)
            MoveTo(TickPosition.X, TickPosition.Y, false);

        TickPosition.X = x;
        TickPosition.Y = y;
        LastTickId = tickId;
        LastTickUpdateTime = now;

        PositionAtTick.X = Position.X;
        PositionAtTick.Y = Position.Y;

        if (!isPlayer && tickTime > 0) {
            MovementVector.X = (float)((TickPosition.X - PositionAtTick.X) / tickTime);
            MovementVector.Y = (float)((TickPosition.Y - PositionAtTick.Y) / tickTime);
        }
    }

    public virtual void UpdateStats(StatData[] statData, int offset, int count) {
        for (var i = 0; i < count; i++) {
            var stat = statData[offset + i];
            switch (stat.Type) {
                case StatsType.Hp:
                    var serverHp = stat.Value;
                    if (!_hpInitialized) {
                        Hp = serverHp;
                        PredictedHp = serverHp;
                        _hpInitialized = true;
                        break;
                    }

                    if (serverHp > Hp) {
                        if (PredictedHp <= 0 && Properties.IsEnemy) {
                            var diff = serverHp - PredictedHp;
                            NotificationLayer.AddStatusText(this, $"+{diff}", 0x00FF00, 1000, 0);
                        }

                        PredictedHp += serverHp - Hp;
                    }
                    else if (serverHp < Hp) {
                        if (PredictedHp > serverHp)
                            PredictedHp = serverHp;
                    }
                    
                    if (serverHp > 0 && PredictedHp <= 0)
                        PredictedHp = serverHp;
                    
                    Hp = serverHp;
                    //Logger.Info($"[CLIENT SYNC] Entity {ObjectId} Server HP: {Hp} | Prediction: {PredictedHp}");

                    if (!IsPredictedDead)
                        SetTransparency(HasConditionEffect(ConditionEffects.Invisible) ? 0.5f : 1.0f);
                    break;

                case StatsType.MaximumHp:
                    MaxHp = stat.Value;
                    if (!_hpInitialized && PredictedHp == 0)
                        PredictedHp = MaxHp;
                    break;
                case StatsType.Defense:
                    Defense = stat.Value;
                    break;
                case StatsType.Size:
                    Size = stat.Value;
                    break;
                case StatsType.Level:
                    Level = stat.Value;
                    break;
                case StatsType.Inventory0:
                case StatsType.Inventory1:
                case StatsType.Inventory2:
                case StatsType.Inventory3:
                case StatsType.Inventory4:
                case StatsType.Inventory5:
                case StatsType.Inventory6:
                case StatsType.Inventory7:
                case StatsType.Inventory8:
                case StatsType.Inventory9:
                case StatsType.Inventory10:
                case StatsType.Inventory11:
                    var index = stat.Type - StatsType.Inventory0;
                    if (stat.Value == -1) {
                        Equipment[index] = null;
                    }
                    else if (Equipment[index] == null ||
                             (Equipment[index] != null && stat.Value != Equipment[index].ObjectType)) {
                        if (ObjectLibrary.TypeToItem.TryGetValue((ushort)stat.Value, out var b))
                            Equipment[index] = b;
                        else
                            Equipment[index] = null;
                    }

                    InventoryUpdate.Dispatch(index);
                    break;
                case StatsType.Effects:
                    var lower = (uint)stat.Value & 0x7FFFFFFFul;
                    ConditionEffects = (ConditionEffects)(((ulong)ConditionEffects & ~0x7FFFFFFFul) | lower);

                    if (HasConditionEffect(ConditionEffects.Invulnerable) ||
                        HasConditionEffect(ConditionEffects.Invincible)) {
                        if (PredictedHp <= 0 && Hp > 0)
                            NotificationLayer.AddStatusText(this, "Immune", 0x00FF00, 1000, 0);
                        PredictedHp = Hp;
                    }

                    SetTransparency(IsPredictedDead ? 0.4f :
                        HasConditionEffect(ConditionEffects.Invisible) ? 0.5f : 1.0f);
                    break;

                case StatsType.Effects2:
                    var upper = (ulong)(uint)stat.Value << 31;
                    ConditionEffects = (ConditionEffects)(((ulong)ConditionEffects & 0x7FFFFFFFul) | upper);

                    SetTransparency(IsPredictedDead ? 0.4f :
                        HasConditionEffect(ConditionEffects.Invisible) ? 0.5f : 1.0f);
                    break;
                case StatsType.Name:
                    if (Name != stat.Text) {
                        Name = string.Intern(stat.Text);
                        RenderBaseType.SetName(stat.Text);
                    }

                    break;
                case StatsType.Texture1:
                    if (stat.Value == Texture1Id)
                        break;

                    Texture1Id = stat.Value;
                    // TexturingCache = new Dict;
                    // Portrait = null;
                    break;
                case StatsType.Texture2:
                    if (stat.Value == Texture2Id)
                        break;

                    Texture2Id = stat.Value;
                    // TexturingCache = new Dict;
                    // Portrait = null;
                    break;
                case StatsType.Glow:
                    GlowColor = stat.Value;
                    break;
                case StatsType.AltTextureIndex:
                    SetAltTexture(stat.Value);
                    break;
                case StatsType.AltItemId:
                    if (stat.Value <= 0)
                        break;

                    AltItemID = (ushort)stat.Value;
                    if (!ObjectLibrary.TypeToTextureData.TryGetValue(AltItemID, out var textureData))
                        textureData = ObjectLibrary.TypeToTextureData[ObjectLibrary.FallbackType];

                    TextureData = textureData;
                    Texture = TextureData.Texture;
                    RenderBaseType?.SetTexture(Texture);
                    break;

                case StatsType.BackPack0:
                case StatsType.BackPack1:
                case StatsType.BackPack2:
                case StatsType.BackPack3:
                case StatsType.BackPack4:
                case StatsType.BackPack5:
                case StatsType.BackPack6:
                case StatsType.BackPack7:
                    index = 12 + stat.Type - StatsType.BackPack0;
                    if (stat.Value == -1) {
                        Equipment[index] = null;
                    }
                    else if (Equipment[index] == null ||
                             (Equipment[index] != null && stat.Value != Equipment[index].ObjectType)) {
                        if (ObjectLibrary.TypeToItem.TryGetValue((ushort)stat.Value, out var b))
                            Equipment[index] = b;
                        else
                            Equipment[index] = null;
                    }

                    InventoryUpdate.Dispatch(index);
                    break;
                case StatsType.HasBackpack:
                    //todo
                    break;
                case StatsType.PortalUsable:
                    PortalUsable = stat.Value != 0;
                    break;
                case StatsType.AltTextureMin:
                    AnimationMin = stat.Value;
                    break;
                case StatsType.AltTextureMax:
                    AnimationMax = stat.Value;
                    break;
                case StatsType.AnimationSpeed:
                    AnimationSpeed = stat.Value;
                    break;
                case StatsType.ObjectConnection:
                    ConnectType = stat.Value;
                    (RenderBaseType as TypeConnectedWall)?.SetConnectType(stat.Value);
                    (RenderBaseType as TypeCaveWall)?.SetConnectType(stat.Value);
                    break;
            }
        }
    }

    //public virtual void Draw() {
    //    Renderer?.Render();
    //}

    public virtual void Reset() {
        ObjectId = 0;

        Position.X = 0;
        Position.Y = 0;
        Z = 0;

        MovementVector.X = 0;
        MovementVector.Y = 0;

        Tile = null;
    }

    public AtlasData GetTexture() {
        return Texture;
    }

    public void SetAttack(int containerType, float attackAngle) {
        AttackStart = Main.GameTime.TotalMs;
        AttackAngle = attackAngle;
    }

    public virtual AtlasData GetTexture(double time, out bool attackFrame, out bool flipped) {
        const float ZeroLimit = 0.00001f;
        const float NegZeroLimit = -ZeroLimit;

        var texture = new AtlasData();
        var action = AnimationType.Stand;
        attackFrame = flipped = false;

        if (TextureData.HasAnimationData) {
            var idx = 0d;
            if (time < AttackStart + AttackPeriod) {
                attackFrame = true;
                action = AnimationType.Attack;
                idx = (time - AttackStart) % AttackPeriod / AttackPeriod;
                FacingAngle = AttackAngle;
            }
            else if (MovementVector != Vector2.Zero) {
                var walkPer = 0.5f / (MovementVector.Length * 4);
                walkPer = walkPer + (400 - walkPer % 400);

                if (MovementVector.X > ZeroLimit || MovementVector.X < NegZeroLimit || MovementVector.Y > ZeroLimit ||
                    MovementVector.Y < NegZeroLimit) {
                    FacingAngle = MathF.Atan2(MovementVector.Y, MovementVector.X);
                    action = AnimationType.Walk;
                }
                else {
                    action = AnimationType.Stand;
                }

                idx = time % walkPer / walkPer;
            }

            texture = TextureData.AnimatedTextures.TextureFromFacing(FacingAngle, action, (float)idx, out attackFrame,
                out flipped);
        }

        return texture;
    }


    // we should move this shit somewhere else too
    public void AnimateCharacter(double time) {
        Texture = GetTexture(time, out var attackFrame, out Flipped);
        RenderBaseType.SetTexture(Texture, attackFrame);
    }

    private void SetAltTexture(int index) {
        if (!ObjectLibrary.TypeToTextureData.TryGetValue(Type, out var baseTextureData))
            return;
        if (index == 0 || index == -1) {
            TextureData = baseTextureData;
        }
        else {
            if (baseTextureData.GetAltTexture(index, out var altData))
                TextureData = altData;
            else
                TextureData = baseTextureData;
        }

        if (TextureData.HasAnimationData && TextureData.AnimatedTextures.FaceRight != null &&
            TextureData.AnimatedTextures.FaceRight.Length > 0)
            Texture = TextureData.AnimatedTextures.FaceRight[0];
        else
            Texture = TextureData.GetTexture(true);
        RenderBaseType?.SetTexture(Texture);
    }

    public void OnAddedToMap(Position position) {
        PositionAtTick = Position;
        TickPosition = Position;

        if (!MoveTo(position.X, position.Y, false))
            Log.Error(
                $"Failed to add entity {ObjectId} (type=0x{Type:X4}) to map at ({position.X}, {position.Y}). Map size: {Map.Width}x{Map.Height}, tiles null: {Map._tiles == null}");

        Effect?.SetEntityPosition(Position);
        // Add entity's effect
    }

    public void OnRemovedFromMap() {
        if (Properties.Static && Tile != null) {
            if (Tile.OccupiedObject == this)
                Tile.OccupiedObject = null;

            Tile = null;
        }

        // Remove entity's effect

        // Remove dmg counter

        // Clear Madness dict

        // Dispose
    }

    public static int DamageWithDefense(int damage, int defense, bool nodef, Entity entity, int penetration = 0) {
        if (entity.HasConditionEffect(ConditionEffects.Invulnerable) ||
            entity.HasConditionEffect(ConditionEffects.Invincible))
            return 0;

        if (entity.HasConditionEffect(ConditionEffects.Armored))
            defense *= 2;

        if (nodef) {
            defense = 0;
        } else if (entity.HasConditionEffect(ConditionEffects.ArmorBroken)) {
            defense = entity.ArmorBreakMult > 0
                ? defense - (int)(defense * (entity.ArmorBreakMult * 0.01f))
                : 0;
        }

        if (penetration > 0)
            defense = (int)(defense * (1 - penetration * 0.01f));

        var limit = damage * 0.15f;
        float ret;

        if (entity.HasConditionEffect(ConditionEffects.Exposed))
            ret = entity.ExposedMult > 0 ? damage - (defense - entity.ExposedMult) : damage - (defense - 20);
        else
            ret = damage - defense;

        if (damage - defense < limit)
            ret = limit;

        if (entity.HasConditionEffect(ConditionEffects.Curse))
            ret = entity.CurseMult > 0 ? ret * (1 + entity.CurseMult * 0.01f) : ret * 1.2f;

        if (entity.HasConditionEffect(ConditionEffects.Protected))
            ret *= 0.25f;

        Log.Info(ret);
        return (int)ret;
    }

    public static uint GetHitColour(ConditionEffectDesc[] effects, bool armorBreak) {
        if (effects != null && effects.Length > 0) {
            var firstEffect = effects[0];
            return GetHitColour((ConditionEffectIndex)firstEffect.EffectId, armorBreak);
        }

        return armorBreak ? 0x9000FFu : 0xFF0000u;
    }

    public static uint GetHitColour(ConditionEffectIndex effect, bool armorBreak) {
        switch (effect) {
            case ConditionEffectIndex.Quiet:
                return 0xe6e6e6u;
            case ConditionEffectIndex.Weak:
                return 0xa8a8a8u;
            case ConditionEffectIndex.Slowed:
                return 0xaabf71u;
            case ConditionEffectIndex.Sick:
                return 0x0f4512u;
            case ConditionEffectIndex.Dazed:
                return 0xc5c977u;
            case ConditionEffectIndex.Stunned:
                return 0xe3b540u;
            case ConditionEffectIndex.Blind:
                return 0x0f0f0fu;
            case ConditionEffectIndex.Hallucinating:
                return 0xff00fbu;
            case ConditionEffectIndex.Drunk:
                return 0x8c9c43u;
            case ConditionEffectIndex.Paralyzed:
                return 0xe0e0b4u;
            case ConditionEffectIndex.Bleeding:
                return 0x330c0cu;
            case ConditionEffectIndex.Stasis:
                return 0x363636u;
            case ConditionEffectIndex.ArmorBroken:
                return 0x28184au;
            case ConditionEffectIndex.Unstable:
                return 0x764bd1u;
            case ConditionEffectIndex.Darkness:
                return 0x000000u;
            case ConditionEffectIndex.Petrify:
                return 0x171717u;
            case ConditionEffectIndex.Curse:
                return 0x4a1d1du;
            case ConditionEffectIndex.Exposed:
                return 0xd1dfe0u;
            case ConditionEffectIndex.Silenced:
                return 0xffffffu;
            default:
                return armorBreak ? 0x9000FFu : 0xFF0000u;
        }
    }

    public void Damage(int damageAmount, ConditionEffectIndex effect = ConditionEffectIndex.None,
        bool isArmorPiercing = false) {
        if (HasConditionEffect(ConditionEffects.Invincible) || HasConditionEffect(ConditionEffects.Invulnerable))
            return;

        if (Map.LocalPlayer == null)
            return;

        if (effect != ConditionEffectIndex.None)
            ConditionEffects |= (ConditionEffects)(1UL << (int)effect);

        if (damageAmount > 0) {
            var newHealth = Map.LocalPlayer.Hp - damageAmount;
            var notification = $"-{damageAmount} [{newHealth}]";
            var clr = GetHitColour(effect, isArmorPiercing);
            Map.AddParticleEffect(new HitEffect(Map.LocalPlayer, clr));
            NotificationLayer.AddStatusText(Map.LocalPlayer, notification, clr, 1000, 0);
        }
    }

    private static bool IsBigBag(ushort type) {
        return type switch {
            0x0509 or 0x050B or 0x050C or 0x050E or 0x050F or 0x0510 or
                0x040D or 0x1591 or 0x1595 or 0x15AB or 0x1606 or 0x04F4 or
                0x04F5 or 0x042D or 0x0427 or 0x04F6 or 0x04F7 or 0x04F8 or
                0x1596 or 0x1597 or 0x158F or 0x15AC or 0x1607 => true,
            _ => false
        };
    }

    #region StatData

    public string Name;

    public int Texture1Id;
    public int Texture2Id;

    public int GlowColor;
    public ushort AltItemID;
    private int _lastAltTex;

    public int MaxHp;
    public int Hp;
    public int Defense;
    public int Size = 100;
    public int Level;

    public int AnimationMin = -1;
    public int AnimationMax = -1;
    public int AnimationSpeed;
    private int _animationMinOld = -1;
    private double _animationStart;

    public ItemDesc[] Equipment = new ItemDesc[20];

    public ConditionEffects ConditionEffects = 0;

    public int ConnectType;

    public int PlayerHost;

    public bool PermaPet;

    public int MadnessPullRadius;
    public int MadnessPullMaxSpeed;

    public int QuestGlowColor;

    public int MinimumHp;

    public bool DontFaceAttacks;

    public bool AllDebuffsImmune;

    public int DamagersCount;

    public int CustomTexture;

    public bool PortalUsable;

    #endregion

}


internal readonly struct EntityFlashDescription {
    public readonly bool Active;
    public readonly double StartTimeMs;
    public readonly uint Color;
    public readonly float PeriodMs;
    public readonly int Repeats;
    public readonly float R;
    public readonly float G;
    public readonly float B;

    public EntityFlashDescription(double startTimeMs, uint color, float periodMs, int repeats) {
        Active = true;
        StartTimeMs = startTimeMs;
        Color = color;
        PeriodMs = Math.Max(1f, periodMs);
        Repeats = Math.Max(1, repeats);
        R = (color >> 16) & 0xFF;
        G = (color >> 8) & 0xFF;
        B = color & 0xFF;
    }
}

