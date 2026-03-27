#version 460 core

uniform sampler2D GameTexture;

in vec2  vUV;
in float vShade;
in float vZ;

out vec4 FragColor;

void main() {
    vec4 color = texture(GameTexture, vUV);
    if (color.a < 0.01) discard;

    color /= color.a;
    color.rgb -= vShade * 0.241 * clamp(0.6 - vZ, 0.0, 0.6);
    
    FragColor = color;
}