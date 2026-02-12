using Godot;
using MementoTest.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Godot.Control;

namespace MementoTest.UI
{
	public partial class BattleHUD : CanvasLayer
	{
		#region Signals
		[Signal] public delegate void CommandSubmittedEventHandler(string commandText);
		[Signal] public delegate void ReactionSuccessEventHandler();
		[Signal] public delegate void EndTurnRequestedEventHandler();
		#endregion


		#region UI References
		[ExportGroup("Command System")]
		[Export] public LineEdit CommandInput;        // Input Transparan (Hidden)
		[Export] public RichTextLabel CommandFeedback; // Teks Visual Tengah Layar
		[ExportGroup("Turn Info")]
		[Export] public Label TurnLabel;

		[ExportGroup("Combat Log")]
		[Export] public RichTextLabel CombatLogLabel;
		[Export] public ScrollContainer LogScroll;







		[ExportGroup("Combat UI")]
		[Export] public Control CombatPanel;

		[ExportGroup("Stats")]
		[Export] public ProgressBar PlayerHPBar;
		[Export] public ProgressBar PlayerAPBar;
		[Export] public Label HPLabel;
		[Export] public Label ScoreLabel;
		[Export] public Label ComboLabel;

		[ExportGroup("Reaction UI")]
		[Export] public Control ReactionPanel;
		[Export] public Label ReactionPromptLabel;
		[Export] public ProgressBar ReactionTimerBar;
		#endregion

		#region Configuration & State
		private const string COLOR_CORRECT = "#00FF00";
		private const string COLOR_WRONG = "#FF4444";
		private const string COLOR_GHOST = "#FFFFFF40";
		private const int MAX_LOG_LINES = 3;



		private HashSet<string> _availableCommands = new HashSet<string>();
		private TaskCompletionSource<bool> _reactionTaskSource;
		private Tween _timerTween;

		private string _expectedReactionWord = "";
		private bool _isReactionPhase = false;
		private bool _canEndTurn = false;

		// Properti publik untuk mengecek apakah HUD sedang menunggu input penting
		public bool IsBusy => _isReactionPhase || (_reactionTaskSource != null && !_reactionTaskSource.Task.IsCompleted);
		#endregion

		private float _reactionStartTime; // Untuk mencatat kapan prompt muncul
		public float LastReactionTime { get; private set; } // Properti yang dicari EnemyController

		#region Lifecycle
		public override void _Ready()
		{
			if (ReactionPanel != null) ReactionPanel.Visible = false;
			if (CombatPanel != null) CombatPanel.Visible = false;

			if (CommandInput != null)
			{
				// Bersihkan koneksi lama agar tidak double trigger
				if (CommandInput.IsConnected(LineEdit.SignalName.TextSubmitted, Callable.From<string>(OnCommandEntered)))
					CommandInput.TextSubmitted -= OnCommandEntered;

				CommandInput.TextSubmitted += OnCommandEntered;
				CommandInput.TextChanged += OnCommandInputChanged;

				// Buat input asli tak terlihat tapi tetap fungsional
				CommandInput.Modulate = new Color(1, 1, 1, 0);
			}
			if (TurnLabel != null)
				TurnLabel.Visible = false;



			ConnectToScoreManager();
			AddToGroup("HUD");

		}
		public override void _Process(double delta)
		{
			// Hanya rebut fokus jika benar-benar hilang, bukan setiap frame
			if (_isReactionPhase && CommandInput != null)
			{
				if (!CommandInput.HasFocus() && CommandInput.Visible && CommandInput.Editable)
				{
					CommandInput.GrabFocus();
				}
			}


			// existing reaction focus logic
			if (_isReactionPhase && CommandInput != null)
			{
				if (!CommandInput.HasFocus() && CommandInput.Visible && CommandInput.Editable)
				{
					CommandInput.GrabFocus();
				}
			}
		}




		private void InitializeHUD()
		{
			SetupInputStyle();
			ClearLog();

			if (ReactionPanel != null)
				ReactionPanel.Visible = false;
		}
		#endregion

		public void SetTurnLabelVisible(bool visible)
		{
			if (TurnLabel != null)
				TurnLabel.Visible = visible;
		}

		#region Input Logic
		private void SetupInputStyle()
		{
			if (CommandInput != null)
			{
				CommandInput.Modulate = new Color(1, 1, 1, 0); // Sembunyikan input asli
				CommandInput.MouseFilter = Control.MouseFilterEnum.Stop;
				CommandInput.GrabFocus();
			}

			if (CommandFeedback != null)
			{
				CommandFeedback.BbcodeEnabled = true;
				CommandFeedback.Text = string.Empty;
			}
		}

		private void OnCommandInputChanged(string newText)
		{
			if (string.IsNullOrEmpty(newText) || CommandFeedback == null)
			{
				CommandFeedback.Text = "";
				return;
			}

			string input = newText.ToUpper();

			// --- 1. LOGIKA AUTO-SUBMIT (Jika Ketikan Benar Total) ---
			if (_isReactionPhase)
			{
				if (input == _expectedReactionWord)
				{
					_reactionTaskSource?.TrySetResult(true);
					return;
				}
			}
			else if (_availableCommands.Contains(input))
			{
				// Langsung submit tanpa perlu tekan Enter
				EmitSignal(SignalName.CommandSubmitted, input);
				ResetInputUI();
				return;
			}

			// --- 2. LOGIKA STRICT TYPO (Satu Huruf Salah = Missed) ---
			string target = _isReactionPhase ? _expectedReactionWord : FindBestMatch(input);

			if (string.IsNullOrEmpty(target) || !target.StartsWith(input))
			{
				// TYPO TERDETEKSI!
				if (_isReactionPhase)
				{
					_reactionTaskSource?.TrySetResult(false);
				}
				else
				{
					// Beri feedback visual bahwa serangan meleset
					CommandFeedback.Text = $"[center][color={COLOR_WRONG}]MISSED! [/color][/center]";
					LogToTerminal(">>> ATTACK MISSED: TYPO!", Colors.Red);

					// Kirim signal MISSED ke TurnManager/PlayerController jika perlu
					EmitSignal(SignalName.CommandSubmitted, "MISSED");
				}

				// Reset input setelah jeda singkat agar player bisa melihat tulisan "MISSED"
				ResetInputWithDelay();
				return;
			}

			// --- 3. VISUAL FEEDBACK (Jika Masih Benar) ---
			UpdateFeedbackVisuals(input, target);
		}

		private async void ResetInputWithDelay()
		{
			// Kunci input agar tidak bisa ngetik saat animasi gagal
			CommandInput.Editable = false;

			// Tunggu sebentar (0.5 detik) agar player bisa baca feedback
			await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

			ResetInputUI();
			CommandInput.Editable = true;
			CommandInput.GrabFocus();
		}
		private string FormatReactionText(string input, string target)
		{
			if (string.IsNullOrEmpty(target)) return "";

			string formatted = "";

			// Kita looping berdasarkan panjang kata target (misal: PARRY)
			for (int i = 0; i < target.Length; i++)
			{
				if (i < input.Length)
				{
					// Jika huruf yang diketik sesuai dengan target
					if (input[i] == target[i])
					{
						formatted += $"[color=#ffffff]{target[i]}[/color]"; // Putih terang
					}
					else
					{
						formatted += $"[color=#ff6666]{target[i]}[/color]"; // Merah (salah ketik)
					}
				}
				else
				{
					// Huruf yang belum diketik (Ghost Text)
					formatted += $"[color=#ffffff40]{target[i]}[/color]"; // Transparan/Abu-abu
				}
			}

			return $"[center]{formatted}[/center]";
		}
		private void UpdateFeedbackVisuals(string input, string target)
		{
			string bbcode = "";

			if (!string.IsNullOrEmpty(target))
			{
				// Warnai setiap huruf yang diketik
				for (int i = 0; i < input.Length; i++)
				{
					bool isCorrect = i < target.Length && input[i] == target[i];
					bbcode += $"[color={(isCorrect ? COLOR_CORRECT : COLOR_WRONG)}]{input[i]}[/color]";
				}

				// Tambahkan Ghost Text untuk sisa kata
				if (input.Length < target.Length)
				{
					bbcode += $"[color={COLOR_GHOST}]{target.Substring(input.Length)}[/color]";
				}
			}
			else
			{
				bbcode = $"[color={COLOR_WRONG}]{input}[/color]";
			}

			CommandFeedback.Text = $"[center]{bbcode}[/center]";
		}

		private string FindBestMatch(string input) =>
			_availableCommands.FirstOrDefault(cmd => cmd.StartsWith(input));

		private void OnCommandEntered(string text)
		{
			string cleanText = text.Trim().ToUpper();

			if (_isReactionPhase)
			{
				if (cleanText == _expectedReactionWord)
					_reactionTaskSource?.TrySetResult(true);
				else
					_reactionTaskSource?.TrySetResult(false); // Gagal jika typo dan tekan enter

				return;
			}

			// Jalankan perintah giliran player
			if (_availableCommands.Contains(cleanText) || cleanText == "END")
			{
				EmitSignal(SignalName.CommandSubmitted, cleanText);
			}

			CommandInput.Clear();
			CommandFeedback.Text = "";
		}

		public void ShowCombatPanel(bool show)
		{
			// Mengontrol panel stats/HP, bukan panel Dodge
			if (CombatPanel != null) CombatPanel.Visible = show;
		}
		public void UpdateTurnLabel(string turnName)
		{
			if (TurnLabel == null) return;

			TurnLabel.Text = turnName.ToUpper();

			// Opsional: Beri warna berbeda agar lebih kontras
			if (turnName.ToUpper().Contains("PLAYER"))
				TurnLabel.Modulate = Colors.SkyBlue;
			else if (turnName.ToUpper().Contains("ENEMY"))
				TurnLabel.Modulate = Colors.OrangeRed;
			else
				TurnLabel.Modulate = Colors.White;
		}
		private void ResetInputUI()
		{
			CommandInput?.Clear();
			if (CommandFeedback != null) CommandFeedback.Text = string.Empty;
		}
		#endregion

		#region Combat Log System
		public void LogToTerminal(string message, Color color)
		{
			if (CombatLogLabel == null) return;

			string timeStr = DateTime.Now.ToString("HH:mm:ss");
			string newLine = $"[color=#888888][{timeStr}][/color] [color={color.ToHtml()}]{message}[/color]";

			var lines = CombatLogLabel.Text
				.Split('\n', StringSplitOptions.RemoveEmptyEntries)
				.ToList();

			lines.Add(newLine);

			if (lines.Count > MAX_LOG_LINES)
				lines = lines.Skip(lines.Count - MAX_LOG_LINES).ToList();

			CombatLogLabel.Text = string.Join("\n", lines);

			ScrollToBottom();
		}

		private async void ScrollToBottom()
		{
			if (LogScroll == null) return;
			await ToSignal(GetTree(), "process_frame");
			LogScroll.ScrollVertical = (int)LogScroll.GetVScrollBar().MaxValue;
		}

		private void ClearLog() => CombatLogLabel.Text = string.Empty;
		#endregion

		#region Reaction Mechanics
		public async Task<bool> WaitForPlayerReaction(string expectedWord, float timeLimit)
		{
			// 1. Reset State
			_reactionTaskSource = new TaskCompletionSource<bool>();
			_isReactionPhase = true;
			_expectedReactionWord = expectedWord.ToUpper();
			_reactionStartTime = Time.GetTicksMsec() / 1000f;

			// 2. Tampilkan UI
			UpdateReactionUI(true, timeLimit);

			// 3. AGGRESSIVE FOCUS
			if (CommandInput != null)
			{
				CommandInput.Editable = true;
				CommandInput.Visible = true;
				CommandInput.Text = ""; // Pastikan kosong

				// Buang fokus lama (jika ada) untuk me-reset status input engine
				CommandInput.ReleaseFocus();

				// Tunggu 2 frame agar engine benar-benar bersih dari event klik/mouse
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

				CommandInput.GrabFocus();

				// Trik Tambahan: Paksa kursor ke kolom 0
				CommandInput.CaretColumn = 0;
			}

			// 4. Timer & Racing Task
			bool result = false;
			var delayTask = Task.Delay((int)(timeLimit * 1000));
			var inputTask = _reactionTaskSource.Task;

			var completedTask = await Task.WhenAny(inputTask, delayTask);
			result = (completedTask == inputTask) ? await inputTask : false;

			// 5. Cleanup
			// 5. Cleanup
			if (!IsInstanceValid(this))
				return false;

			_isReactionPhase = false;

			UpdateReactionUI(false);

			EnableInput(false);
			ResetInputUI();


			return result;
		}
		public void LogRPG(string message, string icon, Color color)
		{
			if (CombatLogLabel == null) return;

			string formatted = $"{icon}  {message}";
			LogToTerminal(formatted, color);
		}


		private void UpdateReactionUI(bool show, float time = 0)
		{
			if (!IsInstanceValid(this)) return;

			if (ReactionPanel == null || !IsInstanceValid(ReactionPanel))
				return;

			ReactionPanel.Visible = show;

			if (show)
			{
				if (ReactionPromptLabel != null && IsInstanceValid(ReactionPromptLabel))
					ReactionPromptLabel.Text = $"TYPE: {_expectedReactionWord}!";

				if (ReactionTimerBar != null && IsInstanceValid(ReactionTimerBar))
				{
					ReactionTimerBar.MaxValue = time;
					ReactionTimerBar.Value = time;

					_timerTween?.Kill();

					if (IsInstanceValid(ReactionTimerBar))
					{
						_timerTween = CreateTween();
						_timerTween.TweenProperty(ReactionTimerBar, "value", 0, time);
					}
				}
			}
		}

		private void FailReaction()
		{
			_reactionTaskSource?.TrySetResult(false);
			if (CommandFeedback != null)
				CommandFeedback.Text = $"[center][color={COLOR_WRONG}]FAILED![/color][/center]";
		}
		#endregion

		#region Public API & Stats
		public void EnterPlayerCommandPhase(IEnumerable<string> commands)
		{
			_isReactionPhase = false;
			_availableCommands = new HashSet<string>(commands.Select(c => c.ToUpper()));


			_availableCommands.Add("END");

			EnableInput(true);
		}

		public void SetEndTurnButtonInteractable(bool interactable)
		{
			_canEndTurn = interactable;

			// Jika sedang tidak boleh end turn, hapus "END" dari daftar bantuan visual
			if (!interactable) _availableCommands.Remove("END");
			else _availableCommands.Add("END");
		}
		public void EnableInput(bool enable)
		{
			if (CommandInput == null) return;

			CommandInput.Editable = enable;

			if (enable)
			{
				CommandInput.FocusMode = FocusModeEnum.All;
				CommandInput.Clear();

				// Solusi CS0117: Merujuk langsung ke Control.MethodName
				CommandInput.CallDeferred(Control.MethodName.GrabFocus);
			}
			else
			{
				CommandInput.ReleaseFocus();
			}
		}

		public void UpdateAP(int current, int max)
		{
			if (PlayerAPBar == null) return;
			PlayerAPBar.MaxValue = max;
			PlayerAPBar.Value = current;
		}

		public void UpdateHP(int current, int max)
		{
			if (PlayerHPBar == null) return;
			PlayerHPBar.MaxValue = max;
			PlayerHPBar.Value = current;
			if (HPLabel != null) HPLabel.Text = $"{current}/{max}";
		}
		public void EnablePlayerInput(bool enable = true)
		{
			EnableInput(enable);
		}
		private void ConnectToScoreManager()
		{
			if (ScoreManager.Instance == null) return;
			ScoreManager.Instance.ScoreUpdated += (s) => ScoreLabel.Text = $"SCORE: {s:N0}";
			ScoreManager.Instance.ComboUpdated += (c) => ComboLabel.Text = $"COMBO: x{c}";
		}

		public void ExitPlayerCommandPhase()
		{
			_isReactionPhase = false;
			_availableCommands.Clear();

			// Matikan input agar player tidak bisa mengetik saat animasi berjalan
			EnableInput(false);

			// Bersihkan teks yang tersisa di layar
			ResetInputUI();

			// LogToTerminal("Command phase ended.", Colors.DimGray); // Opsional
		}
		#endregion
	}
	
}