using Godot;
using System;
using MementoTest.Entities; // Agar kenal AreaTracker
using MementoTest.Core;    // Agar kenal LockedArea
public partial class LevelManager : Node2D
{
	[Export] public LockedArea FogArea2; // Kabut yang menutupi Area 2
	[Export] public LockedArea FogArea3;
	[Export] public LockedArea FogArea4;
	[Export] public LockedArea FogArea5;

	[Export] public AreaTracker Area1Tracker; // Musuh di Area 1
	[Export] public AreaTracker Area2Tracker;
	[Export] public AreaTracker Area3Tracker;
	[Export] public AreaTracker Area4Tracker;
	[Export] public AreaTracker Area5Tracker; // Boss

	public override void _Ready()
	{
		ConnectSignals();
	}

	private void ConnectSignals()
	{
		// LOGIKA: Jika Area 1 Bersih -> Buka Fog Area 2
		if (Area1Tracker != null)
			Area1Tracker.AreaCleared += () => UnlockFog(FogArea2);

		// Jika Area 2 Bersih -> Buka Fog Area 3
		if (Area2Tracker != null)
			Area2Tracker.AreaCleared += () => UnlockFog(FogArea3);

		// Jika Area 3 Bersih -> Buka Fog Area 4
		if (Area3Tracker != null)
			Area3Tracker.AreaCleared += () => UnlockFog(FogArea4);

		// Jika Area 4 Bersih -> Buka Fog Area 5 (Final Boss)
		if (Area4Tracker != null)
			Area4Tracker.AreaCleared += () => UnlockFog(FogArea5);

		// Jika Area 5 (Boss) Bersih -> MENANG
		if (Area5Tracker != null)
			Area5Tracker.AreaCleared += OnLevelVictory;
	}

	private void UnlockFog(LockedArea fog)
	{
		if (fog != null)
		{
			GD.Print("[LEVEL MANAGER] Membuka Area Baru!");
			fog.Unlock(); // Memanggil fungsi Unlock di script LockedArea.cs
		}
	}

	private void OnLevelVictory()
	{
		GD.Print(">>> MISSION COMPLETED! SEMUA MUSUH KALAH! <<<");
		// Di sini nanti kita panggil UI "YOU WIN"
	}
}