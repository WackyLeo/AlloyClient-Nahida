#version 460 core

#define ObjectBuffer

uniform mat4 WorldMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjMatrix;
uniform mat4 BillMatrix;
uniform int RenderPass;

const int OpaquePass = 0;
const int OutlineGlowPass = 1;

const vec2 objPos[6] = vec2[6](
vec2(-0.5, 0.5), vec2(0.5, 0.5), vec2(-0.5, -0.5),
vec2(-0.5, -0.5), vec2(0.5, 0.5), vec2(0.5, -0.5)
);

const vec2 objUV[6] = vec2[6](
vec2(0.0, 1.0), vec2(1.0, 1.0), vec2(0.0, 0.0),
vec2(0.0, 0.0), vec2(1.0, 1.0), vec2(1.0, 0.0)
);

struct extra {
    float Type;
    float SortId;
    float Shade;
    float Alpha;
};

struct InstanceData {
    vec4 Position;
    vec4 UV;
    vec4 Scale;
    vec4 Rotation;
    extra Extra;
    vec4 Color;
    vec4 Mask1;
    vec4 Mask2;
};

layout(std140, binding = 0) readonly buffer InstanceBuffer {
    InstanceData data[ObjectBuffer];
} instanceBuffer;

out OBJECT_OUT {
    vec2 BaseUV;
    vec4 UV;
    flat float ExtraType;
    flat float ExtraSortId;
    flat float ExtraShade;
    flat float ExtraAlpha;
    vec4 Color;
    vec4 Mask1;
    vec4 Mask2;
} vsOutput;

vec2 GetUV(vec2 uv, float flip) {
    uv.x = 0.5 + (0.5 - uv.x) * flip;
    return uv;
}

void main() {
    int instanceId = gl_VertexID / 6;
    int verId = gl_VertexID % 6;

    InstanceData data = instanceBuffer.data[instanceId];
    vec4 position = vec4(objPos[verId], 0, 1);
    position.xy *= data.Scale.xy;

    mat4 rotate = mat4(
    data.Rotation.y * data.Rotation.z, data.Rotation.x * data.Rotation.z, 0, data.Scale.z * data.Rotation.z * -data.Rotation.w,
    -data.Rotation.x * data.Rotation.z, data.Rotation.y * data.Rotation.z, 0, data.Scale.w * data.Rotation.z,
    0, 0, 1, 0,
    0, 0, 0, 1
    );

    position = position * rotate * BillMatrix;
    position.xyz += data.Position.xyz;
    position = position * WorldMatrix * ViewMatrix * ProjMatrix;
    position.z = data.Extra.SortId;
    gl_Position = position;

    vsOutput.BaseUV = GetUV(objUV[verId], data.Rotation.w);
    vsOutput.UV = data.UV;
    vsOutput.ExtraType = data.Extra.Type;
    vsOutput.ExtraSortId = data.Extra.SortId;
    vsOutput.ExtraShade = data.Extra.Shade;
    vsOutput.ExtraAlpha = data.Extra.Alpha;
    vsOutput.Color = data.Color;
    vsOutput.Mask1 = data.Mask1;
    vsOutput.Mask2 = data.Mask2;
}