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
    vec3 WorldPos3D;
} output1;

out vec4 FragColor;

vec2 map(vec2 base, vec2 uvMin, vec2 uvMax) {
    return vec2(base.x * (uvMax.x - uvMin.x) + uvMin.x, base.y * (uvMax.y - uvMin.y) + uvMin.y);
}

void main() {
    vec2 uv = map(output1.BaseUV, output1.UV.xy, output1.UV.xy + output1.UV.zw);
    vec4 color = texture(GameTexture, uv);

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

    vec3 dx = dFdx(output1.WorldPos3D);
    vec3 dy = dFdy(output1.WorldPos3D);
    vec3 normal = normalize(cross(dy, dx));

    vec3 absN = abs(normal);
    vec3 snappedNormal = vec3(0.0);
    if (absN.x > absN.y && absN.x > absN.z) snappedNormal.x = sign(normal.x);
    else if (absN.y > absN.x && absN.y > absN.z) snappedNormal.y = sign(normal.y);
    else snappedNormal.z = sign(normal.z);

    vec3 lightDir = normalize(vec3(1.0, 3.0, 2.0));
    float dotLight = max(0.0, dot(snappedNormal, lightDir));
    float calculatedShade = 0.75 + 0.25 * dotLight;
    float finalShade = mix(1.0, calculatedShade, step(0.5, output1.Extra.z));

    color.rgb *= brightness;
    color.rgb *= finalShade;
    FragColor = color;
}