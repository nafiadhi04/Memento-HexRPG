using Godot;
using MementoTest.Entities;
using System;
using static MementoTest.Core.SaveData;

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

		// Referensi ke Scene Main Menu (Sesuaikan path-nya!)
		private string _mainMenuPath = "res://scenes/ui/main_menu.tscn";
		// Referensi ke UI Win Screen (Nanti kita buat)
		private PackedScene _winScreenScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/win_screen.tscn");
		public int ActiveSlotIndex { get; set; } = 1;
		[Export] public PackedScene WinScreenPrefab;

		private bool _isVictoryTriggered = false;
		private bool _gameStarted = false;



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
		public void MarkGameStarted()
		{
			_gameStarted = true;
			_isVictoryTriggered = false; // reset safety
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
			ActiveSlotIndex = slotIndex;
			CurrentSlotIndex = slotIndex;

			CurrentSaveData = new SaveData
			{
				PlayerName = name,
				ClassType = classType,
				LastPlayedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
				IsVictory = false
			};

			// --- Set HP/AP Awal berdasarkan Config ---
			if (ConfigManager.Instance != null)
			{
				CurrentSaveData.CurrentHP = ConfigManager.Instance.GetClassHP(classType);
				CurrentSaveData.CurrentAP = ConfigManager.Instance.GetClassAP(classType);
			}
			else
			{
				CurrentSaveData.CurrentHP = 100;
				CurrentSaveData.CurrentAP = 5;
			}

			SaveGame();
		}

		public static class AudioHooks
		{
			public static void Trigger(AudioEventType type)
			{
				GD.Print($"[AUDIO HOOK] {type}");
				// Nanti bisa diganti:
				// AudioManager.Instance.Play(type);
			}
		}

		// Dipanggil saat Continue -> Pilih Slot
		public void LoadGame(int slotIndex)
		{
			ActiveSlotIndex = slotIndex;
			GetTree().Paused = false;
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
			var player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;

			if (CurrentSaveData != null)
			{
				// ===== PLAYER =====
				if (player != null)
				{
					CurrentSaveData.CurrentHP = player.CurrentHP;
					CurrentSaveData.CurrentAP = player.CurrentAP;
					CurrentSaveData.PlayerPosition = player.GlobalPosition;
				}

				// ===== ENEMIES =====
				var enemies = GetTree().GetNodesInGroup("Enemy");
				CurrentSaveData.Enemies.Clear();

				foreach (var node in enemies)
				{
					if (node is EnemyController enemyCtrl)
					{
						EnemySaveData data = new EnemySaveData
						{
							EnemyID = enemyCtrl.Name,
							Position = enemyCtrl.GlobalPosition,
							HP = enemyCtrl.CurrentHP,
							IsDead = enemyCtrl.CurrentHP <= 0
						};

						CurrentSaveData.Enemies.Add(data);
					}
				}

				SaveGame();
				GD.Print("[MANAGER] Game Saved!");
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
		public async void CheckWinCondition()
		{
			if (!_gameStarted) return; // 🔥 penting!
			if (_isVictoryTriggered) return;

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			var enemies = GetTree().GetNodesInGroup("Enemy");

			if (enemies.Count == 0)
			{
				_isVictoryTriggered = true;
				HandleVictory();
			}
		}





		private async void HandleVictory()
		{
			if (!_isVictoryTriggered)
				return;

			GD.Print("=== VICTORY HANDLER START ===");

			// 🔒 Freeze game
			GetTree().Paused = true;

			// =========================
			// 1. Update Score
			// =========================
			if (ScoreManager.Instance != null && CurrentSaveData != null)
			{
				int finalScore = ScoreManager.Instance.CurrentScore;

				if (finalScore > CurrentSaveData.HighScore)
				{
					CurrentSaveData.HighScore = finalScore;
				}

				CurrentSaveData.IsVictory = true;   // 🔥 PINDAH KE SINI
				CurrentSaveData.IsEnemyDead = false;
				CurrentSaveData.EnemyHP = 0;
			}
			// =========================
			// 2. Save Data
			// =========================
			SaveGame();

			// =========================
			// 3. Show WinScreen (Toggle Visible)
			// =========================
			var winScreen = GetTree().GetFirstNodeInGroup("WinScreen")
				as MementoTest.UI.WinScreen;

			if (winScreen != null)
			{
				winScreen.ShowVictory();
			}
			else
			{
				GD.PrintErr("WinScreen node tidak ditemukan di scene!");
				return;
			}

			// =========================
			// 4. Optional Auto Return
			// =========================
			await ToSignal(GetTree().CreateTimer(5.0f, true), "timeout");

			GetTree().Paused = false;

			if (SceneTransition.Instance != null)
			{
				SceneTransition.Instance.ChangeScene(_mainMenuPath);
			}
			else
			{
				GetTree().ChangeSceneToFile(_mainMenuPath);
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

		// Di dalam GameManager.cs

		public void AddKillCount()
		{
			if (CurrentSaveData != null)
			{
				CurrentSaveData.TotalKills++;
				GD.Print($"[GAME MANAGER] Kill Added! Total: {CurrentSaveData.TotalKills}");

				// Opsional: Langsung save progress kill agar aman jika game crash
				// SaveGame(); 
			}
			else
			{
				GD.PrintErr("[GAME MANAGER] Cannot add kill count: No active SaveData!");
			}
		}
	}
}