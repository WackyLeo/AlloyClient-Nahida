using AlloyClient.State;
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
        GL.DepthMask(true);
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

    public static void EndFrameAndPostProcess(GameTime gameTime, Vector2 gameCenter, Vector2 gameHalfSize) {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        Render.ShaderPostProcess.Apply();
        Render.ShaderPostProcess.SetValue("uTime", (float)(gameTime.TotalMs / 1000.0));
        Render.ShaderPostProcess.SetValue("uResolution", new Vector2(Settings.ScreenWidth, Settings.ScreenHeight));

        Render.ShaderPostProcess.SetValue("uGameCenter", gameCenter);
        Render.ShaderPostProcess.SetValue("uGameHalfSize", gameHalfSize);

        GL.BindTextureUnit(7, Render._FboTID);
        GL.BindVertexArray(Render._emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        GL.BindVertexArray(0);
        GL.BindTextureUnit(7, 0);
    }
}