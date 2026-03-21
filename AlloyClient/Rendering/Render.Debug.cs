using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AlloyClient.Engine.Graphics.Buffers;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace AlloyClient.Rendering;

public static partial class Render {

    public static bool DebugEnabled = false;

    [StructLayout(LayoutKind.Sequential)]
    private struct DebugVertex {
        public Vector2 Position;
        public Vector3 Color;
    }
    
    private static readonly List<DebugVertex> _debugVerts = [];
    private static int _debugVao = -1;
    private static int _debugVbo = -1;
    private static int _debugShader = -1;
    private static int _debugVpUniform = -1;
    private static bool _debugInitialized = false;

    private static void InitDebug() {
        if (_debugInitialized) return;
        _debugInitialized = true;

        const string vert = @"
            #version 330 core
            layout(location=0) in vec2 aPos;
            layout(location=1) in vec3 aCol;
            uniform mat4 uVP;
            out vec3 vCol;
            void main() { gl_Position = uVP * vec4(aPos, 0.0, 1.0); vCol = aCol; }";

        const string frag = @"
            #version 330 core
            in vec3 vCol;
            out vec4 o;
            void main() { o = vec4(vCol, 1.0); }";

        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vert);
        GL.CompileShader(vs);
        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, frag);
        GL.CompileShader(fs);
        _debugShader = GL.CreateProgram();
        GL.AttachShader(_debugShader, vs);
        GL.AttachShader(_debugShader, fs);
        GL.LinkProgram(_debugShader);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        _debugVpUniform = GL.GetUniformLocation(_debugShader, "uVP");

        _debugVao = GL.GenVertexArray();
        _debugVbo = GL.GenBuffer();
        GL.BindVertexArray(_debugVao);
        GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.ArrayBuffer, _debugVbo);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
        GL.BindVertexArray(0);
    }

    public static void DebugLine(Vector2 from, Vector2 to, Vector3 color, float thickness = 0.04f) {
        if (!DebugEnabled) return;

        var dir = to - from;
        if (dir.LengthSquared < 0.00001f) return;
        dir.Normalize();
        var perp = new Vector2(-dir.Y, dir.X) * (thickness * 0.5f);

        var a = from - perp;
        var b = from + perp;
        var c = to   + perp;
        var d = to   - perp;

        // Triangle 1
        _debugVerts.Add(new DebugVertex { Position = a, Color = color });
        _debugVerts.Add(new DebugVertex { Position = b, Color = color });
        _debugVerts.Add(new DebugVertex { Position = c, Color = color });
        // Triangle 2
        _debugVerts.Add(new DebugVertex { Position = a, Color = color });
        _debugVerts.Add(new DebugVertex { Position = c, Color = color });
        _debugVerts.Add(new DebugVertex { Position = d, Color = color });
    }

    public static void DebugCircle(Vector2 center, float radius, Vector3 color, float thickness = 0.04f, int segments = 32) {
        if (!DebugEnabled) return;
        for (int i = 0; i < segments; i++) {
            float a0 = i       / (float)segments * MathF.PI * 2f;
            float a1 = (i + 1) / (float)segments * MathF.PI * 2f;
            DebugLine(
                center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius,
                center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius,
                color, thickness);
        }
    }

    public static void DebugArrow(Vector2 from, float angle, float length, Vector3 color, float thickness = 0.04f) {
        if (!DebugEnabled) return;
        var to    = from + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
        var left  = to   + new Vector2(MathF.Cos(angle + 2.5f), MathF.Sin(angle + 2.5f)) * length * 0.2f;
        var right = to   + new Vector2(MathF.Cos(angle - 2.5f), MathF.Sin(angle - 2.5f)) * length * 0.2f;
        DebugLine(from, to,    color, thickness);
        DebugLine(to,   left,  color, thickness * 0.7f);
        DebugLine(to,   right, color, thickness * 0.7f);
    }

    public static void FlushDebug(Matrix4 viewProjection) {
        if (!DebugEnabled || _debugVerts.Count == 0) return;
        InitDebug();

        var data = new float[_debugVerts.Count * 5];
        for (int i = 0; i < _debugVerts.Count; i++) {
            var v = _debugVerts[i];
            data[i * 5 + 0] = v.Position.X;
            data[i * 5 + 1] = v.Position.Y;
            data[i * 5 + 2] = v.Color.X;
            data[i * 5 + 3] = v.Color.Y;
            data[i * 5 + 4] = v.Color.Z;
        }

        GL.BindVertexArray(_debugVao);
        GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.ArrayBuffer, _debugVbo);
        GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.ArrayBuffer,
            data.Length * sizeof(float), data,
            OpenTK.Graphics.OpenGL.BufferUsage.StreamDraw);

        GL.UseProgram(_debugShader);
        GL.UniformMatrix4f(_debugVpUniform, 1, false, ref viewProjection);

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _debugVerts.Count);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(0);
        GL.UseProgram(0);
        _debugVerts.Clear();
    }
}