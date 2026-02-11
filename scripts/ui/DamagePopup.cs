using Godot;
using System;

namespace MementoTest.UI
{
	// [PENTING] Ubah inherit dari Node2D menjadi Label
	public partial class DamagePopup : Label
	{
		// Tidak perlu variabel [Export] Label lagi karena script ini ADALAH Labelnya

		public void Setup(int damageAmount, string textLabel = "")
		{
			// Atur Teks langsung ke diri sendiri
			if (!string.IsNullOrEmpty(textLabel))
			{
				Text = textLabel;       // "MISS", "BLOCK"
				Modulate = Colors.Yellow;
			}
			else
			{
				Text = damageAmount.ToString();
				Modulate = Colors.White;
			}

			// Atur pivot agar muncul pas di tengah (Opsional, agar rapi)
			// PivotOffset = Size / 2; 

			Animate();
		}

		private void Animate()
		{
			var tween = CreateTween();

			// Animasi Gerak ke Atas (Position works for Label too)
			tween.TweenProperty(this, "position", Position + new Vector2(0, -50), 0.5f)
				 .SetTrans(Tween.TransitionType.Circ)
				 .SetEase(Tween.EaseType.Out);

			// Animasi Fade Out
			tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.5f);

			// Hapus saat selesai
			tween.TweenCallback(Callable.From(QueueFree));
		}
	}
}