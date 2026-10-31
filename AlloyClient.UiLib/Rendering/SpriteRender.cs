using System;
using AlloyClient.Engine.Graphics;
using AlloyClient.Engine.Graphics.Buffers;
using AlloyClient.UiLib.Core;
using OpenTK.Graphics.OpenGL;

namespace AlloyClient.UiLib.Rendering;

public static class SpriteRender
{
    private const int InstanceBufferSize = 3000;
    private const int IndexBufferSize = InstanceBufferSize * 6;
    private const int VertexBufferSize = InstanceBufferSize * 4;

    private static ushort _instanceCount;
    private static SpriteInstanceData[] _instanceData;
    private static StorageBuffer<SpriteInstanceData> _instanceBuffer;

    private static int _indexCount;
    private static ushort[] _indices;
    private static IndexBuffer _indexBuffer;

    private static ushort _vertexCount;
    private static SpriteVertexData[] _vertices;
    private static VertexBuffer<SpriteVertexData> _vertexBuffer;

    private static VertexArrayObject _vao;

    internal static void Init()
    {
        _instanceData = new SpriteInstanceData[InstanceBufferSize];
        _instanceBuffer = new StorageBuffer<SpriteInstanceData>(InstanceBufferSize);

        _indices = new ushort[IndexBufferSize];
        _indexBuffer = new IndexBuffer(IndexBufferSize);

        _vertices = new SpriteVertexData[VertexBufferSize];
        _vertexBuffer = new VertexBuffer<SpriteVertexData>(SpriteVertexData.VertexStride, VertexBufferSize);

        _vao = new VertexArrayObject();

        _vertexBuffer.BindTo(_vao);
        _indexBuffer.BindTo(_vao);

        GL.BindVertexArray(0);
    }
    private static bool _loggedView;
    internal static void StartDraw()
    {
        _vao.Bind();
        _instanceBuffer.BindToIndex(0);

        UiRender.UiShader.Apply();
        UiRender.UiShader.SetValue("ViewMatrix", UiRender.ViewMatrix);
        UiRender.UiShader.SetValue("Time", (float)Stage.GameTime.TotalMs / 1000f);
        
        if (!_loggedView) {
            _loggedView = true;
            Console.WriteLine($"ViewMatrix M11={UiRender.ViewMatrix.M11} M22={UiRender.ViewMatrix.M22} Screen={UiRender.Screen}");
        }
        _vao.Bind();
        _instanceBuffer.BindToIndex(0);

        UiRender.UiShader.Apply();
        UiRender.UiShader.SetValue("Time", (float)Stage.GameTime.TotalMs / 1000f);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.StencilTest);

        _instanceCount = 0;
        _indexCount = 0;
        _vertexCount = 0;
    }

    internal static void Draw(SpriteInstanceData data, ReadOnlySpan<ushort> indices, ReadOnlySpan<VertexUi> vertices)
    {
        if (_instanceCount + 1 > InstanceBufferSize || _indexCount + indices.Length > IndexBufferSize ||
            _vertexCount + vertices.Length > VertexBufferSize)
            Flush();

        _instanceData[_instanceCount] = data;
        var instanceId = _instanceCount++;
        var len = indices.Length;
        for (var i = 0; i < len; i++) _indices[_indexCount + i] = (ushort)(_vertexCount + indices[i]);
        _indexCount += len;

        var vertexLen = vertices.Length;
        for (var i = 0; i < vertexLen; i++) _vertices[_vertexCount + i] = new SpriteVertexData(vertices[i], instanceId);
        _vertexCount += (ushort)vertexLen;

        UiRender.LastRenderCount++;
    }

    internal static void EndDraw()
    {
        Flush();
        GL.BindVertexArray(0);
    }

    private static void Flush()
    {
        if (_instanceCount == 0) return;

        _instanceBuffer.SetData(_instanceData.AsSpan(0, _instanceCount));
        _indexBuffer.SetData(_indices.AsSpan(0, _indexCount));
        _vertexBuffer.SetData(_vertices.AsSpan(0, _vertexCount));

        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedShort, 0);

        _instanceCount = 0;
        _indexCount = 0;
        _vertexCount = 0;
    }
}