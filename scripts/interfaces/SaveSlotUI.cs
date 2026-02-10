using Godot;
using System;
using MementoTest.Core;

// Pastikan namespace ini SAMA dengan yang ada di SaveMenu.cs
namespace MementoTest.UI
{
	public partial class SaveSlotUI : Button
	{
		// Signal untuk memberi tahu Menu bahwa slot ini diklik
		[Signal] public delegate void SlotSelectedEventHandler(int slotIndex, bool isEmpty);

		[Export] public int SlotIndex = 1; // Slot 1, 2, atau 3

		private Label _slotNameLabel;
		private Label _playerNameLabel;
		private Label _classLabel;
		private Label _dateLabel;

		public override void _Ready()
		{
			// Pastikan path node ini sesuai dengan Scene SaveSlot.tscn Anda
			_slotNameLabel = GetNode<Label>("MarginContainer/VBoxContainer/SlotNameLabel");
			_playerNameLabel = GetNode<Label>("MarginContainer/VBoxContainer/PlayerNameLabel");
			_classLabel = GetNode<Label>("MarginContainer/VBoxContainer/ClassLabel");
			_dateLabel = GetNode<Label>("MarginContainer/VBoxContainer/DateLabel");

			Pressed += OnBtnPressed;
			RefreshUI();
		}

		private void OnBtnPressed()
		{
			// Kirim sinyal ke Parent (SaveMenu)
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
				_classLabel.Text = $"Class: {data.ClassType}";
				_dateLabel.Text = data.LastPlayedDate;
				_playerNameLabel.Modulate = Colors.White;
			}
			else
			{
				_playerNameLabel.Text = "EMPTY";
				_classLabel.Text = "-";
				_dateLabel.Text = "";
				_playerNameLabel.Modulate = Colors.Gray;
			}
		}
	}
}