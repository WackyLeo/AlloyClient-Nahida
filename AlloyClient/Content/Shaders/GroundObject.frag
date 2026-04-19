#version 460 core

#define TileBuffer

uniform sampler2D GameTexture;
uniform vec4 AlphaBlends[8];

in GROUND_OUTPUT {
    vec2 baseUV;
    vec2 coreUV;
    vec4 UV;
    vec4 Mask;
    float Swizzle;
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
    
    if (ogColor.a < 0.1) discard;
    FragColor = ogColor;
}