using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Wajib untuk async/await
using MementoTest.UI;
using MementoTest.Core;
using MementoTest.Resources;

namespace MementoTest.Entities
{
	public partial class PlayerController : CharacterBody2D
	{
		[Export] public int MaxHP = 100;
		[Export] public int MaxAP = 10;
		[Export] public int TypoPenaltyAP = 2;
		[Export] public Godot.Collections.Array<PlayerSkill> SkillList;


		private int _currentHP;
		private MapManager _mapManager;
		private MementoTest.Core.TurnManager _turnManager;
		private int _currentAP;
		private bool _isMoving = false;
		private Vector2I _currentGridPos;

		// Kita tetap pakai Dictionary ini untuk pencarian cepat (internal)
		private Dictionary<string, (int Cost, int Damage)> _skillDatabase;

		private BattleHUD _hud;
		private EnemyController _targetEnemy;

		public override void _Ready()
		{
			base._Ready();

			// --- BAGIAN BARU: KONVERSI INSPECTOR KE DICTIONARY ---
			_skillDatabase = new Dictionary<string, (int, int)>();

			if (SkillList != null)
			{
				foreach (var skill in SkillList)
				{
					// Masukkan data dari Inspector ke Dictionary
					// Kita pakai ToLower() agar player bisa ngetik huruf besar/kecil bebas
					_skillDatabase[skill.CommandName.ToLower()] = (skill.ApCost, skill.Damage);
				}
			}

			_currentHP = MaxHP;
			GD.Print($"Player HP Initialized: {_currentHP}/{MaxHP}");
			// -----------------------------------------------------

			// Setup lainnya tetap sama...
			if (GetParent().HasNode("MapManager"))
			{
				_mapManager = GetParent().GetNode<MapManager>("MapManager");
				_currentGridPos = _mapManager.GetGridCoordinates(GlobalPosition);
				GlobalPosition = _mapManager.GetSnappedWorldPosition(GlobalPosition);
			}

			if (GetParent().HasNode("TurnManager"))
			{
				_turnManager = GetParent().GetNode<MementoTest.Core.TurnManager>("TurnManager");
				_turnManager.PlayerTurnStarted += OnPlayerTurnStart;
			}

			_currentAP = MaxAP;

			if (GetParent().HasNode("BattleHUD"))
			{
				_hud = GetParent().GetNode<BattleHUD>("BattleHUD");
				_hud.CommandSubmitted += ExecuteCombatCommand;
				_hud.UpdateAP(_currentAP, MaxAP);
			}
		}
		// Logic mengecek apakah boleh jalan ke sana?
		private async void TryMoveToTile(Vector2 mousePos)
		{
			// Cegah gerak kalau sedang animasi jalan
			if (_isMoving) return;

			Vector2I targetGrid = _mapManager.GetGridCoordinates(mousePos);

			// Aturan 1: Hanya boleh jalan ke tetangga (jarak 1 hex)
			if (!_mapManager.IsNeighbor(_currentGridPos, targetGrid))
			{
				GD.Print("Terlalu jauh!");
				return;
			}

			// Aturan 2: Cek apakah tile bisa diinjak (Air/Gunung)
			if (!_mapManager.IsTileWalkable(targetGrid)) return;

			// Aturan 3: Cek apakah ada unit lain di sana
			if (_mapManager.IsTileOccupied(targetGrid)) return;

			// Aturan 4: Cek AP (Biaya Jalan = 1 AP)
			int moveCost = 1;
			if (_currentAP >= moveCost)
			{
				// Kurangi AP
				_currentAP -= moveCost;
				if (_hud != null) _hud.UpdateAP(_currentAP, MaxAP);

				// Lakukan Gerakan
				await MoveToGrid(targetGrid);
			}
			else
			{
				// Beri tahu player AP habis
				if (_hud != null) _hud.LogToTerminal("ERROR: NOT ENOUGH AP TO MOVE!", Colors.Red);
			}
		}

		// Logic Animasi Gerak (Tween)
		private async System.Threading.Tasks.Task MoveToGrid(Vector2I targetGrid)
		{
			_isMoving = true;

			// Ubah koordinat grid menjadi posisi dunia nyata (Pixel)
			Vector2 targetWorldPos = _mapManager.MapToLocal(targetGrid);

			// Buat animasi jalan
			Tween tween = CreateTween();
			tween.TweenProperty(this, "global_position", targetWorldPos, 0.3f); // 0.3 detik

			await ToSignal(tween, "finished");

			_currentGridPos = targetGrid;
			_isMoving = false;
		}
		private void OnPlayerTurnStart()
		{
			_currentAP = MaxAP;
			if (_hud != null) _hud.UpdateAP(_currentAP, MaxAP);

			// Log info
			if (_hud != null) _hud.LogToTerminal("--- SYSTEM REBOOTED. AP RESTORED. ---", Colors.Cyan);

			if (_targetEnemy != null && GodotObject.IsInstanceValid(_targetEnemy))
			{
				// Buka Panel Combat Otomatis
				_hud.ShowCombatPanel(true);
				_hud.LogToTerminal($"> AUTO-RECONNECT: {_targetEnemy.Name}", Colors.Yellow);
				_hud.LogToTerminal("> READY FOR INPUT...", Colors.White);
			}
			else
			{
				// Kalau musuh sudah mati saat giliran musuh (misal kena counter attack), reset.
				_targetEnemy = null;
			}
		}

		// Logic Input Mouse untuk Memilih Target
		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
			{
				// Cek apakah kita klik musuh?
				CheckEnemyClick();
			}
		}

		private void CheckEnemyClick()
		{
			Vector2 mousePos = GetGlobalMousePosition();
			var spaceState = GetWorld2D().DirectSpaceState;

			var query = new PhysicsPointQueryParameters2D();
			query.Position = mousePos;
			query.CollideWithBodies = true;
			query.CollideWithAreas = true;

			var result = spaceState.IntersectPoint(query);

			if (result.Count > 0)
			{
				Node collider = (Node)result[0]["collider"];
				if (collider is EnemyController enemy)
				{
					// KENA MUSUH -> Mode Combat
					_targetEnemy = enemy;
					_hud.ShowCombatPanel(true);
					_hud.LogToTerminal($"> TARGET LOCKED: {enemy.Name}", Colors.Yellow);
				}
			}
			else
			{
				// KENA TANAH KOSONG -> Mode Jalan
				_targetEnemy = null;
				if (_hud != null) _hud.ShowCombatPanel(false);

				// --- TAMBAHAN PENTING: PANGGIL FUNGSI GERAK ---
				TryMoveToTile(mousePos);
			}
		}

		// FUNGSI UTAMA: Menerima teks dari UI dan memprosesnya
		private async void ExecuteCombatCommand(string command)
		{
			if (_targetEnemy == null) return;

			command = command.ToLower().Trim();

			// SKENARIO 1: Command Ditemukan (Sukses)
			if (_skillDatabase.ContainsKey(command))
			{
				var skill = _skillDatabase[command];
				int apCost = skill.Cost;
				int damage = skill.Damage;

				if (_currentAP >= apCost)
				{
					// Logic Sukses
					_currentAP -= apCost;
					if (_hud != null) _hud.UpdateAP(_currentAP, MaxAP);

					_hud.LogToTerminal($"> EXECUTING '{command}'...", Colors.Green);
					_targetEnemy.TakeDamage(damage);

					// Selesaikan Sesi Combat
					await EndCombatSession("SUCCESS: PROCESS COMPLETED.");
				}
				else
				{
					// AP Kurang (Tidak ganti giliran, cuma warning)
					_hud.LogToTerminal($"> ERROR: Insufficient AP. Need {apCost}.", Colors.Red);
				}
			}
			// SKENARIO 2: Typo / Command Tidak Ada (GAGAL TOTAL)
			else
			{
				// 1. Kurangi AP sebagai denda
				_currentAP -= TypoPenaltyAP;

				// Pastikan AP tidak minus
				if (_currentAP < 0) _currentAP = 0;

				if (_hud != null) _hud.UpdateAP(_currentAP, MaxAP);

				// 2. Beri pesan Error yang menyeramkan
				_hud.LogToTerminal($"> SYNTAX ERROR: Command '{command}' unknown.", Colors.Red);
				_hud.LogToTerminal($"> PENALTY APPLIED: -{TypoPenaltyAP} AP.", Colors.Orange);

				// 3. Paksa Akhiri Giliran
				await EndCombatSession("CRITICAL FAILURE: SYSTEM HALTED.");
			}
		}

		// Helper Function untuk menutup sesi dan ganti giliran (Biar rapi)
		private async System.Threading.Tasks.Task EndCombatSession(string endMessage)
		{
			_hud.LogToTerminal($"> {endMessage}", Colors.Gray);
			_hud.LogToTerminal("> TERMINATING SESSION...", Colors.Gray);

			await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

			_hud.ShowCombatPanel(false); // Panel tetap kita tutup agar bisa lihat animasi musuh

			if (_turnManager != null)
			{
				_turnManager.ForceEndPlayerTurn();
			}
		}

		public void TakeDamage(int damage)
		{
			_currentHP -= damage;
			GD.Print($"WARNING: Player took {damage} damage! HP: {_currentHP}/{MaxHP}");

			// Efek berkedip merah (Visual Feedback)
			Modulate = Colors.Red;
			CreateTween().TweenProperty(this, "modulate", Colors.White, 0.2f);

			if (_currentHP <= 0)
			{
				Die();
			}
		}

		private void Die()
		{
			GD.Print("GAME OVER: SYSTEM FAILURE.");
			
			SetPhysicsProcess(false); // Matikan player
		}
	}
}