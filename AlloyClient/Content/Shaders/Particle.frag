#version 460 core

uniform sampler2D GameTexture;

out vec4 FragColor;

layout(location = 0) in vec2 BaseUV;
layout(location = 1) in vec4 Color;
layout(location = 2) in float Depth;

void main() {
    float alpha = Color.w < 0.0 ? abs(Color.w) : 1.0;  

    vec2 dUV = fwidth(BaseUV);
    vec2 border = dUV * 1.0;
    if (BaseUV.x < border.x || BaseUV.x > 1.0 - border.x ||
    BaseUV.y < border.y || BaseUV.y > 1.0 - border.y) {
        FragColor = vec4(0, 0, 0, alpha);
    } else if (Color.w > -1.0) {
        FragColor = texture(GameTexture, Color.xy);
    } else {
        FragColor = vec4(Color.xyz, alpha);
    }
}