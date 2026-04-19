#version 460 core
out vec4 FragColor;
layout(location = 0) in vec2 vertUVs;
uniform sampler2D colourTexture;
uniform vec2 uResolution;
uniform vec2 uGameCenter;
uniform vec2 uGameHalfSize;

void main() {
    vec2 uv = vertUVs;
    if (abs((uv.x - uGameCenter.x) / uGameHalfSize.x) > 1.0 ||
    abs((uv.y - uGameCenter.y) / uGameHalfSize.y) > 1.0) {
        FragColor = vec4(0.0, 0.0, 0.0, 1.0); return;
    }

    vec2 norm = (uv - uGameCenter) / uGameHalfSize;
    float aspect = (uGameHalfSize.x * uResolution.x) / (uGameHalfSize.y * uResolution.y);
    norm.x *= aspect;

    float dist = length(norm) / aspect;
    float vignette = smoothstep(1.2, 0.75, dist);
    vignette = mix(0.75, 1.0, vignette);

    vec3 col = texture(colourTexture, uv).rgb;
    FragColor = vec4(col * vignette, 1.0);
}