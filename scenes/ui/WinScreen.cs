using Godot;
using System;
using MementoTest.Core;

namespace MementoTest.UI
{
    public partial class WinScreen : Control
    {
        [Export] public string MainMenuScenePath = "res://scenes/ui/main_menu.tscn";

        // --- BAGIAN INI AGAR BISA DRAG & DROP ---
        [Export] private Label _scoreLabel;
        [Export] private Label _highScoreLabel;     // Drag node HighScoreLabel kesini nanti
          // Drag node MainMenuButton kesini nanti
        // ----------------------------------------

        public override void _Ready()
        {
            Visible = false;
            // Cek apakah node sudah dimasukkan lewat Inspector
            if (_scoreLabel == null || _highScoreLabel == null)
            {
                GD.PrintErr("ERROR: Node UI belum di-assign di Inspector WinScreen! Silakan Drag & Drop node-nya.");
                return;
            }

            ProcessVictoryData();

            
        }

        public void ShowVictory()
        {
            Visible = true;
            ProcessVictoryData();
        }


        private void ProcessVictoryData()
        {
            // Ambil index slot yang sedang dimainkan
            int currentSlot = GameManager.Instance.ActiveSlotIndex;

            if (GameManager.Instance.SaveExists(currentSlot))
            {
                SaveData data = ResourceLoader.Load<SaveData>(GameManager.Instance.GetSavePath(currentSlot));

                // Cek Highscore Logic
                if (data.CurrentScore > data.HighScore)
                {
                    data.HighScore = data.CurrentScore;
                    _highScoreLabel.Text = $"NEW HIGH SCORE: {data.HighScore}";
                    _highScoreLabel.Modulate = Colors.Yellow;
                }
                else
                {
                    _highScoreLabel.Text = $"High Score: {data.HighScore}";
                }

                _scoreLabel.Text = $"Your Score: {data.CurrentScore}";


            }
        }
    }
}