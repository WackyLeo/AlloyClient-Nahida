using System.Reflection;
using System.Text;

namespace AlloyClient.Engine.Graphics;

internal static class ShaderHelper {
    private static readonly Assembly ShaderAssembly = Assembly.GetEntryAssembly();

    internal static void LoadUniformProperties(int handle, Dictionary<string, Shader.UniformInfo> uniforms) {
        GL.GetProgrami(handle, ProgramProperty.ActiveUniforms, out var count);
        if (count == 0)
            return;

        GL.GetProgrami(handle, ProgramProperty.ActiveUniformMaxLength, out var maxLength);
        for (var i = 0u; i < count; i++) {
            var uniform = GL.GetActiveUniform(handle, i, maxLength, out _, out var size, out var type);
            var location = GL.GetUniformLocation(handle, uniform);
            uniforms[uniform] = new Shader.UniformInfo(location, type, size);
        }
    }

    internal static void LoadUniformBlocks(int handle, Dictionary<string, Shader.UniformBlockInfo> blocks) {
        GL.GetProgramInterfacei(handle, ProgramInterface.UniformBlock, ProgramInterfacePName.ActiveResources,
            out var count);
        if (count == 0)
            return;

        GL.GetProgramInterfacei(handle, ProgramInterface.UniformBlock, ProgramInterfacePName.MaxNameLength,
            out var maxLength);
        for (var i = 0u; i < count; i++) {
            var blockName = GL.GetProgramResourceName(handle, ProgramInterface.UniformBlock, i, maxLength, out _);
            blocks[blockName] = new Shader.UniformBlockInfo(i);
        }
    }

    private static string ReadShader(string ext, string name) {
        var resourceName = $"Shaders/{name}.{ext}";
        using var stream = ShaderAssembly.GetManifestResourceStream(resourceName);

        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static void Compile(int handle, string path, (string, string)[] defines) {
        var name = Path.GetFileName(path);

        var vs = new StringBuilder(ReadShader("vert", name));
        var fs = new StringBuilder(ReadShader("frag", name));

        if (defines != null)
            foreach (var def in defines) {
                var txt1 = $"#define {def.Item1}";
                var txt2 = $"{txt1} {def.Item2}";
                vs.Replace(txt1, txt2);
                fs.Replace(txt1, txt2);
            }

        var vertexHandle = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexHandle, vs.ToString());
        CompileShader(vertexHandle, $"{name}.vert");

        var fragmentHandle = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentHandle, fs.ToString());
        CompileShader(fragmentHandle, $"{name}.frag");

        GL.AttachShader(handle, vertexHandle);
        GL.AttachShader(handle, fragmentHandle);
        LinkProgram(handle, name);

        GL.DetachShader(handle, vertexHandle);
        GL.DetachShader(handle, fragmentHandle);
        GL.DeleteShader(vertexHandle);
        GL.DeleteShader(fragmentHandle);
    }

    private static void CompileShader(int shader, string path) {
        GL.CompileShader(shader);

        GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out var code);
        if (code != (int)All.True) {
            GL.GetShaderInfoLog(shader, out var infoLog);
            throw new Exception($"Error compiling shader {path}.{Environment.NewLine}{infoLog}");
        }
    }

    private static void LinkProgram(int handle, string name) {
        GL.LinkProgram(handle);

        GL.GetProgrami(handle, ProgramProperty.LinkStatus, out var code);

        if (code != (int)All.True) {
            GL.GetProgramInfoLog(handle, out var info);
            throw new Exception($"Error linking shader {name}.{Environment.NewLine}{info}");
        }
    }
}