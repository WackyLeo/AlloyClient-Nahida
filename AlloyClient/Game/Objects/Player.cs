using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game.Objects.Enums;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking;
using AlloyClient.Networking.Enums;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.State;
using AlloyClient.Utils;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public class Player : Entity {
    private const float MoveThreshold = 0.4f;
    private const int FocusedSpeed = 15;
    private const float MinMoveSpeed = 0.004f;
    private const float MaxMoveSpeed = 0.0096f;
    private const float MinAttackFreq = 0.0015f;
    private const float MaxAttackFreq = 0.008f;
    
    private static readonly Logger Log = new(nameof(Player));
    
    public Vector2 RelativeMoveVector;
    public float MovementMultiplier = 1;
    public float Rotate;

    public int NextBulletId = 0;
    public int NextBulletId2 = 0;
    
    public bool Focused;
    public int SinkLevel;

    public bool Locked;
    public bool Ignored;

    #region StatData

    public int Restoration;
    public int MaxShield;
    public int Shield;
    public int MaxMp;
    public int Mp;
    public int Attack;
    public int Speed;
    public int Dexterity;
    public int Vitality;
    public int Wisdom;

    public int MaxHpBoost;
    public int MaxMpBoost;
    public int AttackBoost;
    public int DefenseBoost;
    public int SpeedBoost;
    public int DexterityBoost;
    public int VitalityBoost;
    public int WisdomBoost;
    
    public double AttackPeriod;
    public double AttackStart;

    public int AccountId;

    public int NextLevelExp = 1000;
    public int Experience;

    public int Stars;

    public int Tribute;
    public int Credits;
    public int Fame;
    
    public int CurrentFame;
    public int FameGoal;

    public bool NameChosen = true;

    public string Guild;
    public int GuildRank;

    public int OxygenBar;

    public int HealthStackCount;
    public int MagicStackCount;

    public ushort Skin;

    public int PartyId;

    public int LdBoosted;
    public int LdBoostAmount;

    public int XpBoostTime;

    public bool HasBackPack;

    public bool IsFellowGuild;

    #endregion

    protected override RenderBase GetRenderType(ushort type) {
        ObjectLibrary.TypeToObjectProps.TryGetValue(type, out var props);
        if (props == null) {
            return null;
        }

        if (props.RealSize != -1) {
            Size = props.RealSize;
        }

        if (props.MinSize != props.MaxSize) {
            var maxSteps = (props.MaxSize - props.MinSize) / props.SizeStep;
            Size = props.MinSize + (int) (Random.Shared.NextSingle() * maxSteps) * props.SizeStep;
        }

        return new TypePlayer(this);
    }

    private void HandleRelativeMovement(double time, double dt) {
        float angle = Settings.CameraAngle;

        if (Rotate != 0) {
            angle = (float) (angle + dt * Settings.RotateSpeed * Rotate);
            Settings.CameraAngle = (angle % MathHelper.TwoPi + MathHelper.TwoPi) % MathHelper.TwoPi;
        }
        
        var moveSpeed = GetMoveSpeed();
            var moveVectorAngle = MathF.Atan2(RelativeMoveVector.Y, RelativeMoveVector.X);

            // TODO: Madness debuffs and dashes
            if (RelativeMoveVector.X != 0 || RelativeMoveVector.Y != 0) {
                if (Tile.GroundProperties.SlideAmount > 0) {
                    var slideVector = new Vector2 {
                        X = moveSpeed * MathF.Cos(angle + moveVectorAngle),
                        Y = moveSpeed * MathF.Sin(angle + moveVectorAngle)
                    };

                    //TODO: double check monogame to make sure its the same thing
                    //var slideLen = slideVector.Length();
                    var slideLen = slideVector.LengthFast;
                    slideVector *= -1 * (Tile.GroundProperties.SlideAmount - 1);
                    MovementVector *= Tile.GroundProperties.SlideAmount;

                    if (MovementVector.LengthFast < slideLen) {
                        MovementVector += slideVector;
                    }
                }
                else {
                    MovementVector.X = moveSpeed * MathF.Cos(angle + moveVectorAngle);
                    MovementVector.Y = moveSpeed * MathF.Sin(angle + moveVectorAngle);
                }
            }
            else if (MovementVector.LengthFast > 0.00012 && Tile.GroundProperties.SlideAmount > 0) {
                MovementVector *= Tile.GroundProperties.SlideAmount;
            }
            else {
                MovementVector.X = 0;
                MovementVector.Y = 0;
            }

            // TODO: Push tiles
            // if (Tile.GroundProperties.Push) {
            //     MovementVector.X = MovementVector.X - Tile.GroundProperties.Animate.Dx / 1000;
            //     MovementVector.Y = MovementVector.Y - Tile.GroundProperties.Animate.Dy / 1000;
            // }

            if (TextureData.HasAnimationData && this == Map.LocalPlayer) {
                AnimateCharacter(time);
            }

            WalkTo((float) (Position.X + dt * MovementVector.X), (float) (Position.Y + dt * MovementVector.Y));
            RenderBaseType.SetPosition(Position.X, Position.Y, Z);
        
    }

    public override bool Update(double time, double dt) {
        if (ObjectId == Map.LocalPlayerId) {
            HandleRelativeMovement(time, dt);
        } else if (!base.Update(time, dt)) {
            return false;
        }
        Effect?.Update(time, dt);
        return true;
    }

    public override void UpdateStats(StatData[] statData, int offset, int count) {
        base.UpdateStats(statData, offset, count);

        #region Parse StatData

        for (var i = 0; i < count; i++) {
            var stat = statData[offset + i];
            switch (stat.Type) {
                case StatsType.MaximumMp:
                    MaxMp = stat.Value;
                    break;
                case StatsType.Mp:
                    Mp = stat.Value;
                    break;
                case StatsType.Attack:
                    Attack = stat.Value;
                    break;
                case StatsType.Speed:
                    Speed = stat.Value;
                    break;
                case StatsType.Dexterity:
                    Dexterity = stat.Value;
                    break;
                case StatsType.Vitality:
                    Vitality = stat.Value;
                    break;
                case StatsType.Wisdom:
                    Wisdom = stat.Value;
                    break;
                case StatsType.HpBoost:
                    MaxHpBoost = stat.Value;
                    break;
                case StatsType.MpBoost:
                    MaxMpBoost = stat.Value;
                    break;
                case StatsType.AttackBonus:
                    AttackBoost = stat.Value;
                    break;
                case StatsType.DefenseBonus:
                    DefenseBoost = stat.Value;
                    break;
                case StatsType.SpeedBonus:
                    SpeedBoost = stat.Value;
                    break;
                case StatsType.DexterityBonus:
                    DexterityBoost = stat.Value;
                    break;
                case StatsType.VitalityBonus:
                    VitalityBoost = stat.Value;
                    break;
                case StatsType.WisdomBonus:
                    WisdomBoost = stat.Value;
                    break;
                case StatsType.AccountId:
                    AccountId = stat.Value;
                    break;
                case StatsType.ExperienceGoal:
                    NextLevelExp = stat.Value;
                    break;
                case StatsType.Experience:
                    Experience = stat.Value;
                    break;
                case StatsType.Fame:
                    CurrentFame = stat.Value;
                    break;
                case StatsType.FameGoal:
                    FameGoal = stat.Value;
                    break;
                /*case StatsType.NumStars:
                    Stars = stat.Value;
                    break;*/
                case StatsType.Credits:
                    Credits = stat.Value;
                    break;
                case StatsType.CurrentFame:
                    Fame = stat.Value;
                    break;
                case StatsType.CurrentTribute: // Atm we just double up
                case StatsType.Tribute:
                    Tribute = stat.Value;
                    break;
                /*case StatsType.CurrentFame:
                    CurrentFame = stat.Value;
                    break;
                case StatsType.NextClassQuestFame:
                    FameGoal = stat.Value;
                    break;*/
                case StatsType.Guild:
                    if (Guild != stat.Text)
                        Guild = string.Intern(stat.Text);
                    break;
                case StatsType.GuildRank:
                    GuildRank = stat.Value;
                    break;
                /*case StatsType.Oxygen:
                    OxygenBar = stat.Value;
                    break;*/
                case StatsType.HealthStackCount:
                    HealthStackCount = stat.Value;
                    break;
                case StatsType.MagicStackCount:
                    MagicStackCount = stat.Value;
                    break;
                case StatsType.Skin:
                    Skin = (ushort)stat.Value;
                    SetPlayerSkinTemplate(Skin);
                    break;
                case StatsType.HasBackpack:
                    HasBackPack = stat.Value != 0;
                    // add backpack signal
                    break;
                case StatsType.MaxShield:
                    MaxShield = stat.Value;
                    break;
                case StatsType.Shield:
                    Shield = stat.Value;
                    break;
                case StatsType.Restoration:
                    Restoration = stat.Value;
                    break;
            }
        }

        #endregion

    }

    public override AtlasData GetTexture(double time, out bool attackFrame, out bool flipped) {
        var texture = new AtlasData();
        
        var action = AnimationType.Stand;
        var idx = 0d;
        
        if (time < AttackStart + AttackPeriod) {
            action = AnimationType.Attack;
            idx = (time - AttackStart) % AttackPeriod / AttackPeriod;
            FacingAngle = AttackAngle;
        } else if (MovementVector != Vector2.Zero) {
            var walkPer = 3.5f / GetMoveSpeed();
            FacingAngle = MathF.Atan2(MovementVector.Y, MovementVector.X);
            action = AnimationType.Walk;
            idx = time % walkPer / walkPer;
        }

        texture = TextureData.AnimatedTextures.TextureFromFacing(FacingAngle, action, (float) idx, out attackFrame, out flipped);

        return texture;
    }

    private void SetPlayerSkinTemplate(ushort skin) {
        if (skin == 0) return;

        TextureData = ObjectLibrary.TypeToTextureData[skin];
        Texture = TextureData.HasAnimationData ? TextureData.AnimatedTextures.FaceRight[0] : TextureData.GetTexture();
        RenderBaseType.SetTexture(Texture, false);
    }

    private void WalkTo(float x, float y) {
        var pos = ModifyMove(x, y);
        MoveTo(pos.X, pos.Y);

        Camera.Update(pos.X, pos.Y);
    }

    private float AttackFrequency() {
        if (HasConditionEffect(ConditionEffects.Dazed)) {
            return MinAttackFreq;
        }
        
        var attFreq = MinAttackFreq + Dexterity / 75f * (MaxAttackFreq - MinAttackFreq);

        if (HasConditionEffect(ConditionEffects.Berserk))
            attFreq *= 1.25f;
        
        return attFreq;
    }

    public void Shoot(float attackAngle, GameTime gameTime, bool isAbility) {
        if (HasConditionEffect(ConditionEffects.Stunned) || HasConditionEffect(ConditionEffects.Paused))
            return;
        
        var item = Equipment[0];
        if (item == null)
            return;
        var temp = AttackFrequency();
        
        AttackPeriod = 1 / temp * (1 / item.RateOfFire);
        if (gameTime.TotalMs < AttackStart + AttackPeriod)
            return;
        AttackAngle = attackAngle;
        AttackStart = gameTime.TotalMs;
        
        var props = ObjectLibrary.TypeToObjectProps[item.ObjectType];
        var shootPacket = PlayerShoot.CreatePacket();

        var arc = MathHelper.DegreesToRadians(props.ArcGap) * (props.NumProjectiles - 1);
        var startAngle = attackAngle - arc / 2;

        var startCount = item.StartCount;
        var nextCount = item.NextCount;
        var projTypeIndex = 0;

        for (int i = 0; i < props.NumProjectiles; i++) {
            var projDesc = item.AllProjectiles[projTypeIndex % item.AllProjectiles.Length];
            var projObjType = ObjectLibrary.IdToObjectType[projDesc.ObjectId];
            var objProps = ObjectLibrary.TypeToObjectProps[projObjType];
            var angle = startAngle + MathHelper.DegreesToRadians(props.ArcGap) * i;
            var bId = isAbility ? GetBulletId2() : GetBulletId();
            var rawDmg = MathUtils.RandomInt(projDesc.MinDamage, projDesc.MaxDamage);
            var dmg = (int)(rawDmg * (0.5f + (Attack / 50f))); 

            if (!props.ProjectilesDict.TryGetValue(projDesc.BulletType, out var projProps))
                projProps = props.Projectiles[0];

            var proj = ObjectPools.Projectiles.Pop();
            proj.Reset(bId, dmg, angle, this, objProps, projProps);
            Map.AddProjectile(proj, projProps.ProjDelayMs);

            if (i == 0) {
                shootPacket.Shots.Add(new ShootData {
                    Time = (int)Main.GameTime.TotalMs,
                    BulletId = bId,
                    ItemType = item.ObjectType,
                    StartingPos = new Position { X = Position.X, Y = Position.Y },
                    Angle = angle
                });
            }
            
            startCount++;
            if (startCount % nextCount == 0)
                projTypeIndex++;
        }

        Client.QueuePacket(shootPacket);
    }
    
    private Vector2 ModifyMove(float x, float y) {
        var result = new Vector2();

        // if (para and statis dont move) {
        //     result.X = X;
        //     result.Y = Y;
        // }

        var dX = x - Position.X;
        var dY = y - Position.Y;

        if (dX < MoveThreshold && dX > -MoveThreshold && dY < MoveThreshold && dY > -MoveThreshold) {
            result = ModifyStep(x, y);
            return result;
        }

        result.X = Position.X;
        result.Y = Position.Y;

        var stepSize = MoveThreshold / Math.Max(Math.Abs(dX), Math.Abs(dY));
        var d = 0.0f;
        var done = false;

        while (!done) {
            if (d + stepSize >= 1) {
                stepSize = 1 - d;
                done = true;
            }

            result = ModifyStep(result.X + dX * stepSize, result.Y + dY * stepSize);
            d += stepSize;
        }

        return result;
    }

    // Try to keep it as close to the original as possible?
    // Don't wanna mess with it too much.
    // ReSharper disable PossibleLossOfFraction
    // ReSharper disable CompareOfFloatsByEqualityOperator
    private Vector2 ModifyStep(float x, float y) {
        var xCross = Position.X % 0.5f == 0 && x != Position.X || (int) (Position.X / 0.5f) != (int) (x / 0.5f);
        var yCross = Position.Y % 0.5f == 0 && y != Position.Y || (int) (Position.Y / 0.5f) != (int) (y / 0.5f);

        if (!xCross && !yCross || IsValidPosition(x, y)) {
            return new Vector2(x, y);
        }

        float nextXBorder = 0;
        float nextYBorder = 0;

        if (xCross) {
            nextXBorder = x > Position.X ? (int) (x * 2) / 2f : (int) (Position.X * 2) / 2f;

            if ((int) nextXBorder > (int) Position.X) {
                nextXBorder -= 0.01f;
            }
        }

        if (yCross) {
            nextYBorder = y > Position.Y ? (int) (y * 2) / 2f : (int) (Position.Y * 2) / 2f;

            if ((int) nextYBorder > (int) Position.Y) {
                nextYBorder -= 0.01f;
            }
        }

        if (!xCross) {
            if (Tile.GroundProperties.SlideAmount == 0) {
                return new Vector2(x, nextYBorder);
            }

            MovementVector *= -0.5f;
            MovementVector.X *= -1f;

            return new Vector2(x, nextYBorder);
        }

        if (!yCross) {
            if (Tile.GroundProperties.SlideAmount == 0) {
                return new Vector2(nextXBorder, y);
            }

            MovementVector *= -0.5f;
            MovementVector.Y *= -1f;

            return new Vector2(nextXBorder, y);
        }

        var xBorderDist = x > Position.X ? x - nextXBorder : nextXBorder - x;
        var yBorderDist = y > Position.Y ? y - nextYBorder : nextYBorder - y;

        if (xBorderDist > yBorderDist) {
            if (IsValidPosition(x, nextYBorder)) {
                return new Vector2(x, nextYBorder);
            }

            if (IsValidPosition(nextXBorder, y)) {
                return new Vector2(nextXBorder, y);
            }
        }
        else {
            if (IsValidPosition(nextXBorder, y)) {
                return new Vector2(nextXBorder, y);
            }

            if (IsValidPosition(x, nextYBorder)) {
                return new Vector2(x, nextYBorder);
            }
        }

        return new Vector2(nextXBorder, nextYBorder);
    }

    private bool IsValidPosition(float x, float y) {
        var tile = Map.GetTile((int) x, (int) y);

        if (Tile != tile && (tile == null || !tile.IsWalkable())) {
            return false;
        }

        var xFrac = x - (int) x;
        var yFrac = y - (int) y;

        if (xFrac < 0.5) {
            if (IsFullOccupy(x - 1, y)) {
                return false;
            }

            if (yFrac < 0.5) {
                if (IsFullOccupy(x, y - 1) || IsFullOccupy(x - 1, y - 1)) {
                    return false;
                }
            }
            else if (yFrac > 0.5) {
                if (IsFullOccupy(x, y + 1) || IsFullOccupy(x - 1, y + 1)) {
                    return false;
                }
            }
        }
        else if (xFrac > 0.5) {
            if (IsFullOccupy(x + 1, y)) {
                return false;
            }

            if (yFrac < 0.5) {
                if (IsFullOccupy(x, y - 1) || IsFullOccupy(x + 1, y - 1)) {
                    return false;
                }
            }
            else if (yFrac > 0.5) {
                if (IsFullOccupy(x, y + 1) || IsFullOccupy(x + 1, y + 1)) {
                    return false;
                }
            }
        }
        else if (yFrac < 0.5) {
            if (IsFullOccupy(x, y - 1)) {
                return false;
            }
        }
        else if (yFrac > 0.5) {
            if (IsFullOccupy(x, y + 1)) {
                return false;
            }
        }

        return true;
    }

    public void SetRelativeMovement(float rotate, float relMoveVecX, float relMoveVecY) {
        Rotate = rotate;
        RelativeMoveVector.X = relMoveVecX;
        RelativeMoveVector.Y = relMoveVecY;

        if (false) {
            // Confused
            var temp = RelativeMoveVector.X;
            RelativeMoveVector.X = -RelativeMoveVector.Y;
            RelativeMoveVector.Y = -temp;
            Rotate = -Rotate;
        }
    }

    private float GetMoveSpeed() {
        if (false) {
            // Slowed
            return MinMoveSpeed * MovementMultiplier;
        }

        var speed = Focused ? FocusedSpeed : Speed;
        var moveSpeed = MinMoveSpeed + speed / 75 * (MaxMoveSpeed - MinMoveSpeed);

        if (false || false) {
            // Speedy or NinjaSpeedy
            moveSpeed *= 1.5f;
        }

        if (false) {
            // Bunny Speedy
            moveSpeed *= 1.2f;
        }

        return moveSpeed * MovementMultiplier;
    }

    public int GetBulletId() {
        var ret = NextBulletId;
        Log.Info($"GetBulletId: returning {ret}, next will be {(NextBulletId + 1) % 1024}");
        NextBulletId = (NextBulletId + 1) % 1024;
        return ret;
    }
    
    public int GetBulletId2() {
        var ret = NextBulletId2 + 1024;
        NextBulletId2 = (NextBulletId2 + 1) % 512;
        return ret;
    }

    public int ReadServerId() {
        return NextBulletId2 + 1024;
    }

    private static bool IsFullOccupy(float x, float y) {
        var tile = Map.GetTile((int) x, (int) y);

        if (tile == null) {
            return true;
        }

        if (tile.Type == 255) {
            return true;
        }

        if (tile.OccupiedObject?.Properties.FullOccupy == true) {
            return true;
        }

        return false;
    }

    public void OnMove() {
        var tile = Map.GetTile((int) Position.X, (int) Position.Y);

        if (tile == null) {
            return;
        }

        // if (tile.GroundProperties.Interactive) {
        //     var activateGround = ActivateGround.CreatePacket();
        //     activateGround.X = tile.X;
        //     activateGround.Y = tile.Y;
        //     Client.QueuePacket(activateGround);
        // }
        //
        // var interactiveObject = Map.GetInteractiveObject((int) Position.X, (int) Position.Y);
        // if (interactiveObject != null) {
        //     var objectInteract = ObjectInteract.CreatePacket();
        //     objectInteract.ObjectId = interactiveObject.ObjectId;
        //     Client.QueuePacket(objectInteract);
        // }

        const float maxSinkLevel = 18;

        if (tile.GroundProperties is { Sinking: true }) {
            // TODO: IMPORTANT! PHARAOH"S CURSE FORCE SINK NEEDS TO BE IMPLEMENTED
            SinkLevel = (int) MathF.Min(SinkLevel + 1, maxSinkLevel);
            MovementMultiplier = 0.1f + (1 - SinkLevel / maxSinkLevel) * (tile.GroundProperties.Speed - 0.1f);
        }
        else {
            SinkLevel = 0;
            MovementMultiplier = tile.GroundProperties.Speed;
        }
    }
}