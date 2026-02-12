using Godot;
using System;

namespace MementoTest.Core
{
	public partial class GameCamera : Camera2D
	{
		[Export] public float ZoomSpeed = 0.1f;
		[Export] public float MinZoom = 0.5f; // Semakin kecil = semakin jauh (wide)
		[Export] public float MaxZoom = 2.0f; // Semakin besar = semakin dekat (close up)

		private float _shakeStrength = 0f;
		private float _shakeDuration = 0f;
		private Vector2 _originalOffset;
		private RandomNumberGenerator _rng = new RandomNumberGenerator();


		// Target Zoom (agar transisi zoom halus)
		private Vector2 _targetZoom;

		public override void _Ready()
		{
			// 1. Ambil zoom saat ini dari editor
			_targetZoom = Zoom;

			// 2. FIX: Langsung batasi (Clamp) nilai awal ini agar sesuai aturan
			// Jadi kalau Editor zoom = 1, tapi MinZoom = 1.5, game akan memaksa start di 1.5
			_targetZoom = _targetZoom.Clamp(
				new Vector2(MinZoom, MinZoom),
				new Vector2(MaxZoom, MaxZoom)
			);

			// 3. (Opsional) Jika kamu ingin MEMAKSA game mulai dalam keadaan Zoom In Penuh (MaxZoom):
			// Hapus tanda komentar di bawah ini:
			_targetZoom = new Vector2(MaxZoom, MaxZoom);

			// 4. Terapkan nilai yang sudah divalidasi ke kamera
			Zoom = _targetZoom;
		}

		public override void _Process(double delta)
		{
			// Zoom smoothing
			Zoom = Zoom.Lerp(_targetZoom, (float)delta * 10.0f);

			// ===== SHAKE LOGIC =====
			if (_shakeDuration > 0)
			{
				_shakeDuration -= (float)delta;

				float offsetX = _rng.RandfRange(-_shakeStrength, _shakeStrength);
				float offsetY = _rng.RandfRange(-_shakeStrength, _shakeStrength);

				Offset = new Vector2(offsetX, offsetY);
			}
			else
			{
				Offset = Vector2.Zero;
			}
		}


		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.IsPressed())
				{
					if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
					{
						// Zoom In (Mendekat)
						_targetZoom += new Vector2(ZoomSpeed, ZoomSpeed);
					}
					else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
					{
						// Zoom Out (Menjauh)
						_targetZoom -= new Vector2(ZoomSpeed, ZoomSpeed);
					}

					// Batasi zoom (Clamp) agar tidak kebablasan
					_targetZoom = _targetZoom.Clamp(
						new Vector2(MinZoom, MinZoom),
						new Vector2(MaxZoom, MaxZoom)
					);
				}
			}
		}

		public void Shake(float strength, float duration)
		{
			_shakeStrength = strength;
			_shakeDuration = duration;
			_originalOffset = Offset;
			_rng.Randomize();
		}

	}
}