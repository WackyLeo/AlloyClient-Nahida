#version 460 core

uniform sampler2D GameTexture;

in MODEL_OUT {
    vec2 BaseUV;
    vec4 UV;
    vec3 Extra;
    float Zed;
} output1;

out vec4 FragColor;

vec2 map(vec2 base, vec2 uvMin, vec2 uvMax) {
    return vec2(base.x * (uvMax.x - uvMin.x) + uvMin.x, base.y * (uvMax.y - uvMin.y) + uvMin.y);
}

void main() {
    vec2 uv = map(output1.BaseUV, output1.UV.xy, output1.UV.xy + output1.UV.zw);
    vec4 color = texture(GameTexture, uv);

    if (color.a < 0.1) discard;

    color.rgb /= color.a;
    color.rgb -= output1.Extra.z * 0.241 * clamp(0.6 - output1.Zed, 0.0, 0.6);

    FragColor = color;
}