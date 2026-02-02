using Godot;
using System;

public partial class LockedArea : Node2D
{
	[ExportCategory("Fog Settings")]
	[Export] public Vector2 AreaSize = new Vector2(400, 400);
	[Export(PropertyHint.Range, "1.0, 100.0")]
	public float DensityFactor = 25.0f;
	[Export] public float ScaleMin = 3.0f;
	[Export] public float ScaleMax = 5.0f;

	private GpuParticles2D _fogParticles;

	public override void _Ready()
	{
		if (HasNode("FogParticles"))
		{
			_fogParticles = GetNode<GpuParticles2D>("FogParticles");
			SetupFogVisuals();
		}
		else
		{
			GD.PrintErr($"[LOCKED AREA] Error: Node 'FogParticles' tidak ditemukan di {Name}!");
		}
	}

	private void SetupFogVisuals()
	{
		if (_fogParticles == null) return;

		// A. SETUP EMISSION SHAPE
		var material = _fogParticles.ProcessMaterial as ParticleProcessMaterial;
		if (material != null)
		{
			material.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
			material.EmissionBoxExtents = new Vector3(AreaSize.X / 2, AreaSize.Y / 2, 1);
			material.ScaleMin = ScaleMin;
			material.ScaleMax = ScaleMax;
		}

		// B. AUTO DENSITY
		float totalArea = AreaSize.X * AreaSize.Y;
		int calculatedAmount = (int)(totalArea / DensityFactor);
		_fogParticles.Amount = Mathf.Clamp(calculatedAmount, 500, 25000);

		// C. PREPROCESS
		_fogParticles.Lifetime = 2.0f;
		_fogParticles.Preprocess = 2.0f;

		// --- D. FIX CULLING (MASALAH HILANG SAAT ZOOM) ---
		// Kita perbesar kotak "Visibility Rect" agar Godot tahu partikel ini luas.
		// Rect2 butuh (Posisi X, Posisi Y, Lebar, Tinggi)
		// Posisi dimulai dari pojok kiri atas relatif terhadap titik tengah (0,0)

		float margin = 100.0f; // Tambah margin biar aman kalau partikel terbang keluar area
		float width = AreaSize.X + margin;
		float height = AreaSize.Y + margin;

		// Geser posisi X dan Y ke kiri-atas (-setengah lebar, -setengah tinggi)
		var rectPosition = new Vector2(-width / 2, -height / 2);
		var rectSize = new Vector2(width, height);

		_fogParticles.VisibilityRect = new Rect2(rectPosition, rectSize);
		// -----------------------------------------------------

		GD.Print($"[FOG SETUP] {Name} | VisRect Updated: {_fogParticles.VisibilityRect}");
	}

	public void Unlock()
	{
		GD.Print($"[LOCKED AREA] {Name} Unlocked!");
		QueueFree();
	}
}