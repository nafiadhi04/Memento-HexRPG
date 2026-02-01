using Godot;
using System;

namespace MementoTest.UI
{
	public partial class DamagePopup : Label
	{
		public override void _Ready()
		{
			// PENTING: Agar text tidak ikut gerak kalau Karakter jalan.
			// Text akan diam di posisi dunia tempat dia muncul.
			TopLevel = true;
		}

		public void SetupAndAnimate(int amount, Vector2 startPos, Color color)
		{
			Text = amount.ToString();
			GlobalPosition = startPos;

			// Set warna text (modulate)
			Modulate = color;

			// --- ANIMASI ---
			// 1. Gerakan melayang ke atas acak sedikit ke kiri/kanan
			// Supaya kalau damage numpuk tidak saling menutupi
			RandomNumberGenerator rng = new RandomNumberGenerator();
			float xOffset = rng.RandfRange(-20, 20);
			Vector2 endPos = startPos + new Vector2(xOffset, -50); // Naik 50px

			Tween tween = CreateTween();
			tween.SetParallel(true); // Jalankan semua animasi bersamaan

			// Move Up
			tween.TweenProperty(this, "global_position", endPos, 0.7f)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

			// Scale Up & Down (Efek "Pop")
			// Mulai dari kecil (0.5) ke normal (1.0) biar kerasa "nimbul"
			Scale = new Vector2(0.5f, 0.5f);
			tween.TweenProperty(this, "scale", Vector2.One, 0.3f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

			// Fade Out (Menghilang) di akhir
			tween.TweenProperty(this, "modulate:a", 0f, 0.7f)
				.SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);

			// Hancurkan diri sendiri setelah selesai
			tween.Chain().TweenCallback(Callable.From(QueueFree));
		}
	}
}