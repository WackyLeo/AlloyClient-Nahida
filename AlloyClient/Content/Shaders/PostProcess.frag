#version 460 core

layout (location = 0) out vec4 FragColor;

uniform sampler2D colourTexture;

in vec2 vertUVs;

void main() {
    vec3 screenTexture = texture(colourTexture, vertUVs).xyz;

    screenTexture *= 5.0f;

    FragColor = vec4(screenTexture, 1.0);
}