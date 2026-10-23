using System;
using System.Collections.Generic;
using AlloyClient.Assets;
using AlloyClient.Game.Components;
using AlloyClient.Game.Components.Hud;
using AlloyClient.Game.Objects;
using AlloyClient.Game.Objects.Util;
using AlloyClient.Networking;
using AlloyClient.Networking.Packets.Outgoing;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.ParticleEffects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.Rendering.VertexData;
using AlloyClient.State;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Signals;
using Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace AlloyClient.Game;

public static class Map {
    public const int TileRenderDistance = 20;
    private const float EntityRenderPaddingTiles = 4f;

    public static int LastLoadedEntityCount;
    public static int LastStaticEntityCount;
    public static int LastVisibleEntityCount;
    public static int LastFarCulledEntityCount;
    public static float LastEntityRenderRadius;
    public static string MusicName = "Reclaiming Entropy";
    public static GameTime LastGameTime;
    public static GameSprite GameSprite;
    public static double CurrentTime;

    public static int Width;
    public static int Height;
    public static string Name;
    public static string DisplayName;
    public static int Difficulty;
    public static uint Seed;
    public static int Background;
    public static bool AllowPlayerTeleport;
    public static bool ShowDisplays;

    public static MapTile[,] _tiles;
    public static readonly RenderStorage EntityStorage = new();
    public static readonly RenderStorage StaticEntityStorage = new();

    private static readonly RenderStorage[] AllStorages = [EntityStorage, StaticEntityStorage];
    private static readonly List<RenderBase> _sortBuffer = [];

    public static readonly Dictionary<int, Player> Players = new();

    public static readonly Dictionary<int, Entity>
        Entities = new(); // todo: add players to separate dic for minimap prio

    public static readonly Dictionary<int, Entity> InteractiveObjects = new();
    private static readonly Dictionary<float, List<MapTile>> VisibleElevatedTiles = new();
    public static readonly List<ParticleEffect> ParticleGenerators = [];
    public static List<QuestData> DailyQuests = new();

    public static int ParticleGenCount;
    public static EnemyHit PendingEnemyHit = EnemyHit.CreatePacket();
    public static PlayerHit PendingPlayerHit = PlayerHit.CreatePacket();
    private static readonly List<Projectile> Projectiles = [];

    public static int LocalPlayerId;
    public static Player LocalPlayer;

    public static int LastTickId;

    public static Signal<Player> OnPlayerUpdate = new();

    public static bool TilesDirty = true;
    private static int _particleCount;
    private static readonly ParticleData[] Particles = new ParticleData[1024 * 256];
    private static readonly List<int> EntitiesToRemove = [];
    private static readonly List<MapTile> VisibleTopTiles = [];

    private static Vector2 _lastPosition = Vector2.Zero;
    private static float _lastCameraZoom;
    private static float _lastCameraAngle;
    private static int _lastScreenHeight;
    private static int _lastScreenWidth;
    public static RandomSeed Random;

    private static readonly List<(Projectile proj, double spawnAt)> PendingProjectiles = [];

    private static readonly MapTile[] RebuildData = new MapTile[9];

    public static void InitMap(int width, int height, string name, string display, int diff, uint seed, int background,
        bool allowTp, bool showDisplays, string musicName) {
        Width = width;
        Height = height;
        Name = name;
        DisplayName = display;
        Difficulty = diff;
        Seed = seed;
        Random = new RandomSeed(seed);
        Background = background;
        AllowPlayerTeleport = allowTp;
        ShowDisplays = showDisplays;
        MusicName = musicName;
        _tiles = new MapTile[width + 1, height + 1];

        Minimap.OnNewMap.Dispatch(width, height);

        // Pull the DQ Quest data
        if (name == "Quest Room") {
            var reqData = QuestFetchAsk.CreatePacket();
            Client.QueuePacket(reqData);
        }
    }

    public static void Update(double time, double dt) {
        CurrentTime = time;
        _particleCount = 0;
        var fullMatrix = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
        var matrix = new DepthMatrix(fullMatrix);

        var hasDarkness = LocalPlayer?.HasConditionEffect(ConditionEffects.Darkness) ?? false;
        var darknessRadiusSq = 49f;
        var entityRenderRadius = GetEntityRenderRadius();
        var entityRenderRadiusSq = entityRenderRadius * entityRenderRadius;

        LastEntityRenderRadius = entityRenderRadius;
        LastLoadedEntityCount = Entities.Count;
        LastStaticEntityCount = 0;
        LastVisibleEntityCount = 0;
        LastFarCulledEntityCount = 0;

        var updateRadius = hasDarkness ? 7 : TileRenderDistance;
        if (LocalPlayer != null && LocalPlayer.UpdateRadius != updateRadius) {
            LocalPlayer.UpdateRadius = updateRadius;
            TilesDirty = true;
        }

        foreach (var (objectId, entity) in Entities) {
            if (entity.Properties.Static)
                LastStaticEntityCount++;

            if (ShouldSkipFarEntityUpdate(entity, entityRenderRadiusSq)) {
                entity.RenderBaseType?.SetVisibility(false);
                LastFarCulledEntityCount++;
                continue;
            }

            if (!entity.Update(time, dt)) {
                if (!entity.Properties.Static && !entity.Properties.IsEnemy)
                    EntitiesToRemove.Add(objectId);
                continue;
            }

            if (entity.IsPredictedDead && entity.Properties != null && entity.Properties.Class != null &&
                !entity.Properties.Static && entity.Properties.IsEnemy &&
                (entity.Properties.Class == "Wall" || entity.Properties.Class == "DoubleWall" ||
                 entity.Properties.Class == "CaveWall" || entity.Properties.Class == "ConnectedWall")) {
                EntitiesToRemove.Add(objectId);
                continue;
            }

            entity.UpdateVisibility(ref fullMatrix, entityRenderRadiusSq);

            var allowTransparency = entity.RenderBaseType is TypeGameObject || entity.RenderBaseType is TypePlayer;
            if (allowTransparency) {
                if (hasDarkness && entity != LocalPlayer && !Players.ContainsKey(objectId))
                    if ((entity.Position - LocalPlayer.Position).LengthSquared > darknessRadiusSq)
                        entity.RenderBaseType.Visible = false;

                if (entity.IsPredictedDead && entity.Properties != null && !entity.Properties.IsEnemy)
                    entity.RenderBaseType.SetAlpha(0.4f);
                else if (entity.HasConditionEffect(ConditionEffects.Invisible))
                    entity.RenderBaseType.SetAlpha(0.5f);
                else if (entity is Player && objectId != LocalPlayerId)
                    entity.RenderBaseType.SetAlpha(Settings.AllyOpacity);
                else
                    entity.RenderBaseType.SetAlpha(1.0f);
            }

            if (entity.RenderBaseType?.Visible == true)
                LastVisibleEntityCount++;
        }

        foreach (var id in EntitiesToRemove)
            RemoveEntity(id);

        EntitiesToRemove.Clear();

        for (var i = ParticleGenCount - 1; i >= 0; i--) {
            var gen = ParticleGenerators[i];
            if (gen.Update(time, dt))
                continue;

            ParticleGenCount--;
            ParticleGenerators[i] = ParticleGenerators[ParticleGenCount];
            ParticleGenerators[ParticleGenCount] = null;
        }

        for (var i = PendingProjectiles.Count - 1; i >= 0; i--) {
            var (proj, spawnAt) = PendingProjectiles[i];
            if (time >= spawnAt) {
                proj.MarkActive(time);
                Projectiles.Add(proj);
                EntityStorage.Add(proj);
                PendingProjectiles.RemoveAt(i);
            }
        }

        for (var i = Projectiles.Count - 1; i >= 0; i--) {
            var proj = Projectiles[i];
            if (proj.Update(time, dt, matrix))
                continue;
            EntityStorage.Remove(proj);
            ObjectPools.Projectiles.Push(proj);

            var idx = Projectiles.Count - 1;
            Projectiles[i] = Projectiles[idx];
            Projectiles.RemoveAt(idx);
        }
    }

    public static void Draw(GameTime gameTime) {
        if (LocalPlayer == null)
            return;
        LastGameTime = gameTime;
        RenderPipeline.BeginFrame(gameTime);

        if (Render.DebugEnabled) {
            foreach (var (_, entity) in Entities) {
                if (entity.Properties == null)
                    continue;
                if (entity.Properties.IsEnemy)
                    Render.DebugCircle(Camera.WorldToDebug2D(new Vector3(entity.Position.X, entity.Position.Y, 0.5f)),
                        0.25f, new Vector3(1f, 0.5f, 0f), 0.03f);
                if (entity.Properties.IsPlayer)
                    Render.DebugCircle(Camera.WorldToDebug2D(new Vector3(entity.Position.X, entity.Position.Y, 0.5f)),
                        0.25f, new Vector3(0f, 0.5f, 1f), 0.03f);
            }

            var lpPos = Camera.WorldToDebug2D(
                new Vector3(LocalPlayer.Position.X, LocalPlayer.Position.Y, LocalPlayer.Z));
            Render.DebugLine(lpPos - new Vector2(0.3f, 0), lpPos + new Vector2(0.3f, 0), new Vector3(1, 1, 0), 0.03f);
        }

        var pos = Vector2.Floor(LocalPlayer.Position);
        var dirtyRotation = Math.Abs(_lastCameraAngle - Settings.CameraAngle) > 0.001f;
        var dirtyZoom = Math.Abs(_lastCameraZoom - Settings.CameraZoom) > 0.001f;
        var dirtySize = _lastScreenHeight != Settings.ScreenHeight || _lastScreenWidth != Settings.ScreenWidth;

        if (pos != _lastPosition || TilesDirty || dirtyRotation || dirtyZoom || dirtySize) {
            _lastCameraAngle = Settings.CameraAngle;
            _lastCameraZoom = Settings.CameraZoom;
            _lastScreenHeight = Settings.ScreenHeight;
            _lastScreenWidth = Settings.ScreenWidth;
            _lastPosition = pos;
            TilesDirty = false;
            BuildTileVisibility();
        }

        RenderPipeline.BeginElevatedBase();
        foreach (var (zOffset, tiles) in VisibleElevatedTiles) {
            if (tiles.Count == 0)
                continue;
            Render.StartNewElevatedTile();
            foreach (var tile in tiles)
                Render.DrawNewElevatedTile(tile.GetTileData());
            Render.EndNewElevatedTile();
            Render.SetTileElevation(zOffset);
            Render.DrawElevatedTilesBase();
            Render.SetTileElevation(0f);
        }

        DrawGroundObjects(true);
        DrawShadows(true);

        RenderPipeline.BeginElevatedOpaque();
        DrawCaveWalls(true);
        DrawModels(true);

        RenderPipeline.BeginGroundBase();
        Render.SetTileElevation(0f);
        Render.DrawGroundTilesBase();
        Render.DrawGroundTileBlends();

        DrawGroundObjects(false);
        DrawShadows(false);

        RenderPipeline.BeginParticles();
        Render.DrawParticles(Particles, _particleCount);

        RenderPipeline.BeginCaveWalls();
        DrawCaveWalls(false);

        RenderPipeline.BeginModels();
        DrawModels(false);

        RenderPipeline.BeginEntities();
        Render.StartDrawEntity();
        for (var i = 0; i < ParticleGenCount; i++)
            if (ParticleGenerators[i] is IDrawableParticleEffect drawableParticle)
                drawableParticle.Draw();

        _sortBuffer.Clear();
        foreach (var storage in AllStorages)
            if (storage.TryGetValue(ModelType.PbObject, out var entities))
                _sortBuffer.AddRange(entities);

        if (_sortBuffer.Count > 0) {
            _sortBuffer.Sort();
            foreach (var type in _sortBuffer) {
                if (type.Entity != null && type.Entity.Size == 0)
                    continue;
                if (type.Visible) {
                    type.Draw();
                    Render.LastDrawCountEntities++;
                }
            }

            Render.FlushBufferEntity();
        }

        if (VisibleTopTiles.Count > 0) {
            RenderPipeline.BeginTopTiles();
            Render.SetTileElevation(1f);
            Render.DrawTopTiles();
        }

        var vp = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
        Render.FlushDebug(vp);

        var hudPixelWidth = HudView.Width * Stage.ScreenScale.X;
        var gamePixelWidth = Settings.ScreenWidth - hudPixelWidth;
        var gameWidthUV = gamePixelWidth / Settings.ScreenWidth;
        var gameCenter = new Vector2(gameWidthUV / 2f, 0.5f);
        var gameHalfSize = new Vector2(gameWidthUV / 2f, 0.5f);

        RenderPipeline.EndFrameAndPostProcess(gameTime, gameCenter, gameHalfSize);
        GL.BindTextureUnit(Main.gameAtlasSampler.TextureUnit, Main.gameAtlasSampler.TextureHandle);
    }

    private static void DrawGroundObjects(bool elevated) {
        Render.StartDrawGroundObjects();
        foreach (var storage in AllStorages) {
            if (!storage.TryGetValue(ModelType.PbGroundObject, out var objs))
                continue;
            foreach (var obj in objs)
                if (obj.Visible && (elevated ? obj.IsElevated : obj.Position.Z == 0f))
                    obj.Draw();
        }

        Render.FlushGroundObjects();
    }

    private static void DrawShadows(bool elevated) {
        Render.StartDrawShadow();
        foreach (var storage in AllStorages) {
            if (!storage.TryGetValue(ModelType.PbObject, out var objs))
                continue;
            foreach (var type in objs)
                if (type.Visible && (elevated ? type.IsElevated : type.Position.Z == 0f))
                    type.DrawShadow();
        }

        Render.EndShadowDraw();
    }

    private static void DrawCaveWalls(bool elevated) {
        Render.StartDrawCaveWall();
        foreach (var storage in AllStorages) {
            if (!storage.TryGetValue(ModelType.PbCaveWall, out var walls))
                continue;
            foreach (var wall in walls)
                if (wall.Visible && wall.IsElevated == elevated)
                    wall.Draw();
        }

        Render.FlushCaveWall();
    }

    private static void DrawModels(bool elevated) {
        Render.StartDrawModel();
        foreach (var storage in AllStorages) {
            foreach (var kvp in storage) {
                if (kvp.Key == ModelType.Null || kvp.Key == ModelType.PbObject || kvp.Key == ModelType.PbCaveWall)
                    continue;
                Render.SetEntityModel(kvp.Key);
                foreach (var entity in kvp.Value)
                    if (entity.Visible && entity.IsElevated == elevated) {
                        entity.Draw();
                        Render.LastDrawCountEntities++;
                    }

                Render.FlushBufferModel();
            }
        }
    }

    private static void BuildTileVisibility() {
        VisibleTopTiles.Clear();
        foreach (var list in VisibleElevatedTiles.Values)
            list.Clear();
        Render.StartNewDrawTile();

        var halfWidthTiles = Settings.ScreenWidth / (100f * Settings.CameraZoom);
        var halfHeightTiles = Settings.ScreenHeight / (100f * Settings.CameraZoom);

        var distToCorner = MathF.Sqrt(halfWidthTiles * halfWidthTiles + halfHeightTiles * halfHeightTiles);
        var renderDist = (int)MathF.Ceiling(distToCorner) + 2;

        for (var x = -renderDist; x < renderDist; x++) {
            for (var y = -renderDist; y < renderDist; y++) {
                if (LocalPlayer == null) {
                    return;
                }

                var tile = GetTile(x + (int)LocalPlayer.Position.X, y + (int)LocalPlayer.Position.Y);
                if (tile != null && tile.Type != 0xFF) {
                    if (tile.ZOffset != 0f) {
                        if (!VisibleElevatedTiles.TryGetValue(tile.ZOffset, out var list)) {
                            list = new List<MapTile>();
                            VisibleElevatedTiles[tile.ZOffset] = list;
                        }

                        list.Add(tile);
                    }
                    else {
                        tile.DrawTileBase();
                    }

                    if (tile.HasTop)
                        VisibleTopTiles.Add(tile);
                }
            }
        }

        Render.SaveBlendStart();

        for (var x = -renderDist; x < renderDist; x++) {
            for (var y = -renderDist; y < renderDist; y++) {
                var tile = GetTile(x + (int)LocalPlayer.Position.X, y + (int)LocalPlayer.Position.Y);
                if (tile != null && tile.Type != 0xFF && tile.ZOffset == 0f)
                    tile.DrawTileBlends();
            }
        }

        Render.SaveGroundCount();
        foreach (var topTile in VisibleTopTiles)
            topTile.DrawTop();
        Render.SaveTopCount();
        Render.EndNewDrawTile();
    }

    private static float GetEntityRenderRadius() {
        if (LocalPlayer == null)
            return TileRenderDistance;

        var zoom = Math.Max((float)Settings.CameraZoom, 0.001f);
        var halfWidthTiles = Settings.ScreenWidth / (100f * zoom);
        var halfHeightTiles = Settings.ScreenHeight / (100f * zoom);

        return MathF.Ceiling(MathF.Sqrt(halfWidthTiles * halfWidthTiles + halfHeightTiles * halfHeightTiles))
               + EntityRenderPaddingTiles;
    }

    private static bool ShouldSkipFarEntityUpdate(Entity entity, float renderRadiusSq) {
        if (LocalPlayer == null || entity == LocalPlayer || entity.Properties == null)
            return false;

        if (!entity.Properties.Static && (entity.Properties.AlwaysLoaded || entity.Properties.IsQuest || entity is Player))
            return false;

        var dx = entity.Position.X - LocalPlayer.Position.X;
        var dy = entity.Position.Y - LocalPlayer.Position.Y;
        var far = dx * dx + dy * dy > renderRadiusSq;

        if (!far)
            return false;

        return entity.Properties.Static || entity.Properties.IsEnemy;
    }

    public static bool TryGetEntity(int id, out Entity entity) {
        return Entities.TryGetValue(id, out entity);
    }

    public static void FlushEnemyHits() {
        if (PendingEnemyHit.Stack > 0) {
            Client.QueuePacket(PendingEnemyHit);
            PendingEnemyHit = EnemyHit.CreatePacket();
        }
    }

    public static void FlushPlayerHits() {
        if (PendingPlayerHit.Stack > 0) {
            Client.QueuePacket(PendingPlayerHit);
            PendingPlayerHit = PlayerHit.CreatePacket();
        }
    }

    public static MapTile GetTile(Vector2 position) {
        return GetTile((int)position.X, (int)position.Y);
    }

    public static MapTile GetTile(int x, int y) {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        var tile = _tiles[x, y];
        if (tile != null)
            return tile;
        tile = new MapTile(x, y);
        _tiles[x, y] = tile;
        return tile;
    }

    public static void SetTileData(int x, int y, ushort type) {
        var tile = GetTile(x, y);
        if (tile == null)
            return;
        TilesDirty = true;
        tile.SetType(type);
        for (var y1 = y - 1; y1 <= y + 1; y1++) {
            for (var x1 = x - 1; x1 <= x + 1; x1++)
                RebuildTile(GetTile(x1, y1));
        }

        Array.Clear(RebuildData);
    }

    private static void RebuildTile(MapTile tile) {
        if (tile == null)
            return;

        var idx = 0;
        for (var y1 = tile.Y - 1; y1 <= tile.Y + 1; y1++) {
            for (var x1 = tile.X - 1; x1 <= tile.X + 1; x1++) {
                var t = GetTile(x1, y1);
                RebuildData[idx++] = t ?? tile;
            }
        }

        tile.Rebuild(RebuildData);
    }

    public static void AddParticleEffect(ParticleEffect effect) {
        if (ParticleGenCount == ParticleGenerators.Count)
            ParticleGenerators.Add(effect);
        else
            ParticleGenerators[ParticleGenCount] = effect;

        ParticleGenCount++;
    }

    public static bool AddEntity(Entity en, Position position) {
        if (!Entities.TryAdd(en.ObjectId, en))
            return false;

        if (en.Properties.Static && !en.Properties.IsEnemy) {
            StaticEntityStorage.Add(en);
        }
        else {
            EntityStorage.Add(en);
        }

        if (en is Player p) {
            if (p.ObjectId != LocalPlayerId)
                Players.TryAdd(p.ObjectId, p);
            p.Ignored = PartyData.IgnoredPlayers.Contains(p.AccountId);
            p.Locked = PartyData.LockedPlayers.Contains(p.AccountId);
        }


        if (InteractPanel.IsInteractiveObject(en))
            InteractiveObjects.TryAdd(en.ObjectId, en);

        en.OnAddedToMap(position);
        return true;
    }

    public static void RemoveEntity(int id) {
        if (!Entities.TryGetValue(id, out var en))
            return;

        Entities.Remove(id);
        Players.Remove(id);
        InteractiveObjects.Remove(id);

        if (en.Properties.Static && !en.Properties.IsEnemy)
            StaticEntityStorage.Remove(en);
        else
            EntityStorage.Remove(en);

        en.OnRemovedFromMap();
    }

    public static void AddProjectile(Projectile proj) {
        //proj.MarkActive(CurrentTime);
        Projectiles.Add(proj);
        EntityStorage.Add(proj);
    }

    public static void AddParticles(ParticleData[] particles, int count) {
        if (_particleCount + count > Particles.Length)
            count = Particles.Length - _particleCount;

        if (count < 1)
            return;

        Array.Copy(particles, 0, Particles, _particleCount, count);
        _particleCount += count;
    }

    public static void AddParticle(ParticleData particle) {
        if (_particleCount + 1 > Particles.Length)
            return;

        Particles[_particleCount] = particle;
        _particleCount++;
    }

    public static void Reset() {
        Camera.Reset();

        Height = 0;
        Name = null;
        DisplayName = null;
        Difficulty = 0;
        Seed = 0;
        Background = 0;
        AllowPlayerTeleport = false;
        ShowDisplays = false;

        PartyData.Clear();

        Entities.Clear();
        Players.Clear();
        InteractiveObjects.Clear();
        EntityStorage.Clear();
        StaticEntityStorage.Clear();

        Projectiles.Clear();
        PendingProjectiles.Clear();
        DailyQuests.Clear();

        ParticleGenerators.Clear();
        ParticleGenCount = 0;
        _particleCount = 0;
        EntitiesToRemove.Clear();
        VisibleTopTiles.Clear();
        foreach (var list in VisibleElevatedTiles.Values)
            list.Clear();
        VisibleElevatedTiles.Clear();
        _sortBuffer.Clear();

        LastLoadedEntityCount = 0;
        LastStaticEntityCount = 0;
        LastVisibleEntityCount = 0;
        LastFarCulledEntityCount = 0;
        LastEntityRenderRadius = 0f;

        LocalPlayer?.ClearPendingMp();
        LocalPlayerId = 0;
        LocalPlayer = null;

        LastTickId = 0;

        TilesDirty = true;
        _lastPosition = new Vector2(float.NaN, float.NaN);
        _lastCameraZoom = float.NaN;
        _lastCameraAngle = float.NaN;
        _lastScreenHeight = 0;
        _lastScreenWidth = 0;

        _tiles = null;
    }

    public static void OnLocalPlayerCreated(Entity entity) {
        if (LocalPlayer != null) {
            Logger.Error("Local player already exists");
            return;
        }

        if (entity is not Player player) {
            Logger.Error("Local player is not a player");
            return;
        }

        LocalPlayer = player;
        MinimapLayer.SetFocus(player);

        var preloadSounds = new List<string>();
        if (player.Equipment != null)
            foreach (var slot in player.Equipment) {
                if (slot == null)
                    continue;
                if (!string.IsNullOrEmpty(slot.OldSound))
                    preloadSounds.Add(slot.OldSound);
                else if (!string.IsNullOrEmpty(slot.Sound))
                    preloadSounds.Add(slot.Sound);
            }

        GameSprite.Hud.CreatePlayerDependentAssets();
        OnPlayerUpdate.Dispatch(player);
    }
}

public class RenderStorage : Dictionary<ModelType, List<RenderBase>> {
    public void Add(Entity entity) {
        var type = entity.RenderBaseType;
        if (!ContainsKey(type.ModelType))
            base[type.ModelType] = [];

        base[type.ModelType].Add(type);

        switch (type) {
            case TypeWall w:
                Add(w.Top);
                break;
        }
    }

    public void Add(Projectile proj) {
        var type = proj.RenderBaseType;
        if (!ContainsKey(type.ModelType))
            base[type.ModelType] = [];

        base[type.ModelType].Add(type);
    }

    private void Add(RenderBase type) {
        if (type == null)
            return;

        if (!ContainsKey(type.ModelType))
            base[type.ModelType] = [];

        base[type.ModelType].Add(type);
    }

    public void Remove(Entity entity) {
        var type = entity.RenderBaseType;
        if (!ContainsKey(type.ModelType))
            return;

        base[type.ModelType].Remove(type);

        if (type is TypeWall w)
            Remove(w.Top);
    }

    public void Remove(Projectile proj) {
        var type = proj.RenderBaseType;
        if (type == null)
            return;
        if (!ContainsKey(type.ModelType))
            return;
        base[type.ModelType].Remove(type);
    }

    private void Remove(RenderBase type) {
        if (!ContainsKey(type.ModelType))
            return;

        base[type.ModelType].Remove(type);
    }
}
