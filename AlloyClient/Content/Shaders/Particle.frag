#version 460 core

uniform sampler2D GameTexture;

out vec4 FragColor;

in vec2 BaseUV;
in vec4 Color;

void main() {
    vec2 dUV = fwidth(BaseUV);
    float outlineThickness = 1.0;
    vec2 border = dUV * outlineThickness;
    if (BaseUV.x < border.x || BaseUV.x > 1.0 - border.x ||
    BaseUV.y < border.y || BaseUV.y > 1.0 - border.y) {
        FragColor = vec4(0, 0, 0, 1);

    } else if (Color.w > -1) {
        FragColor = texture(GameTexture, Color.xy);
    } else {
        FragColor = vec4(Color.xyz, 1);
    }
}