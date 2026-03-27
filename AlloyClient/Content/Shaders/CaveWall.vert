#version 460 core

uniform mat4 WorldMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjMatrix;

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 UV;
layout(location = 2) in float Shade;

out vec2 vUV;
out float vShade;
out float vZ;

void main() {
    vec4 worldPos = vec4(Position, 1.0) * WorldMatrix * ViewMatrix * ProjMatrix;
    vUV = UV;
    vShade = Shade;
    vZ = Position.z;
    gl_Position = worldPos;
}