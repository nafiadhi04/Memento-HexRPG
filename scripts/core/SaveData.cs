using Godot;
using System;
using MementoTest.Core;

namespace MementoTest.Core
{
	[GlobalClass]
	public partial class SaveData : Resource
	{
		[Export] public string PlayerName { get; set; } = "Player";
		[Export] public PlayerClassType ClassType { get; set; } = PlayerClassType.Warrior;
		[Export] public string LastPlayedDate { get; set; } = "";

		// --- STATS PLAYER ---
		[Export] public int CurrentHP { get; set; }
		[Export] public int CurrentAP { get; set; }

		// --- TAMBAHAN BARU: POSISI ---
		[Export] public Vector2 PlayerPosition { get; set; }

		// --- TAMBAHAN BARU: ENEMY (Sederhana 1v1) ---
		// Jika musuh banyak, kita butuh Array/Dictionary. Untuk sekarang 1 musuh dulu.
		[Export] public int EnemyHP { get; set; }
		[Export] public bool IsEnemyDead { get; set; }

		public SaveData() { }
	}
}