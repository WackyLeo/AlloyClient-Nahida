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
uniform float Zoom;
uniform int RenderPass;

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
    if (uv.x < minUV.x || uv.x > maxUV.x || uv.y < minUV.y || uv.y > maxUV.y) {
        return false;
    }
    return true;
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

    int outlineWidth = 1;
    if (hitRadius <= outlineWidth) {
        return vec4(vsInput.Color.rgb, 1.0);
    }

    if (minDist <= float(maxRadius)) {
        float glowRange = float(maxRadius) - float(outlineWidth);
        float currentDist = minDist - float(outlineWidth);
        float distNormalized = clamp(currentDist / glowRange, 0.0, 1.0);
        float glowIntensity = 0.45;
        float glowFalloff = 1.75;
        float glowAlpha = pow(1.0 - distNormalized, glowFalloff) * glowIntensity;

        if (glowAlpha > 0.01) {
            return vec4(vsInput.Color.rgb, glowAlpha);
        }
    }

    discard;
}

void main() {
    vec4 outputColor;
    float id = vsInput.Extra.Type;

    if (id == TypeGameObject || id == TypeEffect) {
        outputColor = GetGameObject();
    } else if (id == TypeBar) {
        outputColor = vsInput.Color;
    } else {
        outputColor = vec4(0, 0, 0, 0);
    }

    outputColor.a *= vsInput.Extra.Alpha;
    if (outputColor.a == 0) {
        discard;
    }

    FragColor = outputColor;
}