using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using AlloyClient.State;
using AlloyClient.UiLib.Signals;
using AlloyClient.Utils;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public class Entity {
    private static readonly Logger Log = new(nameof(Entity));

    public static float AttackPeriod = 300;

    public int ObjectId;
    public ushort Type;
    
    public readonly Signal<int> InventoryUpdate = new();

    public float HeightOffset;
    public Vector2 Position;
    public float Rotation;
    public float Z;

    public Vector2 MovementVector;
    public Vector2 TickPosition;
    public Vector2 PositionAtTick;

    public int LastTickId;
    public double LastTickUpdateTime;

    public MapTile Tile;
    public ObjectProperties Properties;
    
    public double AttackStart;
    public float AttackAngle;

    #region StatData

    public string Name;

    public int Texture1Id;
    public int Texture2Id;

    public int GlowColor;
    public ushort AltItemID;
    private int _lastAltTex = 0;
    
    public int MaxHp;
    public int Hp;
    public int Defense;
    public int Size = 100;
    public int Level;

    public int AnimationMin = -1;
    public int AnimationMax = -1;
    public int AnimationSpeed = 0;
    private int _animationMinOld = -1;
    private double _animationStart = 0;
    
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

    public RenderBase RenderBaseType;

    public TextureData TextureData;

    public AtlasData Texture;
    
    public bool Flipped;

    public float FacingAngle;
    
    public float Jitter;

    public ParticleEffect Effect;
    
    public float Alpha = 1.0f;
    
    // Merchant Specific

    public int merchandiseType_;

    public void SetObjectId(int id) {
        ObjectId = id;
        Jitter = Random.Shared.NextSingle() * 0.00002f - 0.00001f;
        Effect = ParticleEffect.FromProperties(Properties.Effect, this);
    }

    public void SetType(ushort type)
    {
        Type = type;

        if (!ObjectLibrary.TypeToTextureData.TryGetValue(type, out var textureData))
        {
            Log.Warn($"No texture data found for type 0x{type:X4}, using fallback.");
            textureData = ObjectLibrary.TypeToTextureData[ObjectLibrary.FallbackType];
        }

        TextureData = textureData;

        if (TextureData.HasAnimationData
            && TextureData.AnimatedTextures.FaceRight != null
            && TextureData.AnimatedTextures.FaceRight.Length > 0)
        {
            Texture = TextureData.AnimatedTextures.FaceRight[0];
        }
        else
        {
            Texture = TextureData.GetTexture(random: true);
        }

        RenderBaseType = GetRenderType(type);
    }
    
    public void SetTransparency(float alpha) {
        Alpha = Math.Clamp(alpha, 0f, 1f);

        if (RenderBaseType != null) {
            RenderBaseType.Extra.Alpha = Alpha;
        }
    }
    
    public Color GetDominateColor() {
        if (RenderBaseType is TypeWall) return TextureData.TopTexture.DominantColor;
        return TextureData.DominantColor;
    }

    protected virtual RenderBase GetRenderType(ushort type) {
        ObjectLibrary.TypeToObjectProps.TryGetValue(type, out var props);

        if (props == null) {
            return new TypeNullObject();
        }

        if (props.RealSize != -1) {
            Size = props.RealSize;
        }

        if (props.MinSize != props.MaxSize) {
            var maxSteps = (props.MaxSize - props.MinSize) / props.SizeStep;
            Size = props.MinSize + (int)(Random.Shared.NextSingle() * maxSteps) * props.SizeStep;
        }

        if (!string.IsNullOrEmpty(props.Model)) {
            return new TypeModel3D(props.Model, this);
        }
        
        if (props.DrawOnGround) {
            return new TypeGroundObject(this);
        }

        return props.Class switch {
            "Wall" => new TypeWall(this),
            "DoubleWall" => new TypeWall(this, ModelType.PbDoubleWall),
            "DoubleWall2" => new TypeWall(this, ModelType.PbDoubleWall), // ModelType.PbDoubleWall2
            "TripleWall" => new TypeWall(this, ModelType.PbDoubleWall), // ModelType.PbTripleWall
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
        if (MovementVector is not {X: 0, Y: 0}) {
            if (LastTickId >= Map.LastTickId) {
                var tickDt = time - LastTickUpdateTime;
                var pX = PositionAtTick.X + tickDt * MovementVector.X;
                var pY = PositionAtTick.Y + tickDt * MovementVector.Y;
                MoveTo((float) pX, (float) pY);
            } else {
                MovementVector.X = 0;
                MovementVector.Y = 0;
                MoveTo(TickPosition.X, TickPosition.Y);
            }
        } else {
            MovementVector.X = 0;
            MovementVector.Y = 0;
        }

        // add wall support here at some point
        if (TextureData.HasAnimationData) {
            AnimateCharacter(time);
        }
        
        if (AnimationSpeed > 0) {
            if (_animationMinOld != AnimationMin) {
                _animationStart = time;
                _animationMinOld = AnimationMin;
            }
            ClientAnimation(time);
        }
        
        Effect?.Update(time, dt);
        RenderBaseType.SetPosition(Position.X, Position.Y, Z);
        return true;
    }
    
    private void ClientAnimation(double time) {
        if (AnimationMax <= 0) return;
    
        var frame = (int)(Math.Floor((time - _animationStart) / AnimationSpeed) % AnimationMax) + AnimationMin;
    
        if (frame == _lastAltTex) return;
        _lastAltTex = frame;
    
        SetAltTexture(frame);
    }
    
    public void UpdateVisibility(ref Matrix4 matrix) {
        var dx = Position.X - Camera.Position.X;
        var dy = Position.Y + Camera.Position.Y;
        var distanceSquared = dx * dx + dy * dy;
        int playerSightRadiusSquared = Map.LocalPlayer.UpdateRadius * Map.LocalPlayer.UpdateRadius ;

        bool visible = distanceSquared <= playerSightRadiusSquared;
        RenderBaseType.SetVisibility(visible);
        
        //TODO: double check mg to make sure
        //var sort = Vector3.Transform(new Vector3(Position.X, Position.Y, 0), matrix).Y;
        var sort = Vector3.TransformPerspective(new Vector3(Position.X, Position.Y, 0), matrix).Y;
        RenderBaseType.SetDepth(0.5f + 0.4f * sort + Jitter);
    }

    public bool MoveTo(float x, float y) {
        var tile = Map.GetTile((int)x, (int)y);

        if (tile == null) {
            return false;
        }
        
        if (tile.OccupiedObject != null && tile.OccupiedObject.Properties.OccupySquare) {
            return false;
        }

        Position.X = x;
        Position.Y = y;

        if (Properties.Static) {
            if (Tile != null) {
                Tile.OccupiedObject = null;
            }

            tile.OccupiedObject = this;
        }

        Tile = tile;

        return true;
    }

    public void OnTickPosition(float x, float y, double tickTime, int tickId, bool isPlayer) {
        if (!Settings.MovementInterpolation && LastTickId < Map.LastTickId && !isPlayer) {
            MoveTo(TickPosition.X, TickPosition.Y);
        }

        TickPosition.X = x;
        TickPosition.Y = y;
        LastTickId = tickId;
        LastTickUpdateTime = tickTime;
        PositionAtTick.X = Position.X;
        PositionAtTick.Y = Position.Y;

        if (!isPlayer) {
            MovementVector.X = (float)((TickPosition.X - PositionAtTick.X) / tickTime);
            MovementVector.Y = (float)((TickPosition.Y - PositionAtTick.Y) / tickTime);
        }
    }

    public virtual void UpdateStats(StatData[] statData, int offset, int count) {
        for (var i = 0; i < count; i++) {
            var stat = statData[offset + i];
            switch (stat.Type) {
                case StatsType.MaximumHp:
                    MaxHp = stat.Value;
                    break;
                case StatsType.Hp:
                    Hp = stat.Value;
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
                    } else if (Equipment[index] == null || (Equipment[index] != null && stat.Value != Equipment[index].ObjectType)) {
                        if(ObjectLibrary.TypeToItem.TryGetValue((ushort)stat.Value, out var b))
                        {
                            Equipment[index] = b;
                        }
                        else
                        {
                            Equipment[index] = null;
                        }
                    }
                    InventoryUpdate.Dispatch(index);
                    break;
                case StatsType.Effects:
                    ConditionEffects = (ConditionEffects)stat.Value;
                    SetTransparency(HasConditionEffect(ConditionEffects.Invisible) ? 0.5f : 1.0f);
                    break;
                case StatsType.Name:
                    if (Name != stat.Text) {
                        Name = string.Intern(stat.Text);
                        RenderBaseType.SetName(stat.Text);
                    }
                    break;
                case StatsType.Texture1:
                    if (stat.Value == Texture1Id) {
                        break;
                    }

                    Texture1Id = stat.Value;
                    // TexturingCache = new Dict;
                    // Portrait = null;
                    break;
                case StatsType.Texture2:
                    if (stat.Value == Texture2Id) {
                        break;
                    }

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
                    if (stat.Value <= 0) {
                        break;
                    }
                    
                    AltItemID = (ushort)stat.Value;
                    if (!ObjectLibrary.TypeToTextureData.TryGetValue(AltItemID, out var textureData)) {
                        textureData = ObjectLibrary.TypeToTextureData[ObjectLibrary.FallbackType];
                    }
                    
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
                    } else if (Equipment[index] == null || (Equipment[index] != null && stat.Value != Equipment[index].ObjectType)) {
                        if (ObjectLibrary.TypeToItem.TryGetValue((ushort)stat.Value, out var b))
                        {
                            Equipment[index] = b;
                        }
                        else
                        {
                            Equipment[index] = null;
                        }
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
        attackFrame = false;
        flipped = false;

        if (!TextureData.HasAnimationData)
            return Texture;

        var action = AnimationType.Stand;
        var idx = 0d;

        if (time < AttackStart + AttackPeriod) {
            action = AnimationType.Attack;
            idx = (time - AttackStart) % AttackPeriod / AttackPeriod;
            FacingAngle = AttackAngle;
        } else if (MovementVector != Vector2.Zero) {
            var walkPer = 0.5f / MovementVector.Length;
            walkPer = walkPer + (400 - walkPer % 400);
            FacingAngle = MathF.Atan2(MovementVector.Y, MovementVector.X);
            action = AnimationType.Walk;
            idx = time % walkPer / walkPer;
        }

        return TextureData.AnimatedTextures.TextureFromFacing(FacingAngle, action, (float)idx, out attackFrame, out flipped);
    }

    // we should move this shit somewhere else too
    public void AnimateCharacter(double time) {
        Texture = GetTexture(time, out var attackFrame, out Flipped);
        RenderBaseType.SetTexture(Texture, attackFrame);
    }

    private void SetAltTexture(int index) {
        if (!ObjectLibrary.TypeToTextureData.TryGetValue(Type, out var baseTextureData)) {
            return;
        }
        if (index == 0 || index == -1) {
            TextureData = baseTextureData;
        } 
        else {
            if (baseTextureData.GetAltTexture(index, out var altData)) {
                TextureData = altData;
            } else {
                TextureData = baseTextureData; 
            }
        }
        if (TextureData.HasAnimationData && TextureData.AnimatedTextures.FaceRight != null && TextureData.AnimatedTextures.FaceRight.Length > 0) {
            Texture = TextureData.AnimatedTextures.FaceRight[0];
        } else {
            Texture = TextureData.GetTexture(random: true);
        }
        RenderBaseType?.SetTexture(Texture);
    }

    public void OnAddedToMap(Position position) {
        PositionAtTick = Position;
        TickPosition = Position;

        if (!MoveTo(position.X, position.Y))
        {
            Log.Error($"Failed to add entity {ObjectId} (type=0x{Type:X4}) to map at ({position.X}, {position.Y}). Map size: {Map.Width}x{Map.Height}, tiles null: {Map._tiles == null}");
        }

        // Add entity's effect
    }

    public void OnRemovedFromMap() {
        if (Properties.Static && Tile != null) {
            if (Tile.OccupiedObject == this) {
                Tile.OccupiedObject = null;
            }

            Tile = null;
        }

        // Remove entity's effect

        // Remove dmg counter

        // Clear Madness dict

        // Dispose
    }
}