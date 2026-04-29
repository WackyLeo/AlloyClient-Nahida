#version 460 core

uniform mat4 WorldMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjMatrix;

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 UV;
layout(location = 2) in float Shade;
layout(location = 3) in float SortId;

layout(location = 0) out vec2  vUV;
layout(location = 1) out float vShade;
layout(location = 2) out float vZ;
layout(location = 3) out vec2  vWorldPos;

void main() {
    vec4 worldPos = vec4(Position, 1.0) * WorldMatrix * ViewMatrix * ProjMatrix;
    worldPos.z = SortId;
    gl_Position = worldPos;

    vUV = UV;
    vShade = Shade;
    vZ = Position.z;
    vWorldPos = Position.xy;
}