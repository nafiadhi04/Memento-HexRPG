using Godot;
using System;
using MementoTest.Core;

namespace MementoTest.UI
{
	public partial class SaveSlotUI : Button
	{
		[Signal] public delegate void SlotSelectedEventHandler(int slotIndex, bool isEmpty);
		[Export] public int SlotIndex = 1;

		private Label _slotNameLabel;
		private Label _playerNameLabel;
		private Label _classLabel;
		private Label _dateLabel;

		// 1. TAMBAHAN: Label Highscore
		private Label _highScoreLabel;

		public override void _Ready()
		{
			_slotNameLabel = GetNode<Label>("MarginContainer/VBoxContainer/SlotNameLabel");
			_playerNameLabel = GetNode<Label>("MarginContainer/VBoxContainer/PlayerNameLabel");
			_classLabel = GetNode<Label>("MarginContainer/VBoxContainer/ClassLabel");
			_dateLabel = GetNode<Label>("MarginContainer/VBoxContainer/DateLabel");

			// 2. Pastikan Node ini sudah dibuat di Scene Editor
			_highScoreLabel = GetNode<Label>("MarginContainer/VBoxContainer/HighScoreLabel");

			Pressed += OnBtnPressed;
			RefreshUI();
		}

		private void OnBtnPressed()
		{
			bool isEmpty = !GameManager.Instance.SaveExists(SlotIndex);
			EmitSignal(SignalName.SlotSelected, SlotIndex, isEmpty);
		}

		public void RefreshUI()
		{
			_slotNameLabel.Text = $"SLOT {SlotIndex}";

			if (GameManager.Instance.SaveExists(SlotIndex))
			{
				var data = ResourceLoader.Load<SaveData>(GameManager.Instance.GetSavePath(SlotIndex));

				_playerNameLabel.Text = data.PlayerName;

				// Konversi int kembali ke Enum untuk display (jika perlu)
				_classLabel.Text = $"Class: {(PlayerClassType)data.ClassTypeInt}";
				_dateLabel.Text = data.LastPlayedDate;

				// 3. TAMBAHAN: Logic Tampilan Highscore & Victory
				string status = data.IsVictory ? " [★ CLEARED]" : "";
				_highScoreLabel.Text = $"High Score: {data.HighScore}{status}";

				// Ubah warna jika tamat (opsional visual feedback)
				_highScoreLabel.Modulate = data.IsVictory ? Colors.Yellow : Colors.White;

				_playerNameLabel.Modulate = Colors.White;
			}
			else
			{
				_playerNameLabel.Text = "EMPTY";
				_classLabel.Text = "-";
				_dateLabel.Text = "";
				_highScoreLabel.Text = ""; // Kosongkan
				_playerNameLabel.Modulate = Colors.Gray;
			}
		}
	}
}