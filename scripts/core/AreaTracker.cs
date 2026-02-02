using Godot;
using System;
using MementoTest.Entities; // Agar kenal EnemyController

public partial class AreaTracker : Node2D
{
	// Signal untuk memberitahu LevelManager kalau area ini sudah bersih
	[Signal] public delegate void AreaClearedEventHandler();

	private int _enemyCount = 0;

	public override void _Ready()
	{
		// 1. Hitung jumlah musuh saat game mulai
		foreach (Node child in GetChildren())
		{
			// Pastikan kita hanya menghitung script EnemyController (abaikan dekorasi lain)
			// Ganti 'EnemyController' dengan nama class script musuhmu jika berbeda
			if (child is EnemyController)
			{
				_enemyCount++;

				// 2. Pasang pendengar: Kalau musuh ini dihapus dari game (Mati), panggil fungsi OnEnemyDied
				child.TreeExited += OnEnemyDied;
			}
		}

		GD.Print($"[TRACKER] {Name} mendeteksi {_enemyCount} musuh.");
	}

	private void OnEnemyDied()
	{
		_enemyCount--;

		// Cek apakah musuh sudah habis?
		if (_enemyCount <= 0)
		{
			GD.Print($">>> {Name} BERSIH! Mengirim sinyal buka gerbang...");
			EmitSignal(SignalName.AreaCleared);

			// Matikan script ini agar tidak memproses lagi
			SetProcess(false);
		}
	}
}