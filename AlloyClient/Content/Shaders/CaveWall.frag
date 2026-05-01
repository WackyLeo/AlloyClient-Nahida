#version 460 core

uniform sampler2D GameTexture;
uniform vec2 uPlayerPos;
uniform float uSightRadius;

layout(location = 0) in vec2 vUV;
layout(location = 1) in float vShade;
layout(location = 2) in float vZ;
layout(location = 3) in vec2 vWorldPos;
layout(location = 4) in vec3 vWorldPos3D;

out vec4 FragColor;

void main() {
    if (vShade < -0.5) {
        FragColor = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    vec4 color = texture(GameTexture, vUV);
    if (color.a < 0.1) discard;

    vec2 tileCenter = floor(vWorldPos) + vec2(0.5);
    vec2 playerTile = floor(uPlayerPos) + vec2(0.5);

    float dist = distance(tileCenter, playerTile);
    float fogStart = uSightRadius - 3.0;
    float fogEnd = uSightRadius;

    float fogFactor = clamp((dist - fogStart) / (fogEnd - fogStart), 0.0, 1.0);
    float steps = 5.0;
    fogFactor = floor(fogFactor * steps) / steps;

    float brightness = mix(1.0, 0.4, fogFactor);
    vec3 dx = dFdx(vWorldPos3D);
    vec3 dy = dFdy(vWorldPos3D);
    vec3 normal = normalize(cross(dy, dx));

    vec3 absN = abs(normal);
    vec3 snappedNormal = vec3(0.0);
    if (absN.x > absN.y && absN.x > absN.z) snappedNormal.x = sign(normal.x);
    else if (absN.y > absN.x && absN.y > absN.z) snappedNormal.y = sign(normal.y);
    else snappedNormal.z = sign(normal.z);

    vec3 lightDir = normalize(vec3(1.0, 3.0, 2.0));
    float dotLight = max(0.0, dot(snappedNormal, lightDir));
    float calculatedShade = 0.75 + 0.25 * dotLight;

    color.rgb *= brightness;
    color.rgb *= calculatedShade;

    FragColor = color;
}