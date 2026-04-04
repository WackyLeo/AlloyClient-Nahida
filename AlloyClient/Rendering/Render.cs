using System;
using AlloyClient.Assets;
using AlloyClient.Engine.Graphics;
using AlloyClient.Engine.Graphics.Buffers;
using AlloyClient.Game;
using AlloyClient.Rendering.VertexData;
using AlloyClient.UiLib;
using Common;
using Common.ContentReaders;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering;

public static partial class Render {
    private const int BufferSize = 1000;
    private const int TileBufferSize = 30000;
    private const int ShadowBufferSize = 4096;

    public static int _Fbo;
    public static int _FboTID;
    public static int _RboID;
    public static int _emptyVao;

    private static int _bgVao;
    private static int _bgVbo;

    private static readonly (string, string)[] TileDefines = [("TileBuffer", $"{TileBufferSize}")];
    private static readonly (string, string)[] ShadowDefines = [("ShadowBuffer", $"{ShadowBufferSize}")];
    private static readonly (string, string)[] ObjectDefines = [("ObjectBuffer", $"{BufferSize}")];

    private static TileData[] _elevatedTileData;
    private static StorageBuffer<TileData> _elevatedTileBuffer;
    private static int _elevatedTileCount;

    // Shaders
    private static Shader _shaderGround;
    private static Shader _shaderShadow;
    private static Shader _shaderModel;
    private static Shader _shaderObject;
    private static Shader _shaderParticle;
    private static Shader _shaderGroundObject;
    private static Shader _shaderBackground;
    public static Shader ShaderPostProcess;

    // Vertex Objects
    private static VertexArrayObject _defaultVao;
    private static VertexArrayObject _modelVao;

    // Buffers
    private static IndexBuffer _modelIndexBuffer;
    private static VertexBuffer<VertexBase> _modelVertexBuffer;

    private static TileData[] _tileData;
    private static StorageBuffer<TileData> _tileBuffer;

    private static ShadowData[] _shadowData;
    private static UniformBuffer _shadowBuffer;

    private static VertexModel[] _modelData;
    private static VertexBuffer<VertexModel> _modelDataBuffer;

    private static VertexObject[] _entityData;
    private static StorageBuffer<VertexObject> _entityDataBuffer;

    private static TileData[] _groundObjectData;
    private static StorageBuffer<TileData> _groundObjectBuffer;

    private static int _groundObjectCount;
    public static int BlendTileStart;

    public static unsafe void FirstTimeInit(Sampler atlas) {
        ShaderPostProcess = ContentReader.LoadShader("Shaders/PostProcess");

        _emptyVao = GL.GenVertexArray();
        ShaderPostProcess.Apply();
        var loc = GL.GetUniformLocation(ShaderPostProcess.Handle, "colourTexture");
        if (loc != -1) GL.Uniform1i(loc, 7);

        GL.GenFramebuffer(out _Fbo);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _Fbo);

        GL.GenTexture(out _FboTID);
        GL.BindTexture(TextureTarget.Texture2d, _FboTID);

        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, 800, 600,
            0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

        GL.BindTexture(TextureTarget.Texture2d, 0);
        GL.BindTextureUnit(atlas.TextureUnit, atlas.TextureHandle);

        GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _FboTID, 0);

        GL.GenRenderbuffer(out _RboID);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _RboID);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, 800, 600);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
            RenderbufferTarget.Renderbuffer, _RboID);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        _shaderGround = ContentReader.LoadShader("Shaders/Ground", TileDefines);
        _shaderGround.SetValue("GameTexture", atlas);

        _shaderShadow = ContentReader.LoadShader("Shaders/Shadow", ShadowDefines);

        _shaderModel = ContentReader.LoadShader("Shaders/Model");
        _shaderModel.SetValue("GameTexture", atlas);

        _shaderObject = ContentReader.LoadShader("Shaders/Object", ObjectDefines);
        _shaderObject.SetValue("GameTexture", atlas);

        _shaderParticle = ContentReader.LoadShader("Shaders/Particle");
        _shaderParticle.SetValue("GameTexture", atlas);

        _shaderGroundObject = ContentReader.LoadShader("Shaders/GroundObject", TileDefines);
        _shaderGroundObject.SetValue("GameTexture", atlas);

        _defaultVao = new VertexArrayObject();

        _tileData = new TileData[TileBufferSize];
        _tileBuffer = new StorageBuffer<TileData>(_tileData.Length);

        _groundObjectData = new TileData[TileBufferSize];
        _groundObjectBuffer = new StorageBuffer<TileData>(_groundObjectData.Length);

        _shadowData = new ShadowData[ShadowBufferSize];
        _shadowBuffer = new UniformBuffer(_shadowData.Length * sizeof(ShadowData));

        _modelIndexBuffer = new IndexBuffer(ModelData.Indices.Length);
        _modelIndexBuffer.SetData(ModelData.Indices);
        _modelVertexBuffer = new VertexBuffer<VertexBase>(VertexBase.VertexStride, ModelData.Vertices.Length);
        _modelVertexBuffer.SetData(ModelData.Vertices);

        _modelVao = new VertexArrayObject();

        _modelData = new VertexModel[BufferSize];
        _modelDataBuffer = new VertexBuffer<VertexModel>(VertexModel.VertexStride, BufferSize);
        _modelIndexBuffer.BindTo(_modelVao);
        _modelVertexBuffer.BindTo(_modelVao);
        _modelDataBuffer.BindTo(_modelVao, 1);

        _entityData = new VertexObject[BufferSize];
        _entityDataBuffer = new StorageBuffer<VertexObject>(BufferSize);

        _shaderObject.SetValue("PixelRange", UiRender.MyriadPro.PixelRange);
        _shaderObject.SetValue("TextTextureSize",
            new Vector2(UiRender.MyriadPro.Atlas.Width, UiRender.MyriadPro.Atlas.Height));
        _shaderObject.SetValue("TextTexture", UiRender.MyriadPro.Sampler);

        _elevatedTileData = new TileData[TileBufferSize];
        _elevatedTileBuffer = new StorageBuffer<TileData>(_elevatedTileData.Length);

        BuildParticleBuffers();

        _shaderBackground = ContentReader.LoadShader("Shaders/Clouds");

        float[] quadVertices = { -1f, 1f, -1f, -1f, 1f, 1f, 1f, -1f };
        _bgVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _bgVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices,
            BufferUsage.StaticDraw);

        _bgVao = GL.GenVertexArray();
        GL.BindVertexArray(_bgVao);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        InitCaveWall(atlas);
    }

    public static void ResizeFbo(int width, int height) {
        if (width == 0 || height == 0 || _Fbo == 0) return;

        GL.BindTexture(TextureTarget.Texture2d, _FboTID);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba,
            PixelType.Float, IntPtr.Zero);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _RboID);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, width, height);

        if (Main.gameAtlasSampler != null)
            GL.BindTextureUnit(Main.gameAtlasSampler.TextureUnit, Main.gameAtlasSampler.TextureHandle);
        else
            GL.BindTexture(TextureTarget.Texture2d, 0);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public static void SetShaderParams(GameTime gameTime) {
        var timeObj = (float)(gameTime.TotalMs / 1000.0f);

        var playerPos = Map.LocalPlayer?.Position ?? Vector2.Zero;
        var sightRadius = Map.LocalPlayer?.UpdateRadius ?? 20f;
        _shaderGround.SetValue("uPlayerPos", playerPos);
        _shaderGround.SetValue("uSightRadius", sightRadius);

        _shaderGround.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderGround.SetValue("ViewMatrix", Camera.ViewMatrix);

        _shaderGround.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderGround.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderGround.SetValue("ProjMatrix", Camera.ProjectionMatrix);
        _shaderGround.SetValue("GameTime", timeObj);

        _shaderShadow.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderShadow.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderShadow.SetValue("ProjMatrix", Camera.ProjectionMatrix);
        _shaderShadow.SetValue("BillMatrix", Camera.BillboardMatrix);

        _shaderModel.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderModel.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderModel.SetValue("ProjMatrix", Camera.ProjectionMatrix);

        _shaderObject.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderObject.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderObject.SetValue("ProjMatrix", Camera.ProjectionMatrix);
        _shaderObject.SetValue("BillMatrix", Camera.BillboardMatrix);

        _shaderParticle.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderParticle.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderParticle.SetValue("ProjMatrix", Camera.ProjectionMatrix);
        _shaderParticle.SetValue("BillMatrix", Camera.BillboardMatrix);

        _shaderGroundObject.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderGroundObject.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderGroundObject.SetValue("ProjMatrix", Camera.ProjectionMatrix);
        _shaderGroundObject.SetValue("GameTime", timeObj);

        SetCaveWallShaderParams();
    }

    public static void SetTileElevation(float elevation) {
        _shaderGround.SetValue("TileElevation", elevation);
    }

    public static void SaveBlendStart() {
        BlendTileStart = _tileCount;
    }

    public static void StartNewElevatedTile() {
        _elevatedTileCount = 0;
    }

    public static void DrawNewElevatedTile(TileData data) {
        _elevatedTileData[_elevatedTileCount++] = data;
    }

    public static void EndNewElevatedTile() {
        _elevatedTileBuffer.SetData(_elevatedTileData.AsSpan(0, _elevatedTileCount));
    }

    public static void DrawElevatedTilesBase() {
        if (_elevatedTileCount == 0) return;
        _defaultVao.Bind();
        _shaderGround.Apply();
        _elevatedTileBuffer.BindToIndex(0);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _elevatedTileCount * 6);
    }
}