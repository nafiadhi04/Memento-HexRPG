using Godot;
using System;

namespace MementoTest.Core
{
	public partial class GameManager : Node
	{
		public static GameManager Instance { get; private set; }

		// Data yang sedang dimainkan saat ini
		public SaveData CurrentSaveData { get; private set; }

		// Slot yang sedang aktif (1, 2, atau 3)
		public int CurrentSlotIndex { get; private set; } = 1;

		private const string SAVE_DIR = "user://saves/";

		public override void _Ready()
		{
			Instance = this;
			EnsureSaveDirectory();
		}

		private void EnsureSaveDirectory()
		{
			if (!DirAccess.DirExistsAbsolute(SAVE_DIR))
			{
				DirAccess.MakeDirAbsolute(SAVE_DIR);
			}
		}

		// --- FUNGSI UTAMA SAVE/LOAD ---

		public string GetSavePath(int slotIndex)
		{
			return $"{SAVE_DIR}save_slot_{slotIndex}.tres";
		}

		public bool SaveExists(int slotIndex)
		{
			return FileAccess.FileExists(GetSavePath(slotIndex));
		}

		// Dipanggil saat New Game -> Create Character
		public void CreateNewSave(int slotIndex, string name, PlayerClassType classType)
		{
			CurrentSlotIndex = slotIndex;
			CurrentSaveData = new SaveData();

			CurrentSaveData.PlayerName = name;
			CurrentSaveData.ClassType = classType;
			CurrentSaveData.LastPlayedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

			// --- TAMBAHAN: Set HP/AP Awal berdasarkan Config ---
			if (ConfigManager.Instance != null)
			{
				CurrentSaveData.CurrentHP = ConfigManager.Instance.GetClassHP(classType);
				CurrentSaveData.CurrentAP = ConfigManager.Instance.GetClassAP(classType);
			}
			else
			{
				// Fallback jika ConfigManager belum siap
				CurrentSaveData.CurrentHP = 100;
				CurrentSaveData.CurrentAP = 5;
			}

			SaveGame(); // Tulis ke disk
		}

		// Dipanggil saat Continue -> Pilih Slot
		public void LoadGame(int slotIndex)
		{
			if (SaveExists(slotIndex))
			{
				CurrentSlotIndex = slotIndex;
				string path = GetSavePath(slotIndex);
				CurrentSaveData = ResourceLoader.Load<SaveData>(path);
				GD.Print($"[MANAGER] Save Slot {slotIndex} Loaded: {CurrentSaveData.PlayerName} ({CurrentSaveData.ClassType})");
			}
			else
			{
				GD.PrintErr($"[MANAGER] Save Slot {slotIndex} tidak ditemukan!");
			}
		}

		// Panggil fungsi ini dari PauseMenu saat tombol "Save & Quit" ditekan
		public void SaveGameplayState()
		{
			// 1. Cari Player
			var player = GetTree().GetFirstNodeInGroup("Player") as MementoTest.Entities.PlayerController;

			// 2. Cari Enemy (Pastikan Enemy kamu masuk dalam Group "Enemy")
			// Jika belum punya script EnemyController, sesuaikan tipe datanya nanti
			var enemy = GetTree().GetFirstNodeInGroup("Enemy");

			if (CurrentSaveData != null)
			{
				// --- SIMPAN DATA PLAYER ---
				if (player != null)
				{
					CurrentSaveData.CurrentHP = player.CurrentHP;
					CurrentSaveData.CurrentAP = player.CurrentAP;
					CurrentSaveData.PlayerPosition = player.GlobalPosition; // Simpan Posisi!
				}

				// --- SIMPAN DATA ENEMY ---
				if (enemy != null)
				{
					// Ambil nilai CurrentHP sebagai Variant
					Variant enemyHP = enemy.Get("CurrentHP");

					// Cek apakah tipe datanya benar (Int di Godot = 64-bit integer)
					if (enemyHP.VariantType == Variant.Type.Int)
					{
						// [PERBAIKAN] Gunakan Casting (int) atau .As<int>()
						CurrentSaveData.EnemyHP = enemyHP.As<int>();

						CurrentSaveData.IsEnemyDead = (CurrentSaveData.EnemyHP <= 0);
					}
				}
				else
				{
					// Jika tidak ada musuh (misal sudah mati dan hilang dari scene)
					CurrentSaveData.IsEnemyDead = true;
					CurrentSaveData.EnemyHP = 0;
				}

				// Simpan ke disk
				SaveGame();
				GD.Print($"[MANAGER] Saved! Pos: {CurrentSaveData.PlayerPosition}, EnemyHP: {CurrentSaveData.EnemyHP}");
			}
		}

		// Hapus file save dari disk
		public void DeleteSave(int slotIndex)
		{
			string path = GetSavePath(slotIndex);

			// Cek apakah file ada sebelum dihapus
			if (FileAccess.FileExists(path))
			{
				// DirAccess.RemoveAbsolute adalah cara Godot menghapus file
				Error err = DirAccess.RemoveAbsolute(path);

				if (err == Error.Ok)
				{
					GD.Print($"[MANAGER] Save Slot {slotIndex} DELETED.");
					// Reset CurrentSaveData jika yang dihapus adalah slot yang sedang aktif
					if (CurrentSlotIndex == slotIndex)
					{
						CurrentSaveData = null;
					}
				}
				else
				{
					GD.PrintErr($"[MANAGER] Gagal menghapus slot {slotIndex}: {err}");
				}
			}
		}

		// Simpan data terkini ke disk
		public void SaveGame()
		{
			if (CurrentSaveData == null) return;

			CurrentSaveData.LastPlayedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
			string path = GetSavePath(CurrentSlotIndex);

			Error err = ResourceSaver.Save(CurrentSaveData, path);
			if (err == Error.Ok)
			{
				GD.Print($"[MANAGER] Game Saved to Slot {CurrentSlotIndex}");
			}
			else
			{
				GD.PrintErr($"[MANAGER] Gagal menyimpan game: {err}");
			}
		}
	}
}