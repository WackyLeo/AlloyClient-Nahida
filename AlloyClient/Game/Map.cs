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
    public static readonly Dictionary<int, Player> Players = new();

    public static readonly Dictionary<int, Entity>
        Entities = new(); // todo: add players to separate dic for minimap prio

    public static readonly Dictionary<int, Entity> InteractiveObjects = new();
    private static readonly Dictionary<float, List<MapTile>> VisibleElevatedTiles = new();
    public static readonly List<ParticleEffect> ParticleGenerators = [];

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
    private static readonly ParticleData[] Particles = new ParticleData[30000];
    private static readonly List<int> EntitiesToRemove = [];
    private static readonly List<MapTile> VisibleTopTiles = [];

    private static Vector2 _lastPosition = Vector2.Zero;

    private static readonly List<(Projectile proj, double spawnAt)> PendingProjectiles = [];

    private static readonly MapTile[] RebuildData = new MapTile[9];

    public static void InitMap(int width, int height, string name, string display, int diff, uint seed, int background,
        bool allowTp, bool showDisplays) {
        Width = width;
        Height = height;
        Name = name;
        DisplayName = display;
        Difficulty = diff;
        Seed = seed;
        Background = background;
        AllowPlayerTeleport = allowTp;
        ShowDisplays = showDisplays;

        _tiles = new MapTile[width + 1, height + 1];

        Minimap.OnNewMap.Dispatch(width, height);
    }

    public static void Update(double time, double dt) {
        CurrentTime = time;
        _particleCount = 0;
        var fullMatrix = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
        var matrix = new DepthMatrix(fullMatrix);

        var hasDarkness = LocalPlayer?.HasConditionEffect(ConditionEffects.Darkness) ?? false;
        var darknessRadiusSq = 49f;

        var updateRadius = hasDarkness ? 7 : TileRenderDistance;
        if (LocalPlayer != null && LocalPlayer.UpdateRadius != updateRadius) {
            LocalPlayer.UpdateRadius = updateRadius;
            TilesDirty = true;
        }

        foreach (var (objectId, entity) in Entities) {
            if (!entity.Update(time, dt)) {
                EntitiesToRemove.Add(objectId);
                continue;
            }

            if (LocalPlayer != null && !entity.Properties.Static && !entity.Properties.AlwaysLoaded &&
                entity.Properties.IsEnemy && !entity.Properties.IsQuest) {
                if (entity.RenderBaseType is TypeWall ||
                    entity.RenderBaseType is TypeCaveWall ||
                    entity.RenderBaseType is TypeWallTop)
                    continue;
                
                var dx = entity.Position.X - LocalPlayer.Position.X;
                var dy = entity.Position.Y - LocalPlayer.Position.Y;
                if (dx * dx + dy * dy > TileRenderDistance * TileRenderDistance) {
                    entity.RenderBaseType.Visible = false;
                    continue;
                }
            }

            entity.UpdateVisibility(ref fullMatrix);

            var allowTransparency = entity.RenderBaseType is TypeGameObject || entity.RenderBaseType is TypePlayer;
            if (allowTransparency) {
                if (hasDarkness && entity != LocalPlayer && !Players.ContainsKey(objectId))
                    if ((entity.Position - LocalPlayer.Position).LengthSquared > darknessRadiusSq)
                        entity.RenderBaseType.Visible = false;

                if (entity.HasConditionEffect(ConditionEffects.Invisible))
                    entity.RenderBaseType.SetAlpha(0.5f);
                else
                    entity.RenderBaseType.SetAlpha(1.0f);
            }
        }

        foreach (var id in EntitiesToRemove) RemoveEntity(id);

        EntitiesToRemove.Clear();

        for (var i = ParticleGenCount - 1; i >= 0; i--) {
            var gen = ParticleGenerators[i];
            if (gen.Update(time, dt)) continue;

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
        if (LocalPlayer == null) return;
        LastGameTime = gameTime;
        RenderPipeline.BeginFrame(gameTime);
        if (Render.DebugEnabled) {
            foreach (var (_, entity) in Entities) {
                if (entity.Properties == null) continue;
                if (entity.Properties.IsEnemy)
                    Render.DebugCircle(Camera.WorldToDebug2D(new Vector3(entity.Position.X, entity.Position.Y, 0.5f)),
                        0.25f, new Vector3(1f, 0.5f, 0f), 0.03f);
                if (entity.Properties.IsPlayer)
                    Render.DebugCircle(Camera.WorldToDebug2D(new Vector3(entity.Position.X, entity.Position.Y, 0.5f)),
                        0.25f, new Vector3(0f, 0.5f, 1f), 0.03f);
            }

            if (LocalPlayer != null) {
                var lpPos = Camera.WorldToDebug2D(new Vector3(LocalPlayer.Position.X, LocalPlayer.Position.Y,
                    LocalPlayer.Z));
                Render.DebugLine(lpPos - new Vector2(0.3f, 0), lpPos + new Vector2(0.3f, 0), new Vector3(1, 1, 0),
                    0.03f);
            }
        }

        var pos = Vector2.Floor(LocalPlayer.Position);
        if (pos != _lastPosition || TilesDirty) {
            _lastPosition = pos;
            TilesDirty = false;
            BuildTileVisibility();
        }

        RenderPipeline.BeginElevatedBase();
        foreach (var (zOffset, tiles) in VisibleElevatedTiles) {
            if (tiles.Count == 0) continue;
            Render.StartNewElevatedTile();
            foreach (var tile in tiles) Render.DrawNewElevatedTile(tile.GetTileData());
            Render.EndNewElevatedTile();
            Render.SetTileElevation(zOffset);
            Render.DrawElevatedTilesBase();
            Render.SetTileElevation(0f);
        }

        if (EntityStorage.TryGetValue(ModelType.PbGroundObject, out var groundObjects)) {
            Render.StartDrawGroundObjects();
            foreach (var obj in groundObjects)
                if (obj.Visible && obj.IsElevated)
                    obj.Draw();
            Render.FlushGroundObjects();
        }

        if (EntityStorage.TryGetValue(ModelType.PbObject, out var goElevated)) {
            Render.StartDrawShadow();
            foreach (var type in goElevated)
                if (type.IsElevated)
                    type.DrawShadow();
            Render.EndShadowDraw();
        }

        RenderPipeline.BeginElevatedOpaque();
        if (EntityStorage.TryGetValue(ModelType.PbCaveWall, out var elevatedCaveWalls)) {
            Render.StartDrawCaveWall();
            foreach (var wall in elevatedCaveWalls)
                if (wall.Visible && wall.IsElevated)
                    wall.Draw();
            Render.FlushCaveWall();
        }

        Render.StartDrawModel();
        foreach (var kvp in EntityStorage) {
            if (kvp.Key == ModelType.Null || kvp.Key == ModelType.PbObject || kvp.Key == ModelType.PbCaveWall) continue;
            Render.SetEntityModel(kvp.Key);
            foreach (var entity in kvp.Value)
                if (entity.Visible && entity.IsElevated) {
                    entity.Draw();
                    Render.LastDrawCountEntities++;
                }

            Render.FlushBufferModel();
        }

        RenderPipeline.BeginGroundBase();
        Render.SetTileElevation(0f);
        Render.DrawGroundTilesBase();
        Render.DrawGroundTileBlends();

        if (EntityStorage.TryGetValue(ModelType.PbGroundObject, out var normalGroundObjects)) {
            Render.StartDrawGroundObjects();
            foreach (var obj in normalGroundObjects)
                if (obj.Visible && obj.Position.Z == 0f)
                    obj.Draw();
            Render.FlushGroundObjects();
        }

        if (EntityStorage.TryGetValue(ModelType.PbObject, out var go)) {
            Render.StartDrawShadow();
            foreach (var type in go)
                if (type.Position.Z == 0f && type.Visible)
                    type.DrawShadow();
            Render.EndShadowDraw();
        }

        RenderPipeline.BeginParticles();
        Render.DrawParticles(Particles, _particleCount);

        RenderPipeline.BeginCaveWalls();
        if (EntityStorage.TryGetValue(ModelType.PbCaveWall, out var caveWalls)) {
            Render.StartDrawCaveWall();
            foreach (var wall in caveWalls)
                if (wall.Visible && !wall.IsElevated)
                    wall.Draw();
            Render.FlushCaveWall();
        }

        RenderPipeline.BeginModels();
        Render.StartDrawModel();
        foreach (var kvp in EntityStorage) {
            if (kvp.Key == ModelType.Null || kvp.Key == ModelType.PbObject || kvp.Key == ModelType.PbCaveWall) continue;
            Render.SetEntityModel(kvp.Key);
            foreach (var entity in kvp.Value)
                if (entity.Visible && !entity.IsElevated) {
                    entity.Draw();
                    Render.LastDrawCountEntities++;
                }

            Render.FlushBufferModel();
        }

        RenderPipeline.BeginEntities();
        Render.StartDrawEntity();
        for (var i = 0; i < ParticleGenCount; i++)
            if (ParticleGenerators[i] is RayCastParticle rcp)
                rcp.Draw();

        if (EntityStorage.TryGetValue(ModelType.PbObject, out var entities)) {
            entities.Sort();
            foreach (var type in entities)
                if (type.Visible) {
                    type.Draw();
                    Render.LastDrawCountEntities++;
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

    private static void BuildTileVisibility() {
        VisibleTopTiles.Clear();
        foreach (var list in VisibleElevatedTiles.Values) list.Clear();
        Render.StartNewDrawTile();

        var halfWidthTiles = Settings.ScreenWidth / (100f * Settings.CameraZoom);
        var halfHeightTiles = Settings.ScreenHeight / (100f * Settings.CameraZoom);
        
        var distToCorner = MathF.Sqrt(halfWidthTiles * halfWidthTiles + halfHeightTiles * halfHeightTiles);
        var renderDist = (int)MathF.Ceiling(distToCorner) + 2;

        for (var x = -renderDist; x < renderDist; x++) {
            for (var y = -renderDist; y < renderDist; y++) {
                var tile = GetTile(x + (int)LocalPlayer.Position.X, y + (int)LocalPlayer.Position.Y);
                if (tile != null && tile.Type != 0xFF) {
                    if (tile.ZOffset != 0f) {
                        if (!VisibleElevatedTiles.TryGetValue(tile.ZOffset, out var list)) {
                            list = new List<MapTile>();
                            VisibleElevatedTiles[tile.ZOffset] = list;
                        }

                        list.Add(tile);
                    } else {
                        tile.DrawTileBase();
                    }

                    if (tile.HasTop) VisibleTopTiles.Add(tile);
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
        foreach (var topTile in VisibleTopTiles) topTile.DrawTop();
        Render.SaveTopCount();
        Render.EndNewDrawTile();
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
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        var tile = _tiles[x, y];
        if (tile != null) return tile;
        tile = new MapTile(x, y);
        _tiles[x, y] = tile;
        return tile;
    }

    public static void SetTileData(int x, int y, ushort type) {
        var tile = GetTile(x, y);
        if (tile == null) return;
        TilesDirty = true;
        tile.SetType(type);
        for (var y1 = y - 1; y1 <= y + 1; y1++) {
            for (var x1 = x - 1; x1 <= x + 1; x1++) RebuildTile(GetTile(x1, y1));
        }

        Array.Clear(RebuildData);
    }

    private static void RebuildTile(MapTile tile) {
        if (tile == null) return;

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

    public static void AddEntity(Entity en, Position position) {
        if (!Entities.TryAdd(en.ObjectId, en))
            return;

        EntityStorage.Add(en);

        if (en is Player p) {
            if (p.ObjectId != LocalPlayerId)
                Players.TryAdd(p.ObjectId, p);
            p.Ignored = PartyData.IgnoredPlayers.Contains(p.AccountId);
            p.Locked = PartyData.LockedPlayers.Contains(p.AccountId);
        }


        if (InteractPanel.IsInteractiveObject(en))
            InteractiveObjects.TryAdd(en.ObjectId, en);

        en.OnAddedToMap(position);
    }

    public static void RemoveEntity(int id) {
        if (!Entities.Remove(id, out var en))
            return;

        Players.Remove(id);
        InteractiveObjects.Remove(id);

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

        if (count < 1) return;

        Array.Copy(particles, 0, Particles, _particleCount, count);
        _particleCount += count;
    }

    public static void AddParticle(ParticleData particle) {
        if (_particleCount + 1 > Particles.Length) return;

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

        Projectiles.Clear();
        PendingProjectiles.Clear();

        LocalPlayerId = 0;
        LocalPlayer = null;

        LastTickId = 0;

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
        GameSprite.Hud.CreatePlayerDependentAssets();
        OnPlayerUpdate.Dispatch(player);
    }
}

public class RenderStorage : Dictionary<ModelType, List<RenderBase>> {
    public void Add(Entity entity) {
        var type = entity.RenderBaseType;
        if (!ContainsKey(type.ModelType)) base[type.ModelType] = [];

        base[type.ModelType].Add(type);

        switch (type) {
            case TypeWall w:
                Add(w.Top);
                break;
        }
    }

    public void Add(Projectile proj) {
        var type = proj.RenderBaseType;
        if (!ContainsKey(type.ModelType)) base[type.ModelType] = [];

        base[type.ModelType].Add(type);
    }

    private void Add(RenderBase type) {
        if (type == null) return;

        if (!ContainsKey(type.ModelType)) base[type.ModelType] = [];

        base[type.ModelType].Add(type);
    }

    public void Remove(Entity entity) {
        var type = entity.RenderBaseType;
        if (!ContainsKey(type.ModelType)) return;

        base[type.ModelType].Remove(type);

        if (type is TypeWall w) Remove(w.Top);
    }

    public void Remove(Projectile proj) {
        var type = proj.RenderBaseType;
        if (type == null) return;
        if (!ContainsKey(type.ModelType)) return;
        base[type.ModelType].Remove(type);
    }

    private void Remove(RenderBase type) {
        if (!ContainsKey(type.ModelType)) return;

        base[type.ModelType].Remove(type);
    }
}