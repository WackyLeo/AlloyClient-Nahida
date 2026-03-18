#version 460 core

struct extra {
    float Type;
    float SortId;
    float Shade;
    float Alpha;
};

in OBJECT_OUT {
    vec2 BaseUV;
    vec4 UV;
    extra Extra;
    vec4 Color;
    vec4 Mask1;
    vec4 Mask2;
} vsInput;

out vec4 FragColor;

uniform sampler2D GameTexture;
uniform int RenderPass;

uniform float PixelRange;
uniform vec2 TextTextureSize;
uniform sampler2D TextTexture;

const int OpaquePass = 0;
const int OutlineGlowPass = 1;

const float TypeGameObject = 0.0;
const float TypeText = 3.0;
const float TypeBar = 4.0;
const float TypeEffect = 5.0;

vec2 map(vec2 base, vec2 uvMin, vec2 uvMax) {
    return vec2(base.x * (uvMax.x - uvMin.x) + uvMin.x, base.y * (uvMax.y - uvMin.y) + uvMin.y);
}

float median(float a, float b, float c) {
    return max(min(a, b), min(max(a, b), c));
}

bool inBounds(vec2 uv, vec2 minUV, vec2 maxUV){
    return (uv.x >= minUV.x && uv.x <= maxUV.x && uv.y >= minUV.y && uv.y <= maxUV.y);
}

vec4 GetGameObject() {
    vec2 uvMax = vsInput.UV.xy + vsInput.UV.zw;
    vec2 minUV = vsInput.UV.xy;
    vec2 uv = map(vsInput.BaseUV, minUV, uvMax);
    vec2 dx = dFdx(uv);
    vec2 dy = dFdy(uv);

    vec4 color = textureGrad(GameTexture, uv, dx, dy);
    color.rgb -= vsInput.Extra.Shade * 0.241 * clamp(vsInput.BaseUV.y - 0.4, 0.0, 0.4);

    if (RenderPass == OpaquePass) {
        if (color.a < 0.1 || vsInput.Extra.Alpha < 1.0) discard;
        return color;
    }

    if (color.a >= 0.1) {
        if (vsInput.Extra.Alpha < 1.0) return color;
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
            if (inBounds(sampleTop, minUV, uvMax) && textureGrad(GameTexture, sampleTop, dx, dy).a > 0.1) {
                minDist = min(minDist, length(offsetTop));
                foundOpaquePixel = true;
            }
            vec2 offsetBot = vec2(float(x), float(r));
            vec2 sampleBot = uv + offsetBot.x * dx + offsetBot.y * dy;
            if (inBounds(sampleBot, minUV, uvMax) && textureGrad(GameTexture, sampleBot, dx, dy).a > 0.1) {
                minDist = min(minDist, length(offsetBot));
                foundOpaquePixel = true;
            }
        }
        for (int y = -r + 1; y <= r - 1; y++) {
            vec2 offsetLeft = vec2(float(-r), float(y));
            vec2 sampleLeft = uv + offsetLeft.x * dx + offsetLeft.y * dy;
            if (inBounds(sampleLeft, minUV, uvMax) && textureGrad(GameTexture, sampleLeft, dx, dy).a > 0.1) {
                minDist = min(minDist, length(offsetLeft));
                foundOpaquePixel = true;
            }
            vec2 offsetRight = vec2(float(r), float(y));
            vec2 sampleRight = uv + offsetRight.x * dx + offsetRight.y * dy;
            if (inBounds(sampleRight, minUV, uvMax) && textureGrad(GameTexture, sampleRight, dx, dy).a > 0.1) {
                minDist = min(minDist, length(offsetRight));
                foundOpaquePixel = true;
            }
        }
        if (foundOpaquePixel) { hitRadius = r; break; }
    }

    if (hitRadius <= 1) return vec4(vsInput.Color.rgb, 1.0);

    if (minDist <= float(maxRadius)) {
        float distNorm = clamp((minDist - 1.0) / float(maxRadius - 1), 0.0, 1.0);
        float glowAlpha = pow(1.0 - distNorm, 1.75) * 0.55;
        if (glowAlpha > 0.01) return vec4(vsInput.Color.rgb, glowAlpha);
    }
    discard;
}

vec4 GetText() {
    vec2 uv = map(vsInput.BaseUV, vsInput.UV.xy, vsInput.UV.xy + vsInput.UV.zw);
    vec4 mtsdf = texture(TextTexture, uv);
    float dist = median(mtsdf.r, mtsdf.g, mtsdf.b) - 0.5;

    vec2 unitRange = vec2(PixelRange) / TextTextureSize;
    vec2 screenSize = vec2(1.0) / fwidth(uv);
    float pxRange = max(0.5 * dot(unitRange, screenSize), 1.0);

    float bodyDist = dist * pxRange;
    float bodyAlpha = clamp(bodyDist + 0.5, 0.0, 1.0);

    float outlineThickness = 0.8;
    float strokeAlpha = clamp(bodyDist + outlineThickness + 0.5, 0.0, 1.0);
    float shadowAlpha = smoothstep(0.0, 0.6, mtsdf.a) * 0.7;

    vec4 outlineColor = vec4(0.0, 0.0, 0.0, 1.0);
    vec4 finalColor = mix(outlineColor, vsInput.Color, bodyAlpha);

    float totalAlpha = max(strokeAlpha, shadowAlpha) * vsInput.Extra.Alpha;

    if (RenderPass == OpaquePass) {
        if (totalAlpha < 1.0) discard;
        return vec4(finalColor.rgb, 1.0);
    }

    if (totalAlpha >= 1.0) discard;
    if (totalAlpha <= 0.01) discard;

    return vec4(finalColor.rgb, totalAlpha);
}

bool IsBarSolid(vec2 baseUV, float w, float h) {
    float quadW = 60.0;
    float quadH = 20.0;
    float px = baseUV.x * quadW;
    float py = baseUV.y * quadH;
    float offsetX = (quadW - w) / 2.0;
    float offsetY = (quadH - h) / 2.0;
    return (px >= offsetX && px <= offsetX + w && py >= offsetY && py <= offsetY + h);
}

vec4 GetBar() {
    vec2 uv = vsInput.BaseUV;
    vec2 dx = dFdx(uv);
    vec2 dy = dFdy(uv);

    float w = vsInput.Mask1.x;
    float h = vsInput.Mask1.y;
    float fill = vsInput.Mask1.w;

    bool isSolid = IsBarSolid(uv, w, h);
    if (RenderPass == OpaquePass) {
        if (!isSolid || vsInput.Extra.Alpha < 1.0) discard;
        float quadW = 60.0;
        float px = uv.x * quadW;
        float offsetX = (quadW - w) / 2.0;
        vec3 fillColor = (px - offsetX <= w * fill) ? vsInput.Color.rgb : vec3(0.1);
        return vec4(fillColor, 1.0);
    }

    if (isSolid) {
        if (vsInput.Extra.Alpha < 1.0) {
            float quadW = 60.0;
            float px = uv.x * quadW;
            float offsetX = (quadW - w) / 2.0;
            vec3 fillColor = (px - offsetX <= w * fill) ? vsInput.Color.rgb : vec3(0.1);
            return vec4(fillColor, vsInput.Extra.Alpha);
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

    if (hitRadius <= 1) return vec4(0.0, 0.0, 0.0, 1.0);
    if (minDist <= float(maxRadius)) {
        float distNorm = clamp((minDist - 1.0) / float(maxRadius - 1), 0.0, 1.0);
        float glowAlpha = pow(1.0 - distNorm, 1.75) * 0.55 * vsInput.Extra.Alpha;
        if (glowAlpha > 0.01) return vec4(0.0, 0.0, 0.0, glowAlpha);
    }
    discard;
}

void main() {
    float id = vsInput.Extra.Type;

    if (id == TypeGameObject || id == TypeEffect) {
        FragColor = GetGameObject();
    } else if (id == TypeText) {
        FragColor = GetText();
    } else if (id == TypeBar) {
        FragColor = GetBar();
    } else {
        discard;
    }
}