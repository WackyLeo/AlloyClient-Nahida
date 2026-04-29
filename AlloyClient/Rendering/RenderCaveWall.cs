using System;
using AlloyClient.Engine.Graphics;
using AlloyClient.Engine.Graphics.Buffers;
using AlloyClient.Game;
using AlloyClient.Rendering.Types;
using AlloyClient.Rendering.VertexData;
using Common.ContentReaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering;

public static partial class Render {
    // 512 tiles * 150 verts = 76 800 soo round up to nearest power of two
    private const int CaveWallBufferVerts = 131072;

    private static Shader _shaderCaveWall;
    private static VertexArrayObject _caveWallVao;
    private static VertexBuffer<CaveWallVertex> _caveWallVertexBuffer;

    private static CaveWallVertex[] _caveWallData;
    private static int _caveWallCount;

    public static void InitCaveWall(Sampler atlas) {
        _shaderCaveWall = ContentReader.LoadShader("Shaders/CaveWall");
        _shaderCaveWall.SetValue("GameTexture", atlas);

        _caveWallVao = new VertexArrayObject();
        _caveWallVertexBuffer = new VertexBuffer<CaveWallVertex>(
            CaveWallVertex.VertexStride, CaveWallBufferVerts);
        _caveWallVertexBuffer.BindTo(_caveWallVao);

        _caveWallData = new CaveWallVertex[CaveWallBufferVerts];
    }

    public static void StartDrawCaveWall() {
        _caveWallCount = 0;
        _shaderCaveWall.Apply();
        _caveWallVao.Bind();
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.DepthMask(true);
    }

    public static void DrawCaveWall(TypeCaveWall wall) {
        var tris = wall.Triangles;
        var needed = tris.Count;

        if (needed == 0) return;

        if (_caveWallCount + needed > _caveWallData.Length) FlushCaveWall();

        for (var i = 0; i < needed; i++) _caveWallData[_caveWallCount + i] = tris[i];
        _caveWallCount += needed;
    }

    public static void DrawConnectedWall(TypeConnectedWall wall) {
        var tris = wall.Triangles;
        var needed = tris.Count;

        if (needed == 0) return;

        if (_caveWallCount + needed > _caveWallData.Length) FlushCaveWall();

        for (var i = 0; i < needed; i++) _caveWallData[_caveWallCount + i] = tris[i];
        _caveWallCount += needed;
    }


    public static void FlushCaveWall() {
        if (_caveWallCount == 0) return;
        _caveWallVertexBuffer.SetData(_caveWallData.AsSpan(0, _caveWallCount));
        GL.DrawArrays(PrimitiveType.Triangles, 0, _caveWallCount);
        _caveWallCount = 0;
    }

    public static void SetCaveWallShaderParams(Vector2 playerPos, float sightRadius) {
        _shaderCaveWall.SetValue("WorldMatrix", Camera.WorldMatrix);
        _shaderCaveWall.SetValue("ViewMatrix", Camera.ViewMatrix);
        _shaderCaveWall.SetValue("ProjMatrix", Camera.ProjectionMatrix);
        _shaderCaveWall.SetValue("uPlayerPos", playerPos);
        _shaderCaveWall.SetValue("uSightRadius", sightRadius);
    }
}