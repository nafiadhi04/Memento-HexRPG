using Godot;
using System;

namespace MementoTest.UI
{
	public partial class BattleHUD : CanvasLayer
	{
		[Signal] public delegate void EndTurnRequestedEventHandler();
		[Signal] public delegate void CommandSubmittedEventHandler(string commandText);

		private Button _endTurnBtn;
		private Label _turnLabel;

		// Komponen Baru (Dengan Path VBoxContainer)
		private Control _combatPanel;
		private Label _apLabel;
		private RichTextLabel _combatLog;
		private LineEdit _commandInput;

		public override void _Ready()
		{
			// Ambil node dasar
			_endTurnBtn = GetNode<Button>("Control/EndTurnBtn");
			_turnLabel = GetNode<Label>("Control/TurnLabel");

			// Setup tombol dan event
			_endTurnBtn.Pressed += () => EmitSignal(SignalName.EndTurnRequested);

			// Kita coba ambil panel combat (jika sudah ada di scene)
			// Path ini sudah disesuaikan dengan VBoxContainer
			if (HasNode("Control/CombatPanel"))
			{
				_combatPanel = GetNode<Control>("Control/CombatPanel");

				// Cek apakah VBoxContainer sudah dibuat user?
				if (_combatPanel.HasNode("VBoxContainer/CombatLog"))
				{
					_combatLog = GetNode<RichTextLabel>("Control/CombatPanel/VBoxContainer/CombatLog");
					_commandInput = GetNode<LineEdit>("Control/CombatPanel/VBoxContainer/CommandInput");

					// Hubungkan signal input
					_commandInput.TextSubmitted += OnCommandEntered;
				}

				if (_combatPanel.HasNode("APLabel")) // Jika AP Label masih di luar VBox
					_apLabel = GetNode<Label>("Control/CombatPanel/APLabel");
				else if (_combatPanel.HasNode("VBoxContainer/APLabel")) // Jika AP Label sudah masuk VBox
					_apLabel = GetNode<Label>("Control/CombatPanel/VBoxContainer/APLabel");

				// Sembunyikan di awal
				_combatPanel.Visible = false;
			}
		}

		private void OnCommandEntered(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return;
			EmitSignal(SignalName.CommandSubmitted, text);
			_commandInput.Clear();
		}

		// --- PUBLIC HELPER METHODS (LAZY LOADING) ---

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

		public void UpdateAP(int current, int max)
		{
			// SAFETY CHECK: Lazy Load APLabel
			if (_apLabel == null)
			{
				if (HasNode("Control/CombatPanel/VBoxContainer/APLabel"))
					_apLabel = GetNode<Label>("Control/CombatPanel/VBoxContainer/APLabel");
				else
					_apLabel = GetNode<Label>("Control/CombatPanel/APLabel");
			}

			if (_apLabel != null)
				_apLabel.Text = $"AP: {current}/{max}";
		}

		public void LogToTerminal(string message, Color color)
		{
			// SAFETY CHECK: Lazy Load CombatLog
			if (_combatLog == null)
			{
				_combatLog = GetNode<RichTextLabel>("Control/CombatPanel/VBoxContainer/CombatLog");
			}

			if (_combatLog != null)
			{
				string hexColor = color.ToHtml();
				_combatLog.AppendText($"[color=#{hexColor}]{message}[/color]\n");
			}
		}

		public void ShowCombatPanel(bool show)
		{
			// SAFETY CHECK: Lazy Load Panel & Input
			if (_combatPanel == null)
			{
				_combatPanel = GetNode<Control>("Control/CombatPanel");
				_commandInput = GetNode<LineEdit>("Control/CombatPanel/VBoxContainer/CommandInput");

				// Pastikan event connect jika baru loading
				if (!_commandInput.IsConnected("text_submitted", new Callable(this, MethodName.OnCommandEntered)))
				{
					_commandInput.TextSubmitted += OnCommandEntered;
				}
			}

			_combatPanel.Visible = show;
			if (show && _commandInput != null)
			{
				_commandInput.GrabFocus();
			}
		}
	}
}