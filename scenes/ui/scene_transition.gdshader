shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0);
uniform float hex_size = 40.0;
uniform vec2 screen_size = vec2(1280.0, 720.0);

// Fungsi jarak hex FLAT TOP
float hex_dist(vec2 p)
{
    p = abs(p);
    return max(p.x * 0.8660254 + p.y * 0.5, p.y);
}

void fragment()
{
    vec2 uv = UV * screen_size;

    // ===== FLAT TOP GRID =====
    float w = hex_size * 2.0;
    float h = sqrt(3.0) * hex_size;

    vec2 grid = vec2(w * 0.75, h);

    vec2 a = mod(uv, grid) - grid * 0.5;
    vec2 b = mod(uv - vec2(w * 0.375, h * 0.5), grid) - grid * 0.5;

    vec2 gv = (length(a) < length(b)) ? a : b;

    float dist = hex_dist(gv);

    // ===== Animasi =====
    float wave = (UV.x + UV.y) * 0.5;
    float threshold = progress - wave * 0.2;

    float size_limit = threshold * hex_size;

    if (dist < size_limit)
        COLOR = vec4(0.0, 0.0, 0.0, 1.0);
    else
        COLOR = vec4(0.0, 0.0, 0.0, 0.0);
}
