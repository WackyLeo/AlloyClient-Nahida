#version 460 core
precision highp float;

in OBJECT_OUT {
    vec2 BaseUV;
    vec4 UV;
    flat float ExtraType;
    flat float ExtraSortId;
    flat float ExtraShade;
    flat float ExtraAlpha;
    vec4 Color;
    vec4 Mask1;
    vec4 Mask2;
    vec2 WorldPos;
} vsOutput;

out vec4 FragColor;

uniform sampler2D GameTexture;
uniform int RenderPass;
uniform float PixelRange;
uniform vec2 TextTextureSize;
uniform sampler2D TextTexture;
uniform vec2 uPlayerPos;
uniform float uSightRadius;

const int OpaquePass = 0;

vec2 map(vec2 base, vec2 uvMin, vec2 uvMax) {
    return vec2(base.x * (uvMax.x - uvMin.x) + uvMin.x, base.y * (uvMax.y - uvMin.y) + uvMin.y);
}

float median(float a, float b, float c) { return max(min(a, b), min(max(a, b), c)); }

bool inBounds(vec2 uv, vec2 minUV, vec2 maxUV){
    return (uv.x >= minUV.x && uv.x <= maxUV.x && uv.y >= minUV.y && uv.y <= maxUV.y);
}

vec4 GetGameObject() {
    vec2 uvMax = vsOutput.UV.xy + vsOutput.UV.zw;
    vec2 minUV = vsOutput.UV.xy;
    vec2 uv = map(vsOutput.BaseUV, minUV, uvMax);
    vec2 dx = dFdx(uv);
    vec2 dy = dFdy(uv);

    vec4 color = texture(GameTexture, uv);
    color.rgb -= vsOutput.ExtraShade * 0.241 * clamp(vsOutput.BaseUV.y - 0.4, 0.0, 0.4);

    if (RenderPass == OpaquePass) {
        if (color.a < 0.1 || vsOutput.ExtraAlpha < 1.0) discard;
        vec2 tileCenter = floor(vsOutput.WorldPos) + vec2(0.5);
        vec2 playerTile = floor(uPlayerPos) + vec2(0.5);

        float dist = distance(tileCenter, playerTile);
        float fogStart = uSightRadius - 3.0;
        float fogEnd = uSightRadius;

        float fogFactor = clamp((dist - fogStart) / (fogEnd - fogStart), 0.0, 1.0);
        float steps = 5.0;
        fogFactor = floor(fogFactor * steps) / steps;

        float brightness = mix(1.0, 0.4, fogFactor);
        color.rgb *= brightness;
        return color;
    }

    if (color.a >= 0.1) {
        if (vsOutput.ExtraAlpha < 1.0) return vec4(color.rgb, color.a * vsOutput.ExtraAlpha);
        discard;
    }

    int hitRadius = 999;
    float minDist = 999.0;
    int maxRadius = 6;

    for (int r = 1; r <= maxRadius; r++) {
        bool foundOpaquePixel = false;

        for (int x = -r; x <= r; x++) {
            vec2 offsetTop = vec2(float(x), float(-r));
            vec2 sampleTop = uv + offsetTop.x * dx + offsetTop.y * dy;
            if (inBounds(sampleTop, minUV, uvMax) && textureLod(GameTexture, sampleTop, 0.0).a > 0.1) {
                minDist = min(minDist, length(offsetTop));
                foundOpaquePixel = true;
            }
            vec2 offsetBot = vec2(float(x), float(r));
            vec2 sampleBot = uv + offsetBot.x * dx + offsetBot.y * dy;
            if (inBounds(sampleBot, minUV, uvMax) && textureLod(GameTexture, sampleBot, 0.0).a > 0.1) {
                minDist = min(minDist, length(offsetBot));
                foundOpaquePixel = true;
            }
        }
        for (int y = -r + 1; y <= r - 1; y++) {
            vec2 offsetLeft = vec2(float(-r), float(y));
            vec2 sampleLeft = uv + offsetLeft.x * dx + offsetLeft.y * dy;
            if (inBounds(sampleLeft, minUV, uvMax) && textureLod(GameTexture, sampleLeft, 0.0).a > 0.1) {
                minDist = min(minDist, length(offsetLeft));
                foundOpaquePixel = true;
            }
            vec2 offsetRight = vec2(float(r), float(y));
            vec2 sampleRight = uv + offsetRight.x * dx + offsetRight.y * dy;
            if (inBounds(sampleRight, minUV, uvMax) && textureLod(GameTexture, sampleRight, 0.0).a > 0.1) {
                minDist = min(minDist, length(offsetRight));
                foundOpaquePixel = true;
            }
        }
        if (foundOpaquePixel) { hitRadius = r; break; }
    }

    if (hitRadius <= 1) return vec4(vsOutput.Color.rgb, vsOutput.ExtraAlpha);

    if (minDist <= float(maxRadius)) {
        float distNorm = clamp((minDist - 1.0) / float(maxRadius - 1), 0.0, 1.0);
        float glowAlpha = pow(1.0 - distNorm, 1.75) * 0.55 * vsOutput.ExtraAlpha;
        if (glowAlpha > 0.01) return vec4(vsOutput.Color.rgb, glowAlpha);
    }
    discard;
}

vec4 GetText() {
    vec2 uv = map(vsOutput.BaseUV, vsOutput.UV.xy, vsOutput.UV.xy + vsOutput.UV.zw);
    vec4 mtsdf = texture(TextTexture, uv);
    float dist = median(mtsdf.r, mtsdf.g, mtsdf.b) - 0.5;

    vec2 unitRange = vec2(PixelRange) / TextTextureSize;
    vec2 screenSize = vec2(1.0) / fwidth(uv);
    float pxRange = max(0.5 * dot(unitRange, screenSize), 1.0);

    float bodyDist = dist * pxRange;
    float bodyAlpha = clamp(bodyDist + 0.5, 0.0, 1.0);
    float strokeAlpha = clamp(bodyDist + 1.3, 0.0, 1.0);
    float shadowAlpha = smoothstep(0.0, 0.6, mtsdf.a) * 0.7;

    vec4 finalColor = mix(vec4(0, 0, 0, 1), vsOutput.Color, bodyAlpha);
    float totalAlpha = max(strokeAlpha, shadowAlpha) * vsOutput.ExtraAlpha;

    if (RenderPass == OpaquePass) {
        if (totalAlpha < 1.0) discard;
        return vec4(finalColor.rgb, 1.0);
    }

    if (totalAlpha >= 1.0 || totalAlpha <= 0.01) discard;
    return vec4(finalColor.rgb, totalAlpha);
}

bool IsBarSolid(vec2 baseUV, float w, float h) {
    float px = baseUV.x * 60.0; float py = baseUV.y * 20.0;
    float offsetX = (60.0 - w) / 2.0; float offsetY = (20.0 - h) / 2.0;
    return (px >= offsetX && px <= offsetX + w && py >= offsetY && py <= offsetY + h);
}

vec4 GetBar() {
    vec2 uv = vsOutput.BaseUV;
    vec2 dx = dFdx(uv);
    vec2 dy = dFdy(uv);

    float w = vsOutput.Mask1.x;
    float h = vsOutput.Mask1.y;
    float fill = vsOutput.Mask1.w;

    bool isSolid = IsBarSolid(uv, w, h);
    if (RenderPass == OpaquePass) {
        if (!isSolid || vsOutput.ExtraAlpha < 1.0) discard;
        float quadW = 60.0;
        float px = uv.x * quadW;
        float offsetX = (quadW - w) / 2.0;
        vec3 fillColor = (px - offsetX <= w * fill) ? vsOutput.Color.rgb : vec3(0.1);
        return vec4(fillColor, 1.0);
    }

    if (isSolid) {
        if (vsOutput.ExtraAlpha < 1.0) {
            float quadW = 60.0;
            float px = uv.x * quadW;
            float offsetX = (quadW - w) / 2.0;
            vec3 fillColor = (px - offsetX <= w * fill) ? vsOutput.Color.rgb : vec3(0.1);
            return vec4(fillColor, vsOutput.ExtraAlpha);
        }
        discard;
    }

    int hitRadius = 999;
    float minDist = 999.0;
    int maxRadius = 6;

    for (int r = 1; r <= maxRadius; r++) {
        bool foundSolid = false;
        for (int x = -r; x <= r; x++) {
            vec2 offsetTop = vec2(float(x), float(-r));
            if (IsBarSolid(uv + offsetTop.x * dx + offsetTop.y * dy, w, h)) { minDist = min(minDist, length(offsetTop)); foundSolid = true; }
            vec2 offsetBot = vec2(float(x), float(r));
            if (IsBarSolid(uv + offsetBot.x * dx + offsetBot.y * dy, w, h)) { minDist = min(minDist, length(offsetBot)); foundSolid = true; }
        }
        for (int y = -r + 1; y <= r - 1; y++) {
            vec2 offsetLeft = vec2(float(-r), float(y));
            if (IsBarSolid(uv + offsetLeft.x * dx + offsetLeft.y * dy, w, h)) { minDist = min(minDist, length(offsetLeft)); foundSolid = true; }
            vec2 offsetRight = vec2(float(r), float(y));
            if (IsBarSolid(uv + offsetRight.x * dx + offsetRight.y * dy, w, h)) { minDist = min(minDist, length(offsetRight)); foundSolid = true; }
        }
        if (foundSolid) { hitRadius = r; break; }
    }

    if (hitRadius <= 1) return vec4(0.0, 0.0, 0.0, vsOutput.ExtraAlpha);
    
    if (minDist <= float(maxRadius)) {
        float distNorm = clamp((minDist - 1.0) / float(maxRadius - 1), 0.0, 1.0);
        float glowAlpha = pow(1.0 - distNorm, 1.75) * 0.55 * vsOutput.ExtraAlpha;
        if (glowAlpha > 0.01) return vec4(0.0, 0.0, 0.0, glowAlpha);
    }
    discard;
}

void main() {
    float id = vsOutput.ExtraType;
    if (id == 0.0 || id == 5.0) FragColor = GetGameObject();
    else if (id == 3.0) FragColor = GetText();
    else if (id == 4.0) FragColor = GetBar();
    else discard;
}