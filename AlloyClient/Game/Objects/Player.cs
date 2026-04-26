using System;
using System.Collections.Generic;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game.Components;
using AlloyClient.Game.Objects.Enums;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking;
using AlloyClient.Networking.Enums;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.ParticleEffects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.Sound;
using AlloyClient.State;
using AlloyClient.Ui.Character;
using AlloyClient.Utils;
using Common;
using Common.Structs;
using OpenTK.Mathematics;

namespace AlloyClient.Game.Objects;

public class Player : Entity {
    private const float MoveThreshold = 0.4f;
    private const float MinMoveSpeed = 0.004f;
    private const float MaxMoveSpeed = 0.0096f;
    private const float MinAttackFreq = 0.0015f;
    private const float MaxAttackFreq = 0.008f;
    private static readonly Logger Log = new(nameof(Player));

    public int DamageTakenMultiplier;

    // Other Player Stats:
    public bool Ignored;
    public bool Locked;

    // Movement
    public float MovementMultiplier = 1;

    public int NextBulletId;
    public int NextBulletId2;

    public List<ShootData> PendingShots = new();
    public Vector2 RelativeMoveVector;
    public float Rotate;
    public int SinkLevel;
    public int UpdateRadius = 20;


    protected override RenderBase GetRenderType(ushort type) {
        ObjectLibrary.TypeToObjectProps.TryGetValue(type, out var objProp);
        if (objProp == null) {
            return null;
        }

        if (objProp.Size != -1) {
            Size = objProp.Size;
        }

        if (objProp.MinSize != objProp.MaxSize) {
            var maxSteps = (objProp.MaxSize - objProp.MinSize) / objProp.SizeStep;
            Size = objProp.MinSize + (int)(Random.Shared.NextSingle() * maxSteps) * objProp.SizeStep;
        }

        return new TypePlayer(this);
    }

    private void HandleRelativeMovement(double time, double dt) {
        if (float.IsNaN(Position.X) || float.IsNaN(Position.Y))
            MoveTo(0, 0, true);

        var physicsDt = (float)Math.Clamp(dt, 0.0001, 33.3333);
        float angle = Settings.CameraAngle;

        if (Rotate != 0) {
            angle = angle + physicsDt * Settings.RotateSpeed * Rotate;
            Settings.CameraAngle = (angle % MathHelper.TwoPi + MathHelper.TwoPi) % MathHelper.TwoPi;
        }

        var moveSpeed = GetMoveSpeed();
        var baseSlideAmount = Tile?.GroundProperties?.SlideAmount ?? 0f;
        var isMoving = RelativeMoveVector.X != 0 || RelativeMoveVector.Y != 0;
        var targetVelocity = Vector2.Zero;

        if (isMoving) {
            var moveVectorAngle = MathF.Atan2(RelativeMoveVector.Y, RelativeMoveVector.X);
            targetVelocity = new Vector2(
                moveSpeed * MathF.Cos(angle + moveVectorAngle),
                moveSpeed * MathF.Sin(angle + moveVectorAngle)
            );
        }

        if (baseSlideAmount > 0) {
            var safeSlideAmount = Math.Min(0.99f, baseSlideAmount);
            var frictionMultiplier = MathF.Pow(safeSlideAmount, physicsDt / 16.666666f);

            if (float.IsNaN(frictionMultiplier) || float.IsInfinity(frictionMultiplier))
                frictionMultiplier = 0.95f;

            if (isMoving) {
                var newVel = MovementVector * frictionMultiplier + targetVelocity * (1f - frictionMultiplier);
                if (!float.IsNaN(newVel.X) && !float.IsInfinity(newVel.X))
                    MovementVector = newVel;
            }
            else {
                MovementVector *= frictionMultiplier;
                if (MovementVector.LengthSquared < 0.000001f)
                    MovementVector = Vector2.Zero;
            }
        }
        else {
            MovementVector = targetVelocity;
        }

        if (Tile?.GroundProperties?.Push == true) {
            MovementVector.X -= Tile.GroundProperties.Animate.DeltaX / 1000f;
            MovementVector.Y -= Tile.GroundProperties.Animate.DeltaY / 1000f;
        }

        if (float.IsNaN(MovementVector.X) || float.IsNaN(MovementVector.Y))
            MovementVector = Vector2.Zero;
        else if (MovementVector.LengthSquared > 400f)
            MovementVector = MovementVector.Normalized() * 20f;

        if (TextureData != null && TextureData.HasAnimationData && this == Map.LocalPlayer)
            AnimateCharacter(time);

        var nextX = Position.X + physicsDt * MovementVector.X;
        var nextY = Position.Y + physicsDt * MovementVector.Y;

        if (!double.IsNaN(nextX) && !double.IsNaN(nextY)) {
            var pos = ModifyMove(nextX, nextY);
            MoveTo(pos.X, pos.Y, true);

            Camera.Update(pos.X, pos.Y);
        }

        RenderBaseType?.SetPosition(Position.X, Position.Y, Z);
    }

    private Vector2 ModifyMove(float x, float y) {
        var result = new Vector2();

        var dX = x - Position.X;
        var dY = y - Position.Y;

        if (dX < MoveThreshold && dX > -MoveThreshold && dY < MoveThreshold && dY > -MoveThreshold) {
            result = ModifyStep(x, y);
            return result;
        }

        result.X = Position.X;
        result.Y = Position.Y;

        var maxDiff = Math.Max(Math.Abs(dX), Math.Abs(dY));
        var stepSize = MoveThreshold / maxDiff;

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

    private Vector2 ModifyStep(float x, float y) {
        var xCross = (Position.X % 0.5f == 0 && x != Position.X) || (int)(Position.X / 0.5f) != (int)(x / 0.5f);
        var yCross = (Position.Y % 0.5f == 0 && y != Position.Y) || (int)(Position.Y / 0.5f) != (int)(y / 0.5f);

        if ((!xCross && !yCross) || IsValidPosition(x, y))
            return new Vector2(x, y);

        float nextXBorder = 0;
        float nextYBorder = 0;

        if (Tile?.GroundProperties?.SlideAmount > 0)
            Log.Warn($"[BOUNCE] at ({x:F2}, {y:F2}) xCross: {xCross} yCross: {yCross}");

        if (xCross) {
            nextXBorder = x > Position.X ? (int)(x * 2) / 2f : (int)(Position.X * 2) / 2f;

            if ((int)nextXBorder > (int)Position.X)
                nextXBorder -= 0.01f;
        }

        if (yCross) {
            nextYBorder = y > Position.Y ? (int)(y * 2) / 2f : (int)(Position.Y * 2) / 2f;

            if ((int)nextYBorder > (int)Position.Y)
                nextYBorder -= 0.01f;
        }

        if (!xCross) {
            if (Tile.GroundProperties.SlideAmount == 0)
                return new Vector2(x, nextYBorder);

            MovementVector *= -0.5f;
            MovementVector.X *= -1f;

            return new Vector2(x, nextYBorder);
        }

        if (!yCross) {
            if (Tile.GroundProperties.SlideAmount == 0)
                return new Vector2(nextXBorder, y);

            MovementVector *= -0.5f;
            MovementVector.Y *= -1f;

            return new Vector2(nextXBorder, y);
        }

        var xBorderDist = x > Position.X ? x - nextXBorder : nextXBorder - x;
        var yBorderDist = y > Position.Y ? y - nextYBorder : nextYBorder - y;

        if (xBorderDist > yBorderDist) {
            if (IsValidPosition(x, nextYBorder))
                return new Vector2(x, nextYBorder);
            if (IsValidPosition(nextXBorder, y))
                return new Vector2(nextXBorder, y);
        }
        else {
            if (IsValidPosition(nextXBorder, y))
                return new Vector2(nextXBorder, y);
            if (IsValidPosition(x, nextYBorder))
                return new Vector2(x, nextYBorder);
        }

        return new Vector2(nextXBorder, nextYBorder);
    }

    private bool IsValidPosition(float x, float y) {
        var tile = Map.GetTile((int)x, (int)y);

        if (Tile != tile && (tile == null || !tile.IsWalkable()))
            return false;

        var xFrac = x - (int)x;
        var yFrac = y - (int)y;

        if (xFrac < 0.5) {
            if (IsFullOccupy(x - 1, y))
                return false;

            if (yFrac < 0.5) {
                if (IsFullOccupy(x, y - 1) || IsFullOccupy(x - 1, y - 1))
                    return false;
            }
            else if (yFrac > 0.5) {
                if (IsFullOccupy(x, y + 1) || IsFullOccupy(x - 1, y + 1))
                    return false;
            }
        }
        else if (xFrac > 0.5) {
            if (IsFullOccupy(x + 1, y))
                return false;

            if (yFrac < 0.5) {
                if (IsFullOccupy(x, y - 1) || IsFullOccupy(x + 1, y - 1))
                    return false;
            }
            else if (yFrac > 0.5) {
                if (IsFullOccupy(x, y + 1) || IsFullOccupy(x + 1, y + 1))
                    return false;
            }
        }
        else if (yFrac < 0.5) {
            if (IsFullOccupy(x, y - 1))
                return false;
        }
        else if (yFrac > 0.5) {
            if (IsFullOccupy(x, y + 1))
                return false;
        }

        return true;
    }

    public void SetRelativeMovement(float rotate, float relMoveVecX, float relMoveVecY) {
        Rotate = rotate;
        RelativeMoveVector.X = relMoveVecX;
        RelativeMoveVector.Y = relMoveVecY;
    }


    private static bool IsFullOccupy(float x, float y) {
        var tile = Map.GetTile((int)x, (int)y);

        if (tile == null)
            return true;

        if (tile.Type == 255)
            return true;

        if (tile.OccupiedObject?.Properties.FullOccupy == true)
            return true;

        return false;
    }

    public void OnMove() {
        var tile = Map.GetTile((int)Position.X, (int)Position.Y);
        if (tile == null)
            return;

        const float maxSinkLevel = 18f;
        var protectedFromSink = tile.OccupiedObject?.Properties.ProtectFromSink == true;
        if (!protectedFromSink && tile.GroundProperties.Sinking)
            SinkLevel = (int)MathF.Min(SinkLevel + 1, maxSinkLevel);
        else
            SinkLevel = 0;

        MovementMultiplier = tile.GroundProperties.Speed;
    }

    private float GetMoveSpeed() {
        // Paralyzed Stops movement
        if (HasConditionEffect(ConditionEffects.Paralyzed))
            return 0.0f;

        var speedy = HasConditionEffect(ConditionEffects.Speedy) || HasConditionEffect(ConditionEffects.NinjaSpeedy);
        var speed = Speed;

        // Outcast dash stuff
        if (DashTimer > 0) {
            return 0.06f;
        }

        // LimitSpeed/Crouch speed
        if (LimitSpeed) {
            speed = SpeedLimit <= speed && SpeedLimit >= 0 ? SpeedLimit : speed;
        }

        var moveSpeed = MinMoveSpeed + speed / 75f * (MaxMoveSpeed - MinMoveSpeed);
        if (speedy) {
            moveSpeed *= 1.5f;
        }

        return moveSpeed * MovementMultiplier;
    }

    public override bool Update(double time, double dt) {
        if (DashTimer > 0) {
            DashTimer -= (int)dt;
            if (DashTimer <= 0) {
                DashTimer = 0;
                IFrame = false;
            }
        }

        if (AutoAbility) {
            var target = Camera.ScreenToWorld(UserInput.MousePosition);
            UseAltWeapon(target.X, target.Y, 4);
        }

        if (ObjectId == Map.LocalPlayerId) {
            HandleRelativeMovement(time, dt);
            OnMove();

            if (Tile != null && Tile.GroundProperties.MaxDamage > 0) {
                var now = Main.GameTime.TotalMs;
                if (now > Tile.LastDamage + 500
                    && !HasConditionEffect(ConditionEffects.Invincible)
                    && !HasConditionEffect(ConditionEffects.Invulnerable)
                    && !IFrame
                    && (Tile.OccupiedObject == null || !Tile.OccupiedObject.Properties.ProtectFromGroundDamage)) {
                    var dmg = MathUtils.RandomInt(Tile.GroundProperties.MinDamage, Tile.GroundProperties.MaxDamage);
                    //Hp -= dmg;
                    Tile.LastDamage = now;

                    var pkt = GroundDamage.CreatePacket();
                    pkt.Time = (int)now;
                    pkt.Position = new Position { X = Position.X, Y = Position.Y };
                    Client.QueuePacket(pkt);

                    Map.AddParticleEffect(new HitEffect(this, 0xFF0000));
                    NotificationLayer.AddStatusText(this, $"-{dmg}", 0xFF0000, 1000, 0);
                }
            }
        }
        else if (!base.Update(time, dt)) {
            return false;
        }

        Effect?.Update(time, dt);
        return true;
    }

    public void AutoAbilityToggle(bool toggle) {
        AutoAbility = toggle;
    }

    public void TryReload(float mouseX, float mouseY) {
        if (Ammo >= MaxAmmo) {
            UseAltWeapon(mouseX, mouseY, 5);
        }
        else {
            var curTime = (int)Main.GameTime.TotalMs;
            var item = Equipment[0];
            if (item == null)
                return;

            if (curTime > ReloadCooldown + 2000) {
                // SoundEffectLibrary.play("Weapon_Reload");
                var pkt = UseItem.CreatePacket();
                pkt.Time = curTime;
                pkt.SlotObject = new ObjectSlot {
                    ObjectId = ObjectId,
                    SlotId = 0,
                    ObjectType = item.ObjectType
                };
                pkt.ItemUsePos = new Position { X = 0, Y = 0 };
                pkt.UseType = 1;
                Client.QueuePacket(pkt);
            }
        }
    }

    public bool UseAltWeapon(float mouseX, float mouseY, byte useType) {
        if (Map.LocalPlayer == null || HasConditionEffect(ConditionEffects.Paused))
            return false;

        var ability = Equipment[1];
        if (ability == null)
            return false;

        var range = 25f;
        var xDiff = Position.X - mouseX;
        var yDiff = Position.Y - mouseY;
        var dist = MathF.Sqrt(xDiff * xDiff + yDiff * yDiff);
        var angle = MathF.Atan2(-yDiff, -xDiff);

        var targetX = mouseX;
        var targetY = mouseY;

        if (dist > range) {
            targetX = Position.X + MathF.Cos(angle) * range;
            targetY = Position.Y + MathF.Sin(angle) * range;
        }

        var shoot = false;
        var teleport = false;
        var dash = false;
        var isMultiPhase = ability.MpEndCost > 0;

        if (ability.ActivateEffects != null)
            foreach (var effect in ability.ActivateEffects) {
                if (effect.Effect == "Teleport")
                    teleport = true;
                if (effect.Effect == "Shoot")
                    shoot = true;
                if (effect.Effect == "AutoShoot")
                    shoot = true;
                if (effect.Effect == "Dash")
                    dash = true;
                if (effect.Effect != "AutoShoot" && useType == 4)
                    return false;
            }

        if (teleport && !IsValidPosition(targetX, targetY))
            return false;

        var time = (int)Main.GameTime.TotalMs;

        if (useType == 1 || useType == 4 || (useType == 5 && isMultiPhase)) {
            if (time < NextAltAttack)
                return false;

            var mpCheck = isMultiPhase && ability.SlotType == 25 ? ability.MpEndCost : ability.MpCost;
            var xorCondition = !(isMultiPhase ^ (useType == 5));

            if ((mpCheck != 0 && mpCheck > Mp && xorCondition) || HasConditionEffect(ConditionEffects.Silenced))
                return false;

            var cooldown = (int)(ability.Cooldown * 1000f);
            if (cooldown <= 0)
                cooldown = 550;

            if (isMultiPhase && ability.SlotType == 25) {
                if (useType == 5) {
                    LastAltAttack = time;
                    NextAltAttack = time + cooldown;
                }
            }
            else {
                NextAltAttack = time + cooldown;
                LastAltAttack = time;
            }

            var serverBulletId = ReadServerId();

            if (useType == 5 && isMultiPhase) {
                var pktCancel = UseItem.CreatePacket();
                pktCancel.Time = time;
                pktCancel.SlotObject = new ObjectSlot
                    { ObjectId = ObjectId, SlotId = 1, ObjectType = ability.ObjectType };
                pktCancel.ItemUsePos = new Position { X = targetX, Y = targetY };
                pktCancel.UseType = 2;
                pktCancel.ProjId = serverBulletId;
                Client.QueuePacket(pktCancel);

                var pktShoot = UseItem.CreatePacket();
                pktShoot.Time = time;
                pktShoot.SlotObject = new ObjectSlot
                    { ObjectId = ObjectId, SlotId = 1, ObjectType = ability.ObjectType };
                pktShoot.ItemUsePos = new Position { X = targetX, Y = targetY };
                pktShoot.UseType = 4;
                pktShoot.ProjId = serverBulletId;
                Client.QueuePacket(pktShoot);
            }
            else {
                var pkt = UseItem.CreatePacket();
                pkt.Time = time;
                pkt.SlotObject = new ObjectSlot { ObjectId = ObjectId, SlotId = 1, ObjectType = ability.ObjectType };
                pkt.ItemUsePos = new Position { X = targetX, Y = targetY };
                pkt.UseType = useType;
                pkt.ProjId = serverBulletId;
                Client.QueuePacket(pkt);
            }

            if (dash) {
                DashTimer = 100;
                IFrame = true;
            }

            if (shoot && (!(isMultiPhase && useType != 5) || (isMultiPhase && useType == 5))) {
                if (useType == 5 && Mp < ability.MpEndCost)
                    return false;

                var props = ObjectLibrary.TypeToObjectProps[ability.ObjectType];
                var shootPacket = PlayerShoot.CreatePacket();

                var arc = MathHelper.DegreesToRadians(props.ArcGap) * (props.NumProjectiles - 1);
                var startAngle = angle - arc / 2;

                var startCount = ability.StartCount;
                var nextCount = ability.NextCount;
                var projTypeIndex = 0;

                for (var i = 0; i < props.NumProjectiles; i++) {
                    var projDesc = ability.AllProjectiles[projTypeIndex % ability.AllProjectiles.Length];
                    var projObjType = ObjectLibrary.IdToObjectType[projDesc.ObjectId];
                    var objProps = ObjectLibrary.TypeToObjectProps[projObjType];
                    var projAngle = startAngle + MathHelper.DegreesToRadians(props.ArcGap) * i;
                    var bId = GetBulletId2();

                    // DETERMINISTIC SEED
                    var seededRand = new Random(bId + AccountId);
                    var rawDmg = seededRand.Next(projDesc.MinDamage, projDesc.MaxDamage + 1);
                    var baseMult = 0.5f + Attack / 50f;
                    if (HasConditionEffect(ConditionEffects.Damaging))
                        baseMult *= 1.25f;

                    var dmg = (int)(rawDmg * baseMult);
                    if (!props.ProjectilesDict.TryGetValue(projDesc.BulletType, out var projPropsInfo))
                        projPropsInfo = props.Projectiles[0];

                    var proj = ObjectPools.Projectiles.Pop();
                    proj.Reset(bId, dmg, projAngle, this, objProps, projPropsInfo);
                    Map.AddProjectile(proj);

                    if (i == 0) {
                        if (!string.IsNullOrEmpty(props.OldSound))
                            SoundManager.PlaySoundEffect(props.OldSound, SoundType.Weapon);
                        else if (!string.IsNullOrEmpty(props.Sound))
                            SoundManager.PlaySoundEffect(props.Sound, SoundType.Weapon);

                        shootPacket.Shots.Add(new ShootData {
                            Time = time,
                            BulletId = bId,
                            ItemType = ability.ObjectType,
                            StartingPos = new Position { X = Position.X, Y = Position.Y },
                            Angle = projAngle
                        });
                    }

                    startCount++;
                    if (startCount % nextCount == 0)
                        projTypeIndex++;
                }

                Client.QueuePacket(shootPacket);
            }
        }
        else if (isMultiPhase) {
            var pkt = UseItem.CreatePacket();
            pkt.Time = time;
            pkt.SlotObject = new ObjectSlot { ObjectId = ObjectId, SlotId = 1, ObjectType = ability.ObjectType };
            pkt.ItemUsePos = new Position { X = targetX, Y = targetY };
            pkt.UseType = useType;
            Client.QueuePacket(pkt);
        }

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
                    AccountId = int.Parse(stat.Text);
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
                case StatsType.Cloth1:
                    Cloth1 = (ushort)stat.Value;
                    break;
                case StatsType.Cloth2:
                    Cloth2 = (ushort)stat.Value;
                    break;
                case StatsType.LootGlyphId1:
                    LootGlyphID1 = (ushort)stat.Value;
                    break;
                case StatsType.GlyphId1:
                    GlyphID1 = (ushort)stat.Value;
                    break;
                case StatsType.GlyphId2:
                    GlyphID2 = (ushort)stat.Value;
                    break;
                case StatsType.SpeedLimit:
                    SpeedLimit = (ushort)stat.Value;
                    break;
                case StatsType.AllDamageMultiplier:
                    AllDamageMultiplier = stat.Value;
                    break;
                case StatsType.DamageTakenMultiplier:
                    DamageTakenMultiplier = stat.Value;
                    break;
                case StatsType.PartyId:
                    PartyId = stat.Value;
                    PartyMembers.Clear();
                    if (PartyId != 0)
                        foreach (var (_, p) in Map.Players)
                            if (p != this && p.PartyId == PartyId)
                                PartyMembers.Add(p);

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
        }
        else if (MovementVector != Vector2.Zero) {
            var walkPer = 3.5f / GetMoveSpeed();
            FacingAngle = MathF.Atan2(MovementVector.Y, MovementVector.X);
            action = AnimationType.Walk;
            idx = time % walkPer / walkPer;
        }

        texture = TextureData.AnimatedTextures.TextureFromFacing(FacingAngle, action, (float)idx, out attackFrame,
            out flipped);

        return texture;
    }


    private void SetPlayerSkinTemplate(ushort skin) {
        if (skin == 0)
            return;

        if (!ObjectLibrary.TypeToTextureData.ContainsKey(skin)) {
            TextureData = ObjectLibrary.TypeToTextureData[0x030e];
            Texture = TextureData.HasAnimationData
                ? TextureData.AnimatedTextures.FaceRight[0]
                : TextureData.GetTexture();
            RenderBaseType.SetTexture(Texture, false);
            return;
        }

        TextureData = ObjectLibrary.TypeToTextureData[skin];
        Texture = TextureData.HasAnimationData ? TextureData.AnimatedTextures.FaceRight[0] : TextureData.GetTexture();
        RenderBaseType.SetTexture(Texture, false);
    }

    private float AttackFrequency() {
        if (HasConditionEffect(ConditionEffects.Dazed))
            return MinAttackFreq;

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
        AttackPeriod = (float)(1 / temp * (1 / item.RateOfFire) * 1.02);

        if (gameTime.TotalMs < AttackStart + AttackPeriod)
            return;

        AttackAngle = attackAngle;
        AttackStart = gameTime.TotalMs;

        var props = ObjectLibrary.TypeToObjectProps[item.ObjectType];

        var arc = MathHelper.DegreesToRadians(props.ArcGap) * (props.NumProjectiles - 1);
        var startAngle = attackAngle - arc / 2;

        var startCount = item.StartCount;
        var nextCount = item.NextCount;
        var projTypeIndex = 0;

        for (var i = 0; i < props.NumProjectiles; i++) {
            var projDesc = item.AllProjectiles[projTypeIndex % item.AllProjectiles.Length];
            var projObjType = ObjectLibrary.IdToObjectType[projDesc.ObjectId];
            var objProps = ObjectLibrary.TypeToObjectProps[projObjType];
            var angle = startAngle + MathHelper.DegreesToRadians(props.ArcGap) * i;
            var bId = GetBulletId();

            var seededRand = new Random(bId + AccountId);
            var rawDmg = seededRand.Next(projDesc.MinDamage, projDesc.MaxDamage + 1);
            var dmg = (int)(rawDmg * (0.5f + Attack / 50f));

            if (!props.ProjectilesDict.TryGetValue(projDesc.BulletType, out var projProps))
                projProps = props.Projectiles[0];

            var proj = ObjectPools.Projectiles.Pop();
            proj.Reset(bId, dmg, angle, this, objProps, projProps);
            Map.AddProjectile(proj);

            if (i == 0) {
                if (!string.IsNullOrEmpty(props.OldSound))
                    SoundManager.PlaySoundEffect(props.OldSound, SoundType.Weapon);
                else if (!string.IsNullOrEmpty(props.Sound))
                    SoundManager.PlaySoundEffect(props.Sound, SoundType.Weapon);

                PendingShots.Add(new ShootData {
                    Time = (int)Main.GameTime.TotalMs,
                    BulletId = bId,
                    ItemType = item.ObjectType,
                    StartingPos = new Position { X = Position.X, Y = Position.Y },
                    Angle = attackAngle
                });
            }

            startCount++;
            if (startCount % nextCount == 0)
                projTypeIndex++;
        }
    }

    public int GetBulletId() {
        var ret = NextBulletId;
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

    public int AccountId;
    public int AllDamageMultiplier;

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
    public List<Player> PartyMembers { get; } = new();

    public int OxygenBar;

    public int HealthStackCount;
    public int MagicStackCount;

    public ushort Skin;

    // Cosmetic's
    public ushort Cloth1;
    public ushort Cloth2;
    public ushort LootGlyphID1;
    public ushort GlyphID1;
    public ushort GlyphID2;

    // Dash
    public int DashTimer;
    public int SpeedLimit = 20;
    public bool LimitSpeed;
    public bool IFrame;
    public bool AutoAbility;
    public int NextAltAttack;
    public int LastAltAttack;
    public int Ammo;
    public int MaxAmmo;
    public int ReloadCooldown;

    public int PartyId;

    public int LdBoosted;
    public int LdBoostAmount;

    public int XpBoostTime;

    public bool HasBackPack;

    public bool IsFellowGuild;

    #endregion

}