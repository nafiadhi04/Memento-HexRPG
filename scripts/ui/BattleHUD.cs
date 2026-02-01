using Godot;
using System;

namespace MementoTest.UI
{
	public partial class BattleHUD : CanvasLayer
	{
		[Signal] public delegate void EndTurnRequestedEventHandler();
		// Signal baru: Mengirim teks command ke Controller
		[Signal] public delegate void CommandSubmittedEventHandler(string commandText);

		private Button _endTurnBtn;
		private Label _turnLabel;

		// Komponen Baru
		private Control _combatPanel;
		private Label _apLabel;
		private RichTextLabel _combatLog;
		private LineEdit _commandInput;

		public override void _Ready()
		{
			_endTurnBtn = GetNode<Button>("Control/EndTurnBtn");
			_turnLabel = GetNode<Label>("Control/TurnLabel");

			// Setup Komponen Baru
			_combatPanel = GetNode<Control>("Control/CombatPanel");
			_apLabel = GetNode<Label>("Control/CombatPanel/APLabel");
			_combatLog = GetNode<RichTextLabel>("Control/CombatPanel/CombatLog");
			_commandInput = GetNode<LineEdit>("Control/CombatPanel/CommandInput");

			_endTurnBtn.Pressed += () => EmitSignal(SignalName.EndTurnRequested);

			// Saat player tekan Enter di kotak input
			_commandInput.TextSubmitted += OnCommandEntered;

			// Sembunyikan panel combat di awal
			_combatPanel.Visible = false;
		}

		private void OnCommandEntered(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return;

			// Kirim signal ke PlayerController
			EmitSignal(SignalName.CommandSubmitted, text);

			// Kosongkan input
			_commandInput.Clear();
		}

		// --- PUBLIC HELPER METHODS ---

		public void UpdateAP(int current, int max)
		{
			// SAFETY CHECK: Kalau label belum siap, cari paksa sekarang juga
			if (_apLabel == null)
			{
				_apLabel = GetNode<Label>("Control/CombatPanel/APLabel");
			}

			_apLabel.Text = $"AP: {current}/{max}";
		}

		public void LogToTerminal(string message, Color color)
		{
			// SAFETY CHECK
			if (_combatLog == null)
			{
				_combatLog = GetNode<RichTextLabel>("Control/CombatPanel/CombatLog");
			}

			string hexColor = color.ToHtml();
			_combatLog.AppendText($"[color=#{hexColor}]{message}[/color]\n");
		}

		public void ShowCombatPanel(bool show)
		{
			// SAFETY CHECK
			if (_combatPanel == null)
			{
				_combatPanel = GetNode<Control>("Control/CombatPanel");
				_commandInput = GetNode<LineEdit>("Control/CombatPanel/CommandInput");
			}

			_combatPanel.Visible = show;
			if (show) _commandInput.GrabFocus();
		}

		// ... (Method lama SetEndTurnButtonInteractable & UpdateTurnLabel tetap ada) ...
		public void SetEndTurnButtonInteractable(bool interactable)
		{
			if (_endTurnBtn == null) _endTurnBtn = GetNode<Button>("Control/EndTurnBtn");
			_endTurnBtn.Disabled = !interactable;
			_endTurnBtn.Text = interactable ? "END TURN" : "ENEMY TURNING...";
		}

		public void UpdateTurnLabel(string text)
		{
			if (_turnLabel == null) _turnLabel = GetNode<Label>("Control/TurnLabel");
			_turnLabel.Text = text;
		}
	}
}