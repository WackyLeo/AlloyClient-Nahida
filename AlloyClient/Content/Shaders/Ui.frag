#version 450 core

in VS_OUT {
    vec4 Position1;
    flat uint Color;
    flat uint Override;
    vec2 Info;
    vec2 UVCoords;
    vec4 Scissor;
    vec4 Extra1;
    vec4 Extra2;
    vec4 ColorTransform;
} inp;

out vec4 FragColor;

uniform sampler2D GameAtlasTexture;
uniform sampler2D UiAtlasTexture;
uniform sampler2D UiAtlasTextureLinear;
uniform sampler2D MinimapTexture;

uniform float PixelRange;
uniform vec2 TextTextureSize;
uniform sampler2D TextTexture;

uniform sampler2D TitleGraphicTexture;
uniform sampler2D TitleLogoTexture;
uniform float Time;

const float TextTypeNormal = 0.0;
const float TextTypeSmall = 1.0;

const float IdColor = 0.0;
const float IdGameAtlas = 1.0;
const float IdUiAtlas = 2.0;
const float IdUiAtlasLinear = 3.0;
const float IdUiSlice = 4.0;
const float IdText = 5.0;
const float IdTitleGraphic = 6.0;
const float IdTitleLogo = 7.0;
const float IdMinimap = 8.0;
const float IdEllipse = 9.0;
const float IdHpBar = 10.0;
const float IdSdfRect = 11.0;
const float IdDiamondLine = 12.0;
const float IdCutEdge = 13.0;

// Foward declare eff's

vec3 CalculateFireEffect(vec2 screenPos, float time);
vec3 CalculateRainbowEffect(vec2 screenPos, float time);
vec4 CalculateGlitchEffect(vec2 uv, vec2 screenPos, float time, float pxRange, float bodyAlpha, float totalBgAlpha, vec4 outlineColor);
vec3 CalculateElectricEffect(vec2 screenPos, float time, float bodyAlpha, float strokeAlpha);
vec3 CalculateVoidEffect(vec2 screenPos, float time, float bodyAlpha);

vec4 unpackColor(uint color) {
    return vec4(
    float(color & 0x000000FFu) / 255.0,
    float((color & 0x0000FF00u) >> 8u) / 255.0,
    float((color & 0x00FF0000u) >> 16u) / 255.0,
    float((color & 0xFF000000u) >> 24u) / 255.0
    );
}

float map(float value, float originalMin, float originalMax, float newMin, float newMax) {
    return (value - originalMin) / (originalMax - originalMin) * (newMax - newMin) + newMin;
}

float scale(float val, vec2 rect, float border, float borderTex) {
    if (val <= border)
    return map(val, 0, border, rect.x, rect.x + borderTex);
    if (val >= 1.0 - border)
    return map(val, 1.0 - border, 1, rect.y - borderTex, rect.y);
    return map(val, border, 1.0 - border, rect.x + borderTex, rect.y - borderTex);
}

vec4 slice() {
    vec2 uv;
    uv.x = scale(inp.UVCoords.x, inp.Extra1.xy, inp.Extra2.z, inp.Extra2.x);
    uv.y = scale(inp.UVCoords.y, inp.Extra1.zw, inp.Extra2.w, inp.Extra2.y);
    return texture(UiAtlasTexture, uv);
}

float median(float a, float b, float c) {
    return max(min(a, b), min(max(a, b), c));
}

float screenPxRange(vec2 uv) {
    vec2 unitRange = vec2(PixelRange, PixelRange) / TextTextureSize;
    vec2 screenSize = vec2(1.0, 1.0) / fwidth(uv);
    return max(0.5 * dot(unitRange, screenSize), 1.0);
}

vec2 SafeNormalize(vec2 v) {
    float vLength = length(v);

    vLength = (vLength > 0.0) ?
    1.0 / vLength : 0.0;

    return v * vLength;
}

float GetOpacityFromDistance(float signedDistance, vec2 Jdx, vec2 Jdy) {
    const float distanceLimit = sqrt(2.0f) / 2.0f;
    float thickness = 1.0f / (PixelRange / 2.0);

    vec2 gradientDistance = SafeNormalize(vec2(dFdx(signedDistance), dFdy(signedDistance)));
    vec2 gradient = vec2(gradientDistance.x * Jdx.x + gradientDistance.y * Jdy.x, gradientDistance.x * Jdx.y + gradientDistance.y * Jdy.y);
    float scaledDistanceLimit = min(thickness * distanceLimit * length(gradient), 0.5f);

    return smoothstep(-scaledDistanceLimit, scaledDistanceLimit, signedDistance);
}

vec4 RenderHpBar() {
    float w = inp.Extra1.x;
    float h = inp.Extra1.y;
    float p = inp.Extra1.z;

    float totalW = w + p * 2.0;
    float totalH = h + p * 2.0;
    float px = inp.UVCoords.x * totalW;
    float py = inp.UVCoords.y * totalH;

    float dx = max(0.0, max(p - px, px - (p + w)));
    float dy = max(0.0, max(p - py, py - (p + h)));
    float dist = max(dx, dy);

    if (dist == 0.0) return unpackColor(inp.Color);

    float outlineWidth = 1.0;
    if (dist <= outlineWidth) return vec4(0.0, 0.0, 0.0, 1.0);
    discard;
}

vec4 RenderSdfRect() {
    float w = inp.Extra1.x;
    float h = inp.Extra1.y;
    float cut = inp.Extra1.z;
    float shadowRadius = inp.Extra1.w;
    float shadowMaxAlpha = inp.Extra2.x;
    int cutsMask = int(inp.Extra2.y);
    float outlineSize = inp.Extra2.z;

    vec2 totalSize = vec2(w + shadowRadius * 2.0 + outlineSize * 2.0, h + shadowRadius * 2.0 + outlineSize * 2.0);
    vec2 pos = inp.UVCoords * totalSize;
    vec2 center = totalSize * 0.5;

    vec2 dir = pos - center;
    vec2 p = abs(dir);
    vec2 halfSize = vec2(w * 0.5, h * 0.5);

    vec2 d = p - halfSize;
    float dist = max(d.x, d.y);
    bool applyCut = false;

    if (dir.x <= 0.0 && dir.y <= 0.0 && (cutsMask & 1) != 0) applyCut = true;
    else if (dir.x >= 0.0 && dir.y <= 0.0 && (cutsMask & 2) != 0) applyCut = true;
    else if (dir.x <= 0.0 && dir.y >= 0.0 && (cutsMask & 4) != 0) applyCut = true;
    else if (dir.x >= 0.0 && dir.y >= 0.0 && (cutsMask & 8) != 0) applyCut = true;

    if (applyCut) {
        float cornerDist = (p.x + p.y - (halfSize.x + halfSize.y - cut)) * 0.70710678;
        dist = max(dist, cornerDist);
    }

    vec4 bgColor = unpackColor(inp.Color);
    vec4 outlineColor = unpackColor(inp.Override);

    float px = fwidth(dist);
    if (px == 0.0) px = 1.0;

    float shadowNorm = clamp((dist - outlineSize) / max(shadowRadius, 0.001), 0.0, 1.0);
    float shadowAlpha = shadowMaxAlpha * (1.0 - shadowNorm * shadowNorm);
    vec4 shadowColor = vec4(0.0, 0.0, 0.0, shadowAlpha);

    float outlineEdge = smoothstep(outlineSize - px, outlineSize + px, dist);
    vec4 bgOutColor = mix(outlineColor, shadowColor, outlineEdge);

    float bgEdge = smoothstep(-px, px, dist);
    vec4 finalColor = mix(bgColor, bgOutColor, bgEdge);

    if (finalColor.a <= 0.001) discard;
    return finalColor;
}

vec4 RenderText() {
    vec4 mtsdf = texture(TextTexture, inp.UVCoords);
    float dist = median(mtsdf.r, mtsdf.g, mtsdf.b) - 0.5;
    float pxRange = screenPxRange(inp.UVCoords);

    float bodyDist = dist * pxRange;
    float bodyAlpha = clamp(bodyDist + 0.5, 0.0, 1.0);

    float outlineThickness = clamp(inp.Extra1.x, 0.0, 4.0);
    float strokeAlpha = clamp(bodyDist + outlineThickness + 0.5, 0.0, 1.0);


    float shadowAlpha = smoothstep(0.0, 0.8, mtsdf.a) * inp.Extra1.z;
    float totalBgAlpha = max(strokeAlpha, shadowAlpha);

    vec4 outlineColor = unpackColor(inp.Override);
    vec4 textColor = unpackColor(inp.Color);

    int effectId = int(inp.Extra2.x);
    if (effectId == 1) {
        textColor.rgb = CalculateFireEffect(gl_FragCoord.xy, Time);
    }
    else if (effectId == 2) {
        textColor.rgb = CalculateRainbowEffect(gl_FragCoord.xy, Time);
    }
    else if (effectId == 3) {
        return CalculateGlitchEffect(inp.UVCoords, gl_FragCoord.xy, Time, pxRange, bodyAlpha, totalBgAlpha, outlineColor);
    }
    else if (effectId == 4) {
        textColor.rgb = CalculateElectricEffect(gl_FragCoord.xy, Time, bodyAlpha, strokeAlpha);
    }
    else if (effectId == 5) {
        textColor.rgb = CalculateVoidEffect(gl_FragCoord.xy, Time, bodyAlpha);
    }

    vec4 finalColor = mix(outlineColor, textColor, bodyAlpha);
    return vec4(finalColor.rgb, clamp(totalBgAlpha, 0.0, 1.0));
}

float samp(vec2 uv, vec2 dx, vec2 dy) {
    return textureGrad(GameAtlasTexture, uv, dx, dy).a;
}

vec4 RenderOutline() {
    vec2 uv = inp.UVCoords;
    vec2 dx = dFdx(uv);
    vec2 dy = dFdy(uv);
    vec4 color = textureGrad(GameAtlasTexture, uv, dx, dy);

    if (inp.UVCoords.y > inp.Extra1.x) {
        color.rgb -= 0.241 * (((inp.UVCoords.y - inp.Extra1.y) / inp.Extra1.z) - 0.4);
    }

    if (color.a > 0) {
        return color;
    }

    if (inp.Extra1.w == -1 && inp.Extra2.z == -1){
        discard;
    }

    vec2 spriteMin = vec2(inp.Extra2.x, inp.Extra1.y);
    vec2 spriteMax = vec2(inp.Extra2.x + inp.Extra2.y, inp.Extra1.y + inp.Extra1.z);

    vec4 outlineColor = unpackColor(inp.Override);
    float scale = min(4, inp.Extra2.w / 60.0);

    vec2 texSize = vec2(textureSize(GameAtlasTexture, 0));
    ivec2 currentTexel = ivec2(uv * texSize);

    float pxW = length(dx);
    float pxH = length(dy);
    vec2 invPx = vec2(1.0 / pxW, 1.0 / pxH);

    float outlineSize = floor(max(1, scale));
    float glowSize = max(6, 6.0 * scale);

    vec2 dirs[8] = vec2[](
    -dx - dy, -dy, dx - dy, dx,
    dx + dy, dy, -dx + dy, -dx
    );

    float outlineAlpha = 0.0;
    float nearestDist = 999.0;

    for (float i = 1; i <= glowSize && outlineAlpha == 0.0; i++) {
        for (int j = 0; j < 8; j++) {
            vec2 sampleUV = uv + dirs[j] * i;

            if (sampleUV.x < spriteMin.x || sampleUV.x > spriteMax.x ||
            sampleUV.y < spriteMin.y || sampleUV.y > spriteMax.y) {
                continue;
            }

            ivec2 neighborTexel = ivec2(sampleUV * texSize);
            if (neighborTexel == currentTexel) continue;
            if (texelFetch(GameAtlasTexture, neighborTexel, 0).a == 0) continue;

            vec2 nearestPoint = clamp(uv, vec2(neighborTexel) / texSize, vec2(neighborTexel + ivec2(1)) / texSize);
            vec2 distPx = abs(uv - nearestPoint) * invPx;

            if (max(distPx.x, distPx.y) <= outlineSize) {
                outlineAlpha = 1.0;
                break;
            }

            nearestDist = min(nearestDist, length(distPx));
        }
    }

    if (inp.Extra1.w != -1 && outlineAlpha > 0.0){
        return vec4(outlineColor.rgb, 1.0);
    }

    if (inp.Extra2.z != -1 && nearestDist < 999.0) {
        float normalized = nearestDist / glowSize;
        float glowAlpha = 0.8 * exp(-normalized * 4) * (1.0 - smoothstep(0.8, 1.0, normalized));
        if (glowAlpha > 0.0){
            return vec4(outlineColor.rgb, glowAlpha);
        }
    }

    discard;
}

vec4 RenderNoOutline(sampler2D tex) {
    return texture(tex, inp.UVCoords);
}

vec4 RenderMinimap() {
    vec2 coords = inp.UVCoords;
    if (coords.x < 0 || coords.x > 1 || coords.y < 0 || coords.y > 1) {
        return vec4(0, 0, 0, 1);
    }

    return texture(MinimapTexture, coords);
}

vec4 RenderEllipse() {
    float rx = inp.Extra1.x - inp.Extra1.z, ry = inp.Extra1.y - inp.Extra1.z;
    float x = inp.UVCoords.x, y = inp.UVCoords.y;

    float inner = x * x / (rx * rx) + y * y / (ry * ry);
    rx = inp.Extra1.x; ry = inp.Extra1.y;
    float outline = x * x / (rx * rx) + y * y / (ry * ry);
    if (x * x / (rx * rx) + y * y / (ry * ry) > 1)
    return vec4(0, 0, 0, 0);
    float color_val;

    if (inner > 1) {
        color_val = 1;
    } else {
        color_val = 0;
    }

    return mix(unpackColor(inp.Color), unpackColor(inp.Override), color_val);
}

vec4 RenderCutEdge() {
    float w = inp.Extra1.x;
    float h = inp.Extra1.y;
    float cx = inp.Extra1.z;
    float cy = inp.Extra1.w;
    int cutsMask = int(inp.Extra2.x);

    vec2 totalSize = vec2(w, h);
    vec2 p = inp.UVCoords * totalSize;
    vec2 center = totalSize * 0.5;

    vec2 dir = p - center;
    vec2 absDir = abs(dir);
    vec2 halfSize = totalSize * 0.5;

    vec2 d = absDir - halfSize;
    float dist = max(d.x, d.y);

    bool applyCut = false;
    if (dir.x <= 0.0 && dir.y <= 0.0 && (cutsMask & 1) != 0) applyCut = true;
    else if (dir.x >= 0.0 && dir.y <= 0.0 && (cutsMask & 2) != 0) applyCut = true;
    else if (dir.x <= 0.0 && dir.y >= 0.0 && (cutsMask & 4) != 0) applyCut = true;
    else if (dir.x >= 0.0 && dir.y >= 0.0 && (cutsMask & 8) != 0) applyCut = true;

    if (applyCut) {
        vec2 n = vec2(cy, cx);
        float len = length(n);
        if (len > 0.0) {
            n /= len;
            vec2 pointOnLine = vec2(halfSize.x - cx, halfSize.y);
            float cornerDist = dot(absDir - pointOnLine, n);
            dist = max(dist, cornerDist);
        }
    }

    vec4 bgColor = unpackColor(inp.Color);

    float px = fwidth(dist);
    if (px == 0.0) px = 1.0;
    
    float alpha = clamp(0.5 - (dist / px), 0.0, 1.0);

    if (alpha <= 0.001) discard;
    return vec4(bgColor.rgb, bgColor.a * alpha);
}

float sdDiamond(vec2 p, float s) {
    vec2 q = abs(p);
    
    // 0.707 is 1/sqrt(2) for exact distance
    return (q.x + q.y - s) * 0.70710678;
}

float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

vec4 RenderDiamondLine() {
    float lineLength = inp.Extra1.x;
    float lineThickness = inp.Extra1.y;
    float diamondSize = inp.Extra1.z;

    vec2 totalSize = vec2(lineLength + diamondSize * 2.0, max(lineThickness, diamondSize * 2.0));
    vec2 p = inp.UVCoords * totalSize;
    vec2 center = totalSize * 0.5;
    vec2 localP = p - center;

    float dBox = sdBox(localP, vec2(lineLength * 0.5, lineThickness * 0.5));
    float dLeft = sdDiamond(localP - vec2(-lineLength * 0.5, 0.0), diamondSize);
    float dRight = sdDiamond(localP - vec2(lineLength * 0.5, 0.0), diamondSize);
    float dist = min(dBox, min(dLeft, dRight));

    vec4 bgColor = unpackColor(inp.Color);
    float px = fwidth(dist);
    if (px == 0.0) px = 1.0;

    float alpha = smoothstep(px, -px, dist);

    if (alpha <= 0.001) discard;
    return vec4(bgColor.rgb, bgColor.a * alpha);
}

void main() {
    vec4 pixel = vec4(0);

    //TODO: replace pos1 with gl_FragCoord and send screen coords in scissor instead
    if (inp.Position1.x < inp.Scissor.x || inp.Position1.x > inp.Scissor.z || inp.Position1.y < inp.Scissor.w || inp.Position1.y > inp.Scissor.y) {
        discard;
    }

    vec4 color = unpackColor(inp.Color);
    float type = inp.Info.x;

    if (type == IdColor) {
        pixel = color;
    } else if (type == IdGameAtlas) {
        pixel = RenderOutline();
    } else if (type == IdUiAtlas) {
        pixel = RenderNoOutline(UiAtlasTexture);
    } else if (type == IdUiAtlasLinear) {
        pixel = RenderNoOutline(UiAtlasTextureLinear);// todo: msdfa sampling
    } else if (type == IdUiSlice) {
        pixel = slice();
    } else if (type == IdText) {
        pixel = RenderText();
    } else if (type == IdTitleGraphic) {
        pixel = RenderNoOutline(TitleGraphicTexture);
    } else if (type == IdTitleLogo) {
        pixel = RenderNoOutline(TitleLogoTexture);
    } else if (type == IdMinimap) {
        pixel = RenderMinimap();
    } else if (type == IdEllipse) {
        pixel = RenderEllipse();
    } else if (type == IdHpBar) {
        pixel = RenderHpBar();
    } else if (type == IdSdfRect) {
        pixel = RenderSdfRect();
    } else if (type == IdDiamondLine) {
        pixel = RenderDiamondLine();
    } else if (type == IdCutEdge) {
        pixel = RenderCutEdge();
    }

    if (color.a > 0 && type != IdColor && type != IdText && type != IdEllipse && type != IdSdfRect && type != IdDiamondLine && type != IdCutEdge) {
        pixel *= color;
    }

    vec4 add = floor(inp.ColorTransform / 1000.0);
    vec4 mult = inp.ColorTransform - add * 1000.0;

    pixel = clamp(pixel, vec4(0.0), vec4(1.0));

    pixel = mult * pixel;
    pixel += add / 255.0;

    pixel.a *= inp.Info.y;
    FragColor = pixel;
}

// Effects

vec3 CalculateFireEffect(vec2 screenPos, float time) {
    float n = sin(screenPos.x * 0.05 + time * 3.0) * 0.5 + 0.5;
    n += sin(screenPos.y * 0.08 - time * 5.0) * 0.5;
    n = clamp(n, 0.0, 1.0);

    vec3 red = vec3(1.0, 0.1, 0.0);
    vec3 orange = vec3(1.0, 0.6, 0.0);
    vec3 yellow = vec3(1.0, 0.9, 0.2);

    if (n < 0.5) return mix(red, orange, n * 2.0);
    return mix(orange, yellow, (n - 0.5) * 2.0);
}

vec3 CalculateRainbowEffect(vec2 screenPos, float time) {
    float x = screenPos.x * 0.02;

    return vec3(
    sin(time * 2.0 + x) * 0.5 + 0.5,
    sin(time * 2.0 + x + 2.0) * 0.5 + 0.5,
    sin(time * 2.0 + x + 4.0) * 0.5 + 0.5
    );
}

vec4 CalculateGlitchEffect(vec2 uv, vec2 screenPos, float time, float pxRange, float bodyAlpha, float totalBgAlpha, vec4 outlineColor) {
    float band = floor(screenPos.y / 4.0);

    float trigger = step(0.93, sin(time * 4.7 + band * 0.4) * sin(time * 9.1));
    float offset = trigger * (fract(sin(band * 127.1 + time * 3.3) * 43758.5) - 0.5) * 0.007;

    vec4 msR = texture(TextTexture, uv + vec2(offset * 2.8, 0.0));
    vec4 msB = texture(TextTexture, uv - vec2(offset * 2.8, 0.0));

    float distR = (median(msR.r, msR.g, msR.b) - 0.5) * pxRange;
    float distB = (median(msB.r, msB.g, msB.b) - 0.5) * pxRange;

    float aR = clamp(distR + 0.5, 0.0, 1.0);
    float aG = bodyAlpha;
    float aB = clamp(distB + 0.5, 0.0, 1.0);

    float flicker = 0.82 + 0.18 * sin(time * 55.0 + band * 1.3);
    float scanline = sin(screenPos.y * 1.8 + time * 1.5) * 0.06 + 0.94;

    vec4 result;
    result.r = mix(outlineColor.r, 1.0 * flicker * scanline, aR);
    result.g = mix(outlineColor.g, 0.97 * flicker * scanline, aG);
    result.b = mix(outlineColor.b, 1.0 * flicker * scanline, aB);
    result.a = clamp(totalBgAlpha, 0.0, 1.0);

    return result;
}

vec3 CalculateElectricEffect(vec2 screenPos, float time, float bodyAlpha, float strokeAlpha) {
    float ex = screenPos.x;
    float ey = screenPos.y;

    float arc  = sin(ex * 0.26 + time * 16.0) * sin(ey * 0.19 + time * 11.0);
    arc += sin(ex * 0.44 - time * 23.0) * sin(ey * 0.32 - time * 8.5) * 0.6;
    arc += sin((ex + ey) * 0.14 + time * 19.0) * 0.4;
    arc  = arc / 2.0 * 0.5 + 0.5;

    float bolt = smoothstep(0.70, 1.0, arc);

    vec3 dark= vec3(0.0, 0.25, 0.85);
    vec3 mid = vec3(0.15, 0.65, 1.0);
    vec3 bright = vec3(0.85, 1.0, 1.0);

    vec3 elec = mix(dark, mid, arc);
    elec = mix(elec, bright, bolt);

    float edge = (1.0 - bodyAlpha) * strokeAlpha;
    elec = mix(elec, bright, edge * bolt * 1.5);

    return elec;
}

vec3 CalculateVoidEffect(vec2 screenPos, float time, float bodyAlpha) {
    float vx = screenPos.x * 0.02;
    float vy = screenPos.y * 0.02;

    float n1 = sin(vx * 1.6 + time * 0.6) * sin(vy * 1.3 - time * 0.5);
    float n2 = sin(vx * 2.4 - time * 1.0) * sin(vy * 2.0 + time * 0.7);
    float n3 = sin((vx + vy) * 1.2 + time * 0.8) * 0.5;

    float n = (n1 + n2 + n3) / 3.0;
    n = n * 0.5 + 0.5;

    n = smoothstep(0.2, 0.85, n);
    vec3 c1 = vec3(0.12, 0.88, 1.00);
    vec3 c2 = vec3(0.82, 1.00, 1.00);
    vec3 c3 = vec3(1.00, 1.00, 1.00);

    vec3 voidColor;
    if (n < 0.5)
    voidColor = mix(c1, c2, (n - 0.33) * 3.0);
    else
    voidColor = mix(c2, c3, (n - 0.66) * 3.0);

    vec2 cell = floor(screenPos / 3.0);
    float rng = fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453);

    float isSpark = step(0.975, rng);
    float twinkle = isSpark * pow(
    sin(time * 6.0 + rng * 6.2831) * 0.5 + 0.5,
    6.0
    );

    vec3 sparkleColor = vec3(0.7, 1.0, 1.0);
    voidColor = mix(voidColor, sparkleColor, twinkle * bodyAlpha);
    return voidColor;
}