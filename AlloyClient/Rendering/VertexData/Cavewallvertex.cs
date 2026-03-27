using System.Runtime.InteropServices;
using AlloyClient.Engine.Graphics.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
 
namespace AlloyClient.Rendering.VertexData;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CaveWallVertex(Vector3 position, Vector2 uv, float shade) : IVertexData<CaveWallVertex> {
    public Vector3 Position = position;
    public Vector2 UV = uv;
    public float Shade = shade;   
 
    public static VertexStride VertexStride { get; } = new([
        new ElementFormat(0, VertexAttribType.Float, FormatType.Vector3),
        new ElementFormat(1, VertexAttribType.Float, FormatType.Vector2),
        new ElementFormat(2, VertexAttribType.Float, FormatType.Default),
    ]);

    public bool Equals(CaveWallVertex other) {
        throw new System.NotImplementedException();
    }
}