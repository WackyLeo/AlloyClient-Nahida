#version 460 core

uniform sampler2D GameTexture;
uniform vec2 uPlayerPos;
uniform float uSightRadius;

layout(location = 0) in vec2 vUV;
layout(location = 1) in float vShade;
layout(location = 2) in float vZ;
layout(location = 3) in vec2 vWorldPos;

out vec4 FragColor;

void main() {
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
    color.rgb *= brightness;
    FragColor = color;
    
    //if (color.a < 0.01) discard;

    //color /= color.a;
    //color.rgb -= vShade * 0.241 * clamp(0.6 - vZ, 0.0, 0.6);

    //FragColor = color;
}