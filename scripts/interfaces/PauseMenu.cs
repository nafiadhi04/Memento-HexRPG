using Godot;
using MementoTest.Core;
using System;

public partial class PauseMenu : CanvasLayer
{
	// Menggunakan Export agar kita bisa drag-and-drop di Inspector
	[ExportGroup("Menu Buttons")]
	[Export] public Button ResumeBtn;
	[Export] public Button RestartBtn;
	[Export] public Button MainMenuBtn;
	[Export] public Button SaveQuitBtn;

	public override void _Ready()
	{
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;

		// Sambungkan sinyal secara otomatis jika tombol sudah di-set
		if (ResumeBtn != null) ResumeBtn.Pressed += OnResumePressed;
		if (RestartBtn != null) RestartBtn.Pressed += OnRestartPressed;
		if (MainMenuBtn != null) MainMenuBtn.Pressed += OnMainMenuPressed;
		if (SaveQuitBtn != null) SaveQuitBtn.Pressed += OnSaveQuitPressed;

		GD.Print("[PAUSE] Menu Ready dengan sistem Export.");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape)
		{
			TogglePause();
		}
	}

	public void TogglePause()
	{
		bool isPaused = !GetTree().Paused;
		GetTree().Paused = isPaused;
		Visible = isPaused;

		if (isPaused)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			// Fokus ke tombol Resume jika ada
			ResumeBtn?.GrabFocus();
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Hidden;
		}
	}

	private void OnResumePressed() => TogglePause();

	private void OnRestartPressed()
	{
		// Restart tidak perlu save, karena pemain ingin mengulang
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	// --- UPDATED: MAIN MENU DENGAN AUTO SAVE ---
	private void OnMainMenuPressed()
	{
		GD.Print("[PAUSE] Returning to Main Menu (Auto-Saving)...");

		// 1. Simpan Game Dulu
		if (GameManager.Instance != null)
		{
			GameManager.Instance.SaveGameplayState();
		}

		// 2. Unpause game (Penting agar scene berikutnya tidak diam)
		GetTree().Paused = false;

		// 3. Pindah Scene
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}

	private void OnSaveQuitPressed()
	{
		GD.Print("[PAUSE] Exiting Game (Auto-Saving)...");

		// 1. Simpan Game
		if (GameManager.Instance != null)
		{
			GameManager.Instance.SaveGameplayState();
		}

		// 2. Quit Aplikasi (Desktop)
		GetTree().Quit();
	}
}