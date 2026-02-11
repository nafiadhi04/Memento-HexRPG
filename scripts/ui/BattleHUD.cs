using Godot;
using System;
using System.Threading.Tasks;
using MementoTest.Core;
using System.Collections.Generic;
using MementoTest.Resources;

namespace MementoTest.UI
{
	public partial class BattleHUD : CanvasLayer
	{
		// --- SIGNALS ---
		[Signal] public delegate void CommandSubmittedEventHandler(string commandText);
		[Signal] public delegate void EndTurnRequestedEventHandler();
		// (Signal ReactionEnded dihapus karena kita ganti pakai Task yang lebih canggih)
		[Signal]
		public delegate void ReactionSuccessEventHandler();


		[Export] public Label ScoreLabel;
		[Export] public Label ComboLabel;

		// =============================
		// PLAYER COMMAND DATA
		// =============================
		private HashSet<string> _availableCommands = new HashSet<string>();

		// --- EXPORT UI ---
		[Export] public Control ReactionPanel;
		[Export] public Label ReactionPromptLabel;
		[Export] public ProgressBar ReactionTimerBar;

		// Warna Hex untuk BBCode
		private const string COLOR_CORRECT = "#00FF00"; // Hijau (Benar)
		private const string COLOR_WRONG = "#FF0000";   // Merah (Typo)
		private const string COLOR_GHOST = "#FFFFFF40"; // Abu-abu (Sisa kata/Hi



		// --- [BARU] PLAYER STATS BARS ---
		[Export] public ProgressBar PlayerHPBar;
		[Export] public ProgressBar PlayerAPBar;
		[Export] public Label HPLabel;

		// --- INTERNAL ---// =============================
		// INPUT STATE
		// =============================
		private bool _isPlayerCommandPhase = false;   // giliran player ngetik command attack
		private bool _isReactionPhase = false;        // parry / dodge
		private string _expectedCommand = "";         // command attack yang valid
		private string _expectedReactionWord = "";    // parry / dodge


		//  Wadah untuk menunggu hasil input player
		private TaskCompletionSource<bool> _reactionTaskSource;

		private Tween _timerTween;

		private Control _combatPanel;
		private LineEdit _commandInput;
		private RichTextLabel _combatLog;

		// --- EXPORT UI ---
		[ExportGroup("Command System")]
		[Export] public LineEdit CommandInput;        // Input asli (Transparan)
		[Export] public RichTextLabel CommandFeedback;
		private Label _apLabel;
		private Button _endTurnBtn;
		private TaskCompletionSource<bool> _reactionTcs;
		private bool _isWaitingReaction;
		private double _reactionStartTime;
		public float LastReactionTime { get; private set; }

		private string _expectedWord = "";
		private bool _reactionActive = false;
		public bool IsBusy { get; private set; }




		public override void _Ready()
		{
			GD.Print($"[HUD] Score visible: {ScoreLabel.Visible}");

			if (ReactionPanel != null)
				ReactionPanel.Visible = false;

			_endTurnBtn = GetNodeOrNull<Button>("Control/EndTurnBtn");
			if (_endTurnBtn != null)
				_endTurnBtn.Pressed += () => EmitSignal(SignalName.EndTurnRequested);

			if (PlayerHPBar != null)
			{
				PlayerHPBar.MaxValue = PlayerHPBar.MaxValue;
				PlayerHPBar.Value = PlayerHPBar.MaxValue;
				PlayerHPBar.Modulate = Colors.White;
			}

			if (PlayerAPBar != null)
			{
				PlayerAPBar.Value = PlayerAPBar.MaxValue;
			}

			SetupCombatUI();

			// Auto-assign jika belum di-set di Inspector
			if (CommandInput == null) CommandInput = GetNodeOrNull<LineEdit>("%CommandInput");
			if (CommandFeedback == null) CommandFeedback = GetNodeOrNull<RichTextLabel>("%CommandFeedback");

			if (CommandInput != null)
			{
				CommandInput.TextChanged += OnCommandInputChanged;
				CommandInput.TextSubmitted += OnCommandEntered;

				// Pastikan input asli transparan agar tidak menutupi teks warna
				// (Tapi caret/kursor tetap terlihat jika tema default)
				CommandInput.Modulate = new Color(1, 1, 1, 0);
			}

			if (CommandFeedback != null)
			{
				CommandFeedback.BbcodeEnabled = true;
				CommandFeedback.Text = "";
			}

			if (ReactionPanel != null) ReactionPanel.Visible = false;

			_commandInput = GetNode<LineEdit>(
				"Control/CombatPanel/VBoxContainer/CommandInput"

				
			);

			//  CONNECT SEKALI 
			_commandInput.TextSubmitted += OnCommandEntered;
			_commandInput.TextChanged += OnCommandInputChanged;

			// Optional safety
			_commandInput.Editable = true;
			_commandInput.MouseFilter = Control.MouseFilterEnum.Stop;

			CallDeferred("ConnectToScoreManager");

			GD.Print("[HUD] BattleHUD Ready - Input connected ONCE");
		}

		private void ConnectToScoreManager()
		{
			if (ScoreManager.Instance != null)
			{
				ScoreManager.Instance.ScoreUpdated += OnScoreUpdated;
				ScoreManager.Instance.ComboUpdated += OnComboUpdated;

				// Reset tampilan awal
				OnScoreUpdated(0);
				OnComboUpdated(0);
			}
		}

		private void OnScoreUpdated(int newScore)
		{
			if (ScoreLabel != null) ScoreLabel.Text = $"SCORE: {newScore:N0}"; // N0 biar ada titik (1.000)
		}

		private void OnComboUpdated(int newCombo)
		{
			if (ComboLabel != null)
			{
				ComboLabel.Text = $"COMBO: x{newCombo}";

				// Efek visual sederhana: Warna berubah kalau combo tinggi
				if (newCombo > 5) ComboLabel.Modulate = Colors.Yellow;
				else ComboLabel.Modulate = Colors.White;

				// Animasi kecil (Punch)
				var tween = CreateTween();
				ComboLabel.Scale = new Vector2(1.5f, 1.5f);
				tween.TweenProperty(ComboLabel, "scale", Vector2.One, 0.2f);
			}
		}

		private void SetupCombatUI()
		{
			if (!HasNode("Control/CombatPanel")) return;
			if (_combatPanel == null) _combatPanel = GetNodeOrNull<Control>("Control/CombatPanel");

			// Pastikan panel combat tersembunyi di awal
			if (_combatPanel != null) _combatPanel.Visible = false;

			_combatPanel = GetNode<Control>("Control/CombatPanel");
			_combatLog = _combatPanel.GetNodeOrNull<RichTextLabel>("VBoxContainer/CombatLog");
			_commandInput = _combatPanel.GetNodeOrNull<LineEdit>("VBoxContainer/CommandInput");

			_apLabel = _combatPanel.GetNodeOrNull<Label>("VBoxContainer/APLabel")
					?? _combatPanel.GetNodeOrNull<Label>("APLabel");

			_combatPanel.Visible = false;
		}

		public override void _Process(double delta)
		{
			// Logika Timer & Fokus saat Fase Reaksi
			if (_isReactionPhase && ReactionPanel.Visible)
			{
				// Paksa Fokus ke Input agar player bisa langsung ketik
				if (_commandInput != null && !_commandInput.HasFocus())
					_commandInput.GrabFocus();

				// Hitung Mundur Waktu
				if (ReactionTimerBar != null)
				{
					ReactionTimerBar.Value -= delta;
					if (ReactionTimerBar.Value <= 0)
					{
						FailReaction("TIMEOUT");
					}
				}
			}
		}

		// --- FUNGSI UTAMA (DIPANGGIL MUSUH) ---
		public async Task<bool> WaitForPlayerReaction(string expectedWord, float timeLimit)
		{
			// =============================
			// 1. CANCEL PREVIOUS REACTION (SAFE)
			// =============================
			if (_reactionTaskSource != null && !_reactionTaskSource.Task.IsCompleted)
			{
				_reactionTaskSource.TrySetResult(false);
			}

			_reactionTaskSource = new TaskCompletionSource<bool>();

			if (_timerTween != null && _timerTween.IsValid())
				_timerTween.Kill();

			_isReactionPhase = true;
			_expectedReactionWord = expectedWord.ToLower();

			_reactionStartTime = Time.GetUnixTimeFromSystem();

			_commandInput.Clear();
			_commandInput.Modulate = Colors.White;
			_commandInput.Editable = true;
			_commandInput.Visible = true;

			// tunggu 1 frame supaya UI siap
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			_commandInput.GrabFocus();

			// =============================
			// 3. SETUP UI
			// =============================
			ShowCombatPanel(true);

			if (ReactionPanel == null)
			{
				GD.PrintErr("[HUD] ReactionPanel NULL!");
				return false;
			}
			_reactionTcs = new TaskCompletionSource<bool>();

			ReactionPanel.Visible = true;

			if (ReactionPromptLabel != null)
			{
				ReactionPromptLabel.Text = $"TYPE: {expectedWord.ToUpper()}!";
			}

			if (ReactionTimerBar != null)
			{
				ReactionTimerBar.MaxValue = timeLimit;
				ReactionTimerBar.Value = timeLimit;

				_timerTween = CreateTween();
				_timerTween.TweenProperty(
					ReactionTimerBar,
					"value",
					0,
					timeLimit
				).SetTrans(Tween.TransitionType.Linear);

				_timerTween.TweenCallback(Callable.From(() =>
				{
					if (_reactionTaskSource != null && !_reactionTaskSource.Task.IsCompleted)
					{
						GD.Print("[HUD] REACTION TIMEOUT");
						FailReaction("TIMEOUT");
					}
				}));
			}

			// =============================
			// 4. INPUT PREPARATION (FOCUS SAFE)
			// =============================
			if (_commandInput != null)
			{
				_commandInput.Visible = true;
				_commandInput.Editable = true;
				_commandInput.Clear();
				_commandInput.Modulate = Colors.White;

				// tunggu 1 frame agar UI siap sepenuhnya
				await ToSignal(GetTree(), "process_frame");

				_commandInput.GrabFocus();
				GD.Print($"[HUD] Input Focus FORCED. Owner: {_commandInput.Name}");
			}

			GD.Print($"[HUD] WAITING INPUT: '{expectedWord}'");

			// =============================
			// 5. WAIT RESULT
			// =============================
			bool result = await _reactionTaskSource.Task;

			// =============================
			// 6. CLEANUP (FINAL & CONSISTENT)
			// =============================
			_isReactionPhase = false;
			_commandInput.Modulate = Colors.White;

			if (_timerTween != null && _timerTween.IsValid())
				_timerTween.Kill();

			if (ReactionPanel != null)
				ReactionPanel.Visible = false;

			if (_commandInput != null)
			{
				_commandInput.Clear();
				_commandInput.Modulate = Colors.White;
				_commandInput.ReleaseFocus();
			}

			if (result)
			{
				EmitSignal(SignalName.ReactionSuccess);
			}

			return result;
		}




		// --- INPUT HANDLER ---
		private void OnCommandEntered(string text)
		{
			// [PERUBAHAN] Trim + ToUpper
			string cleanText = text.Trim().ToUpper();

			// 1. Logic Reaction
			if (_isReactionPhase)
			{
				// Bandingkan dengan expected reaction (juga di-ToUpper)
				if (cleanText == _expectedReactionWord.ToUpper())
				{
					_reactionTaskSource?.TrySetResult(true);
				}
				else
				{
					FailReaction("TYPO");
				}
				ResetInputUI();
				return;
			}

			// 2. Logic Normal Command
			// Validasi apakah command ada di daftar
			if (_availableCommands.Contains(cleanText))
			{
				EmitSignal(SignalName.CommandSubmitted, cleanText);
			}
			else
			{
				EmitSignal(SignalName.CommandSubmitted, cleanText);
			}

			ResetInputUI();
		}



		private void ResetInputUI()
		{
			if (CommandInput != null) CommandInput.Clear();
			if (CommandFeedback != null) CommandFeedback.Text = "";
		}

		// ==========================================================
		//  STATE MANAGEMENT
		// ==========================================================

		// Dipanggil PlayerController saat giliran player mulai
		public void EnterPlayerCommandPhase(IEnumerable<string> commands)
		{
			_isReactionPhase = false;

			_availableCommands.Clear();
			// [PERUBAHAN] Simpan semua sebagai UpperCase
			foreach (var cmd in commands) _availableCommands.Add(cmd.ToUpper());

			EnableInputUI(true);
			GD.Print("[HUD] Player Command Phase Started");
		}
		private void OnCommandInputChanged(string newText)
		{
			if (string.IsNullOrEmpty(newText))
			{
				if (CommandFeedback != null) CommandFeedback.Text = "";
				return;
			}

			// [PERUBAHAN] Paksa UpperCase di sini
			string input = newText.ToUpper();
			string bestMatch = "";
			string bbcode = "";

			// --- A. FASE REAKSI (PARRY/DODGE) ---
			if (_isReactionPhase)
			{
				// Pastikan target juga UpperCase
				bestMatch = _expectedReactionWord.ToUpper();
			}
			// --- B. FASE COMMAND BIASA (ATTACK) ---
			else
			{
				// Cari skill yang cocok (Asumsi daftar skill sudah UpperCase semua)
				bestMatch = FindBestMatch(input);
			}

			// --- C. GENERATE WARNA (BBCODE) ---
			if (!string.IsNullOrEmpty(bestMatch))
			{
				for (int i = 0; i < input.Length; i++)
				{
					if (i < bestMatch.Length && input[i] == bestMatch[i])
					{
						bbcode += $"[color={COLOR_CORRECT}]{input[i]}[/color]"; // Hijau
					}
					else
					{
						bbcode += $"[color={COLOR_WRONG}]{input[i]}[/color]"; // Merah
					}
				}

				// Ghost Text (Sisa Huruf)
				if (input.Length < bestMatch.Length)
				{
					string ghostSuffix = bestMatch.Substring(input.Length);
					bbcode += $"[color={COLOR_GHOST}]{ghostSuffix}[/color]";
				}
			}
			else
			{
				bbcode = $"[color={COLOR_WRONG}]{input}[/color]";
			}

			if (CommandFeedback != null) CommandFeedback.Text = $"[center]{bbcode}[/center]";
		}

		private string FindBestMatch(string input)
		{

			foreach (var cmd in _availableCommands)
			{
				
				if (cmd.ToUpper().StartsWith(input)) return cmd.ToUpper();
			}
			return null;
		}	
		private void UpdateTypingColor(string input, string expected)
		{
			if (string.IsNullOrEmpty(input))
			{
				_commandInput.Modulate = Colors.White;
				return;
			}

			for (int i = 0; i < input.Length; i++)
			{
				if (i >= expected.Length || input[i] != expected[i])
				{
					_commandInput.Modulate = Colors.IndianRed;
					return;
				}
			}

			_commandInput.Modulate = Colors.LimeGreen;
		}


		private void FailReaction(string reason)
		{
			if (!_isReactionPhase)
				return;

			GD.Print($"[HUD] REACTION FAILED: {reason}");

			if (!_reactionTaskSource.Task.IsCompleted)
				_reactionTaskSource.SetResult(false);

			_isReactionPhase = false;

			if (ReactionPromptLabel != null)
				ReactionPromptLabel.Text = "FAILED!";

			_commandInput.Modulate = Colors.Red;
		}

		public void SetAvailableCommands(IEnumerable<PlayerSkill> skills)
		{
			_availableCommands.Clear();

			foreach (var skill in skills)
			{
				_availableCommands.Add(skill.CommandName.ToLower());
			}

			GD.Print($"[HUD] Available Commands: {string.Join(", ", _availableCommands)}");
		}


		public async void asEnterPlayerCommandPhase(IEnumerable<string> commands)
		{
			if (_commandInput == null)
			{
				GD.PrintErr("[HUD] CommandInput is NULL in EnterPlayerCommandPhase");
				return;
			}

			_isPlayerCommandPhase = true;
			_isReactionPhase = false;

			_availableCommands = new HashSet<string>(commands);

			_commandInput.Visible = true;
			_commandInput.Editable = true;
			_commandInput.Clear();
			_commandInput.Modulate = Colors.White;

			await ToSignal(GetTree(), "process_frame");
			_commandInput.GrabFocus();

			GD.Print("[HUD] Player command phase started");
		}


		private void FocusCommandInput()
		{
			_commandInput.GrabFocus();
		}
		private bool IsValidCommand(string command)
		{
			return _availableCommands.Contains(command);
		}


		public void ExitPlayerCommandPhase()
		{
			_isPlayerCommandPhase = false;
			_commandInput.ReleaseFocus();
		}

		public void EnableInputUI(bool enable)
		{
			if (_combatPanel != null) _combatPanel.Visible = enable;

			if (enable && CommandInput != null)
			{
				CommandInput.Editable = true;
				CommandInput.Clear();
				CommandInput.GrabFocus();
			}
		}

		// --- Helper Methods ---
		public void ShowCombatPanel(bool show)
		{
			if (_combatPanel != null)
			{
				_combatPanel.Visible = show;
				if (show && _commandInput != null) _commandInput.GrabFocus();
			}
		}
		public void LogToTerminal(string message, Color color)
		{
			if (_combatLog != null)
			{
				string hexColor = color.ToHtml();
				_combatLog.AppendText($"[color=#{hexColor}]{message}[/color]\n");
			}
			else
			{
				// Fallback jika UI Log belum siap
				GD.Print($"[LOG] {message}");
			}
		}

		private void GrabFocusDeferred()
		{
			if (_commandInput != null) _commandInput.GrabFocus();
		}

		public void SetEndTurnButtonInteractable(bool interactable)
		{
			if (_endTurnBtn != null) _endTurnBtn.Disabled = !interactable;
		}
		public void UpdateTurnLabel(string text) { } // Implementasi label jika ada
		public void UpdateAP(int current, int max)
		{
			// 1. Update Text Label (Cara Lama)
			if (_apLabel != null) _apLabel.Text = $"AP: {current}/{max}";

			// 2. [BARU] Update Progress Bar dengan Animasi
			if (PlayerAPBar != null)
			{
				PlayerAPBar.MaxValue = max;

				// Buat Tween agar bar turun/naik perlahan
				var tween = CreateTween();
				tween.TweenProperty(PlayerAPBar, "value", current, 0.3f)
					.SetTrans(Tween.TransitionType.Sine)
					.SetEase(Tween.EaseType.Out);
			}
		}

		public void UpdateHP(int current, int max)
		{
			// Update Text (Opsional)
			if (HPLabel != null) HPLabel.Text = $"{current}/{max}";

			// Update Progress Bar dengan Animasi
			if (PlayerHPBar != null)
			{
				PlayerHPBar.MaxValue = max;

				var tween = CreateTween();
				tween.TweenProperty(PlayerHPBar, "value", current, 0.3f)
					.SetTrans(Tween.TransitionType.Quint) // Efek hentakan sedikit
					.SetEase(Tween.EaseType.Out);

				// Ubah warna bar jadi merah gelap jika HP kritis (< 20%)
				if ((float)current / max < 0.2f)
				{
					PlayerHPBar.Modulate = Colors.Red;
				}
				else
				{
					PlayerHPBar.Modulate = Colors.White; // Reset warna normal
				}
			}
		}

		private void CompleteReaction(bool success)
		{
			if (_reactionTcs == null) return;

			if (!_reactionTcs.Task.IsCompleted)
			{
				_reactionTcs.SetResult(success);
			}

			_isWaitingReaction = false;
		}


		private void ForceCancelReaction()
		{
			if (_reactionTcs != null && !_reactionTcs.Task.IsCompleted)
			{
				_reactionTcs.SetResult(false);
			}

			_isWaitingReaction = false;
			_commandInput.Clear();
			_commandInput.ReleaseFocus();
		}



		public async void EnablePlayerInput()
		{
			if (_combatPanel != null)
			{
				_combatPanel.Visible = true;

				if (_commandInput != null)
				{
					_commandInput.Editable = true;
					_commandInput.Clear();

					// 1. Tunggu 1 frame agar Godot selesai merender perubahan UI turn sebelumnya
					await ToSignal(GetTree(), "process_frame");

					// 2. NUCLEAR RESET: Lepaskan fokus dulu (biar tidak bingung)
					_commandInput.ReleaseFocus();

					// 3. Ambil Fokus
					_commandInput.GrabFocus();

					// 4. Pastikan kursor ada di posisi siap ketik
					_commandInput.CaretColumn = 0;

					// Debug: Pastikan fokus benar-benar diambil
					GD.Print($"[HUD] Input Focus FORCED. Owner: {GetViewport().GuiGetFocusOwner()?.Name}");
				}
			}
		}
	}
}