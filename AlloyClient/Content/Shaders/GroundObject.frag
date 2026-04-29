#version 460 core

#define TileBuffer

uniform sampler2D GameTexture;
uniform vec4 AlphaBlends[8];
uniform vec2 uPlayerPos;
uniform float uSightRadius;

in GROUND_OUTPUT {
    vec2 baseUV;
    vec2 coreUV;
    vec4 UV;
    vec4 Mask;
    float Swizzle;
    vec2 WorldPos;
} vsOutput;

out vec4 FragColor;

void main() {
    vec2 tileTexelSize = round(vsOutput.UV.zw * 4096.0);
    vec2 tileTexelOrigin = round(vsOutput.UV.xy * 4096.0);
    vec2 wrappedTexels = floor(fract(vsOutput.coreUV) * tileTexelSize);
    vec2 uv = (tileTexelOrigin + wrappedTexels + 0.5) / 4096.0;
    uv = mix(uv.xy, uv.yx, vsOutput.Swizzle);
    vec4 ogColor = texture(GameTexture, uv);
    
    if (vsOutput.Mask.x > -1.0) {
        vec2 maskTexelSize = round(vsOutput.Mask.zw * 4096.0);
        vec2 maskTexelOrigin = round(vsOutput.Mask.xy * 4096.0);
        vec2 maskTexels = floor(vsOutput.baseUV * maskTexelSize);
        float alpha = texture(GameTexture, (maskTexelOrigin + maskTexels + 0.5) / 4096.0).a;
        ogColor.a = alpha;
    }
    
    //if (ogColor.a < 0.1) discard;
    //FragColor = ogColor;
    
    if (ogColor.a < 0.1) discard;
    vec2 tileCenter = floor(vsOutput.WorldPos) + vec2(0.5);
    vec2 playerTile = floor(uPlayerPos) + vec2(0.5);

    float dist = distance(tileCenter, playerTile);
    float fogStart = uSightRadius - 3.0;
    float fogEnd = uSightRadius;

    float fogFactor = clamp((dist - fogStart) / (fogEnd - fogStart), 0.0, 1.0);
    float steps = 5.0;
    fogFactor = floor(fogFactor * steps) / steps;

    float brightness = mix(1.0, 0.4, fogFactor);
    ogColor.rgb *= brightness;
    FragColor = ogColor;
}