using Godot;
using System;
using MementoTest.Core;

namespace MementoTest.Core
{
	[GlobalClass]
	public partial class SaveData : Resource
	{
		[Export] public string PlayerName { get; set; } = "Player";
		[Export] public string LastPlayedDate { get; set; } = "";

		// --- BAGIAN 1: JEMBATAN TIPE KELAS (Supaya tidak error ClassType) ---
		[Export] public int ClassTypeInt { get; set; } = 0;

		public PlayerClassType ClassType
		{
			get => (PlayerClassType)ClassTypeInt;
			set => ClassTypeInt = (int)value;
		}

		// --- BAGIAN 2: DATA PROGRESS SESI (Akan di-reset saat New Game+) ---
		[Export] public int CurrentHP { get; set; } = 100;
		[Export] public int CurrentAP { get; set; } = 3;
		[Export] public Vector2 PlayerPosition { get; set; } = Vector2.Zero;
		[Export] public bool IsVictory { get; set; } = false;
		[Export] public int EnemyHP { get; set; }
		[Export] public bool IsEnemyDead { get; set; }

		// [FIX] INI YANG HILANG SEBELUMNYA:
		[Export] public int CurrentScore { get; set; } = 0;

		// --- BAGIAN 3: DATA PERMANEN (Tidak di-reset saat New Game+) ---
		[Export] public int HighScore { get; set; } = 0;
		[Export] public int TotalKills { get; set; } = 0;
		[Export] public Godot.Collections.Array<string> UnlockedSkills { get; set; } = new();

		public SaveData() { }
	}
}