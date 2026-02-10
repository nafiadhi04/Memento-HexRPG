shader_type canvas_item;

// Range 0.0 (Layar Bersih) sampai 1.0 (Layar Tertutup Hitam Full)
uniform float progress : hint_range(0.0, 1.0);

// Ukuran Hexagon (Semakin besar angka, semakin kecil hex-nya)
uniform float hex_size = 40.0; 

// Rasio aspek layar (agar hex tidak gepeng)
uniform vec2 screen_size = vec2(1280.0, 720.0); 

// Fungsi matematika untuk membuat bentuk Hexagon Flat Top
float hex_dist(vec2 p) {
    p = abs(p);
    // Flat Top Hexagon math
    float c = dot(p, normalize(vec2(1.0, 1.73))); 
    c = max(c, p.x);
    return c;
}

void fragment() {
    // Sesuaikan UV dengan rasio layar
    vec2 uv = UV * screen_size;
    
    // Logic Grid Hexagon (Flat Top)
    // Offset baris ganjil/genap
    vec2 r = vec2(1.0, 1.73) * hex_size;
    vec2 h = r * 0.5;
    
    vec2 a = mod(uv, r) - h;
    vec2 b = mod(uv - h, r) - h;
    
    vec2 gv;
    if (length(a) < length(b)) {
        gv = a;
    } else {
        gv = b;
    }
    
    // Hitung jarak pixel ke pusat hex terdekat
    float dist = hex_dist(gv);
    
    // --- ANIMASI ---
    // Kita buat hex menutup dari luar ke dalam (vignette style) atau acak
    // Di sini kita pakai simple threshold: Hex membesar bareng-bareng
    // Ditambah sedikit variasi berdasarkan posisi agar terlihat "techy"
    
    float wave = (UV.x + UV.y) * 0.5; // Gradasi diagonal
    float threshold = progress * (1.0 + 0.5) - (wave * 0.2); 
    
    // Normalisasi threshold agar tegas (Hard Edge)
    // hex_size * 0.5 adalah jari-jari maksimal hex
    float size_limit = threshold * (hex_size * 0.55); 
    
    if (dist < size_limit) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0); // Warna Hitam (Hexagon)
    } else {
        COLOR = vec4(0.0, 0.0, 0.0, 0.0); // Transparan
    }
}