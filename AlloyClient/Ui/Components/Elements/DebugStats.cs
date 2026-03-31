using System;
using AlloyClient.Game;
using AlloyClient.Rendering;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Components.Elements;

public class DebugStats : Sprite {
    private const float Outline = 0.5f;

    private readonly SimpleText _avgFrameTime = new(new TextConfig
        { Text = "Avg: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _entities = new(new TextConfig
        { Text = "Entities: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _fps = new(new TextConfig
        { Text = "FPS: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _gcAlloc = new(new TextConfig
        { Text = "Total: 0 MB", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _gcAllocDelta = new(new TextConfig
        { Text = "Allocated delta: 0 KB", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _maxFrameTime = new(new TextConfig
        { Text = "Max: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _p90FrameTime = new(new TextConfig
        { Text = "P90: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _p99FrameTime = new(new TextConfig
        { Text = "P99: 0 ms", X = 8, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    private readonly SimpleText _tiles = new(new TextConfig
        { Text = "Tiles: 0", X = 2, FontSize = 16, FontType = FontType.Bold, OutlineThickness = Outline });

    // Cached values to prevent unnecessary updates
    private double _lastAvg, _lastP90, _lastP99, _lastMax, _lastFps;
    private long _lastGcBytes;
    private long _lastMemMb, _lastAllocDeltaKb;
    private int _lastTiles, _lastEntities;

    private double _lastTime;

    public DebugStats() {
        return;
        AddChild(_fps);
        AddChild(_avgFrameTime);
        AddChild(_p90FrameTime);
        AddChild(_p99FrameTime);
        AddChild(_maxFrameTime);
        AddChild(_gcAlloc);
        AddChild(_gcAllocDelta);
        AddChild(_tiles);
        AddChild(_entities);

        AddEventListener(Event.EnterFrame, OnFrameEnter);

        _fps.Y = 4;
        _avgFrameTime.Y = _fps.Y + 20;
        _p90FrameTime.Y = _avgFrameTime.Y + 20;
        _p99FrameTime.Y = _p90FrameTime.Y + 20;
        _maxFrameTime.Y = _p99FrameTime.Y + 20;
        _gcAlloc.Y = _maxFrameTime.Y + 20;
        _gcAllocDelta.Y = _gcAlloc.Y + 20;
        _tiles.Y = _gcAllocDelta.Y + 20;
        _entities.Y = _tiles.Y + 20;
    }

    private void OnFrameEnter() {
        var gameTime = Stage.GameTime;
        if (gameTime.TotalMs - _lastTime < 1000) return;

        _lastTime = gameTime.TotalMs;

        var avg = GameScreen.AvgFrameTime;
        if (Math.Abs(avg - _lastAvg) > 0.001) {
            _lastAvg = avg;
            _avgFrameTime.SetText("Avg: " + avg.ToString("0.###") + " ms");
        }

        var p90 = GameScreen.P90FrameTime;
        if (Math.Abs(p90 - _lastP90) > 0.001) {
            _lastP90 = p90;
            _p90FrameTime.SetText("P90: " + p90.ToString("0.###") + " ms");
        }

        var p99 = GameScreen.P99FrameTime;
        if (Math.Abs(p99 - _lastP99) > 0.001) {
            _lastP99 = p99;
            _p99FrameTime.SetText("P99: " + p99.ToString("0.###") + " ms");
        }

        var max = GameScreen.MaxFrameTime;
        if (Math.Abs(max - _lastMax) > 0.001) {
            _lastMax = max;
            _maxFrameTime.SetText("Max: " + max.ToString("0.###") + " ms");
        }

        var fps = GameScreen.FPS;
        if (Math.Abs(fps - _lastFps) > 0.1) {
            _lastFps = fps;
            _fps.SetText("FPS: " + fps.ToString("0.0"));
        }

        var memMb = (long)(GC.GetTotalMemory(false) / 1_000_000f);
        if (memMb != _lastMemMb) {
            _lastMemMb = memMb;
            _gcAlloc.SetText("Total: " + memMb + " MB");
        }

        var gcBytes = GC.GetTotalAllocatedBytes();
        var deltaKb = (long)((gcBytes - _lastGcBytes) / 1000.0);
        _lastGcBytes = gcBytes;

        if (deltaKb != _lastAllocDeltaKb) {
            _lastAllocDeltaKb = deltaKb;
            _gcAllocDelta.SetText("Allocated delta: " + deltaKb + " KB");
        }

        var tiles = Render.LastDrawCountTiles;
        if (tiles != _lastTiles) {
            _lastTiles = tiles;
            _tiles.SetText("Tiles: " + tiles);
        }

        var entities = Render.LastDrawCountEntities;
        if (entities != _lastEntities) {
            _lastEntities = entities;
            _entities.SetText("Entities: " + entities);
        }
    }
}