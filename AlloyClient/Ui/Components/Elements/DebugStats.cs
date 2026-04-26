using System;
using AlloyClient.Game;
using AlloyClient.Game.Components.Hud;
using AlloyClient.Rendering;
using AlloyClient.State;
using AlloyClient.UiLib;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Components.Elements;

public class DebugStats : Sprite {
    private const float Outline = 0.5f;
    
    private readonly SimpleText _devBuild = new(new TextConfig {
        Text = "IR DEVELOPMENT BUILD", FontSize = 28, FontType = FontType.Bold, OutlineThickness = 1.0f,
        Anchor = UiAnchor.MiddleTop
    });
    
    private readonly SimpleText _allocRate = new(new TextConfig {
        Text = "Alloc rate: 0 MB/s", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _avgFrameTime = new(new TextConfig {
        Text = "Avg: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _entities = new(new TextConfig {
        Text = "Entities: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _fps = new(new TextConfig {
        Text = "FPS: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _frameTimeTimer = new(new TextConfig {
        Text = "Frame time over the last 30 seconds", X = 2, FontSize = 16, FontType = FontType.Bold,
        OutlineThickness = Outline, Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _gcAlloc = new(new TextConfig {
        Text = "Allocated: 0 MB", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _gcAllocDelta = new(new TextConfig {
        Text = "Allocated delta: 0 B", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _gcGen0Count = new(new TextConfig {
        Text = "Gen0 count: 0", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _gcGen1Count = new(new TextConfig {
        Text = "Gen1 count: 0", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _gcGen2Count = new(new TextConfig {
        Text = "Gen2 count: 0", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _heapSize = new(new TextConfig {
        Text = "Heap: 0 MB", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _lohSize = new(new TextConfig {
        Text = "LOH: 0 MB", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _maxFrameTime = new(new TextConfig {
        Text = "Max: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _memory = new(new TextConfig {
        Text = "Memory", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _p90FrameTime = new(new TextConfig {
        Text = "P90: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _p99FrameTime = new(new TextConfig {
        Text = "P99: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _particles = new(new TextConfig {
        Text = "Particles: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _shadows = new(new TextConfig {
        Text = "Shadows: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _tiles = new(new TextConfig {
        Text = "Tiles: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private readonly SimpleText _ui = new(new TextConfig {
        Text = "Ui: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline,
        Anchor = UiAnchor.LeftTop
    });

    private long _lastGcBytes;

    private double _lastTime;

    public DebugStats() {
        AddChild(_fps);

        AddChild(_frameTimeTimer);
        AddChild(_avgFrameTime);
        AddChild(_p90FrameTime);
        AddChild(_p99FrameTime);
        AddChild(_maxFrameTime);
        AddChild(_memory);
        AddChild(_gcAlloc);
        AddChild(_gcAllocDelta);
        AddChild(_gcGen0Count);
        AddChild(_gcGen1Count);
        AddChild(_tiles);
        AddChild(_shadows);
        AddChild(_entities);
        AddChild(_particles);
        AddChild(_ui);
        AddChild(_gcGen2Count);
        AddChild(_heapSize);
        AddChild(_lohSize);
        AddChild(_allocRate);
        AddEventListener(Event.EnterFrame, OnFrameEnter);
        
        AddChild(_devBuild);
        _devBuild.Y = -36;
        _devBuild.X = Settings.DefaultScreenWidth / 2;
        
        _fps.Y = 4;
        _frameTimeTimer.Y = _fps.Y + _fps.Height + 4;
        _avgFrameTime.Y = _frameTimeTimer.Y + _frameTimeTimer.Height + 4;
        _p90FrameTime.Y = _avgFrameTime.Y + _avgFrameTime.Height + 4;
        _p99FrameTime.Y = _p90FrameTime.Y + _p90FrameTime.Height + 4;
        _maxFrameTime.Y = _p99FrameTime.Y + _p99FrameTime.Height + 4;
        _memory.Y = _maxFrameTime.Y + _maxFrameTime.Height + 4;
        _gcAlloc.Y = _memory.Y + _memory.Height + 4;
        _gcAllocDelta.Y = _gcAlloc.Y + _gcAlloc.Height + 4;
        _gcGen0Count.Y = _gcAllocDelta.Y + _gcAllocDelta.Height + 4;
        _gcGen1Count.Y = _gcGen0Count.Y + _gcGen0Count.Height + 4;
        _gcGen2Count.Y = _gcGen1Count.Y + _gcGen1Count.Height + 4;
        _heapSize.Y = _gcGen2Count.Y + _gcGen2Count.Height + 4;
        _lohSize.Y = _heapSize.Y + _heapSize.Height + 4;
        _allocRate.Y = _lohSize.Y + _lohSize.Height + 4;

        _tiles.Y = _allocRate.Y + _allocRate.Height + 4;
        _shadows.Y = _tiles.Y + _tiles.Height + 4;
        _entities.Y = _shadows.Y + _shadows.Height + 4;
        _particles.Y = _entities.Y + _entities.Height + 4;
        _ui.Y = _particles.Y + _particles.Height + 4;
    }

    private void OnFrameEnter() {
        _fps.SetText($"FPS: {Math.Round(GameScreen.FPS, 1)}");

        var gameTime = Stage.GameTime;
        var deltaMs = gameTime.TotalMs - _lastTime;
        if (deltaMs < 1000) return;

        _lastTime = gameTime.TotalMs;

        _avgFrameTime.SetText($"Avg: {Math.Round(GameScreen.AvgFrameTime, 3)} ms");
        _p90FrameTime.SetText($"P90: {Math.Round(GameScreen.P90FrameTime, 3)} ms");
        _p99FrameTime.SetText($"P99: {Math.Round(GameScreen.P99FrameTime, 3)} ms");
        _maxFrameTime.SetText($"Max: {Math.Round(GameScreen.MaxFrameTime, 3)} ms");
        _fps.SetText($"FPS: {Math.Round(GameScreen.FPS, 1)}");

        // Total live memory
        var totalMemory = GC.GetTotalMemory(false);
        _gcAlloc.SetText($"Total: {Math.Round(totalMemory / 1_000_000f, 2)} MB");

        // Allocation tracking
        var gcBytes = GC.GetTotalAllocatedBytes();
        var deltaBytes = gcBytes - _lastGcBytes;

        var seconds = deltaMs / 1000.0;
        var allocRateMb = seconds > 0 ? deltaBytes / 1_000_000.0 / seconds : 0;

        _gcAllocDelta.SetText($"Allocated delta: {Math.Round(deltaBytes / 1000.0, 2)} KB");
        _allocRate.SetText($"Alloc rate: {Math.Round(allocRateMb, 2)} MB/s");

        _lastGcBytes = gcBytes;

        // GC counts
        _gcGen0Count.SetText($"Gen0 count: {GC.CollectionCount(0)}");
        _gcGen1Count.SetText($"Gen1 count: {GC.CollectionCount(1)}");
        _gcGen2Count.SetText($"Gen2 count: {GC.CollectionCount(2)}");

        // Deep GC info
        var info = GC.GetGCMemoryInfo();

        // Total managed heap
        _heapSize.SetText($"Heap: {Math.Round(info.HeapSizeBytes / 1_000_000f, 2)} MB");

        // LOH (THIS is what you care about)
        var lohSize = info.GenerationInfo.Length > 3
            ? info.GenerationInfo[3].SizeAfterBytes
            : 0;

        _lohSize.SetText($"LOH: {Math.Round(lohSize / 1_000_000f, 2)} MB");

        // Render stats
        _tiles.SetText($"Tiles: {Render.LastDrawCountTiles}");
        _shadows.SetText($"Shadows: {Render.LastDrawCountShadows}");
        _entities.SetText($"Entities: {Render.LastDrawCountEntities}");
        _particles.SetText($"Particles: {Render.LastDrawParticleCount}");
        _ui.SetText($"Ui: {UiRender.LastRenderCount}");
        if (Stage != null) {
            _devBuild.X = (Stage.StageWidth / 2) - (HudView.Width / 2);
            _devBuild.Y = 6;
        }
    }
}