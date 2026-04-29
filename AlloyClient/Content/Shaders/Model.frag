#version 460 core

uniform sampler2D GameTexture;
uniform vec2 uPlayerPos;
uniform float uSightRadius;

in MODEL_OUT {
    vec2 BaseUV;
    vec4 UV;
    vec3 Extra;
    float Zed;
    vec2 WorldPos;
} output1;

out vec4 FragColor;

vec2 map(vec2 base, vec2 uvMin, vec2 uvMax) {
    return vec2(base.x * (uvMax.x - uvMin.x) + uvMin.x, base.y * (uvMax.y - uvMin.y) + uvMin.y);
}

void main() {
    vec2 uv = map(output1.BaseUV, output1.UV.xy, output1.UV.xy + output1.UV.zw);
    vec4 color = texture(GameTexture, uv);

    //if (color.a < 0.1) discard;

    //color.rgb /= color.a;
    //color.rgb -= output1.Extra.z * 0.241 * clamp(0.6 - output1.Zed, 0.0, 0.6);

    //FragColor = color;

    if (color.a < 0.1) discard;
    vec2 tileCenter = floor(output1.WorldPos) + vec2(0.5);
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
}