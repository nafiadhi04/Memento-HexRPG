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