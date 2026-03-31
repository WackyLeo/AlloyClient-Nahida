using AlloyClient.Game.Components.Hud;
using AlloyClient.State;
using AlloyClient.UiLib.Core;
using Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering;

public static class RenderPipeline {
    public static void BeginFrame(GameTime gameTime) {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Render._Fbo);
        GL.ClearColor(0f, 0f, 0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        Render.SetShaderParams(gameTime);
    }

    public static void BeginElevatedBase() {
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
    }

    public static void BeginElevatedOpaque() {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.DepthMask(true);
    }

    public static void BeginGroundBase() {
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
    }

    public static void BeginParticles() {
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.DepthMask(false);
    }

    public static void BeginCaveWalls() {
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.DepthMask(true);
    }

    public static void BeginModels() {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.DepthMask(true);
    }

    public static void BeginEntities() {
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
    }

    public static void BeginTopTiles() {
        GL.Enable(EnableCap.DepthTest);
    }

    public static void EndFrameAndPostProcess(GameTime gameTime) {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        Render._shaderPostProcess.Apply();
        Render._shaderPostProcess.SetValue("uTime", (float)(gameTime.TotalMs / 1000.0));
        Render._shaderPostProcess.SetValue("uResolution", new Vector2(Settings.ScreenWidth, Settings.ScreenHeight));

        var hudPixelWidth = HudView.Width * Stage.ScreenScale.X;
        var gamePixelWidth = Settings.ScreenWidth - hudPixelWidth;

        var gameWidthUV = gamePixelWidth / Settings.ScreenWidth;
        var gameCenterX = gameWidthUV / 2f;

        var gameCenter = new Vector2(gameCenterX, 0.5f);
        var gameHalfSize = new Vector2(gameWidthUV / 2f, 0.5f);

        Render._shaderPostProcess.SetValue("uGameCenter", gameCenter);
        Render._shaderPostProcess.SetValue("uGameHalfSize", gameHalfSize);

        GL.BindTextureUnit(7, Render._FboTID);
        GL.BindVertexArray(Render._emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        GL.BindVertexArray(0);
        GL.BindTextureUnit(7, 0);
    }
}