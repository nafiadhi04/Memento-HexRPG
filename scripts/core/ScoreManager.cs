using Godot;
using System;

namespace MementoTest.Core
{
    public partial class ScoreManager : Node
    {
        // Singleton Instance
        public static ScoreManager Instance { get; private set; }

        // Variables
        public int CurrentScore { get; private set; } = 0;
        public int CurrentCombo { get; private set; } = 0;
        public int MaxCombo { get; private set; } = 0;

        // Signals untuk update UI
        [Signal] public delegate void ScoreUpdatedEventHandler(int newScore);
        [Signal] public delegate void ComboUpdatedEventHandler(int newCombo);

        public override void _Ready()
        {
            Instance = this;
        }

        // Panggil ini kalau Player mengetik BENAR
        public void AddScore(int baseScore)
        {
            CurrentCombo++;
            if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;

            // Rumus Skor: Base + (Combo * Bonus)
            // Contoh: Skor dasar 100. Combo 5. Total = 100 + (5 * 10) = 150.
            int bonus = CurrentCombo * 10; 
            int totalGain = baseScore + bonus;

            CurrentScore += totalGain;

            GD.Print($"[SCORE] +{totalGain} (Combo: {CurrentCombo})");

            // Kabari UI
            EmitSignal(SignalName.ScoreUpdated, CurrentScore);
            EmitSignal(SignalName.ComboUpdated, CurrentCombo);
        }

        // Panggil ini kalau Player TYPO atau KENA PUKUL
        public void ResetCombo()
        {
            if (CurrentCombo > 0)
            {
                GD.Print("[SCORE] COMBO BROKEN!");
                CurrentCombo = 0;
                EmitSignal(SignalName.ComboUpdated, 0);
            }
        }
        
        public void ResetAll()
        {
            CurrentScore = 0;
            CurrentCombo = 0;
            MaxCombo = 0;
        }
    }
}