using Godot;
using System;

namespace MementoTest.Core
{
	// Inherit Resource agar mudah disimpan oleh Godot
	public partial class SaveData : Resource
	{
		[Export] public string PlayerName { get; set; } = "Player";
		[Export] public PlayerClassType ClassType { get; set; } = PlayerClassType.Warrior;

		// Kita simpan tanggal update agar tahu mana save terbaru
		[Export] public string LastPlayedDate { get; set; } = "";

		// Bisa tambah stats lain nanti (Level, XP, CurrentStage)
		[Export] public int Level { get; set; } = 1;
	}
}