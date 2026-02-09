using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MementoTest.UI;
using MementoTest.Core;
using MementoTest.Resources;
using System.Linq;

namespace MementoTest.Entities
{
	public partial class PlayerController : CharacterBody2D
	{
		[ExportCategory("Player Stats")]
		[Export] public int MaxHP = 100;
		[Export] public int MaxAP = 10;
		[Export] public int TypoPenaltyAP = 2;
		[Export] public int SuccessRegenAP = 3;
		[Export] public int TurnStartBaseRegen = 2;

		[ExportCategory("Combat Data")]
		[Export] public Godot.Collections.Array<PlayerSkill> SkillList;
		[Export] public PackedScene DamagePopupScene;

		[Export] public AnimatedSprite2D Sprite;
		[Export] private PackedScene BowProjectileScene;
		[Export] private PackedScene MagicProjectileScene;
		[Export]
		private PackedScene _projectileScene;




		// State Variables
		private int _currentHP;
		public int CurrentHP => _currentHP;

		private int _currentAP;
		private bool _isMoving = false;
		private bool _isAttacking = false;
		private Vector2I _currentGridPos;
		private bool _isTargetLocked = false;
		private Vector2 _lockedLookDir = Vector2.Right;

		// References
		private MapManager _mapManager;
		private TurnManager _turnManager;
		private BattleHUD _hud;
		private EnemyController _targetEnemy;
		private Tween _activeTween;
		private Dictionary<string, PlayerSkill> _skillDatabase;
		private Vector2 _lastLookDir = Vector2.Down;
		private bool _isHurt = false;
		Vector2 _moveDir;
		Vector2 _lookDir;
		private Vector2 _facingDir = Vector2.Down; // default
		private bool _lockFacing = false;
		private Vector2 _lockedAttackDir;






		public override void _Ready()
		{
			base._Ready();

			// [FIX VISUAL] Paksa Player selalu di atas lantai & selector
			ZIndex = 5;

			AddToGroup("Player");

			// Setup Database Skill
			_skillDatabase = new Dictionary<string, PlayerSkill>();

			foreach (var skill in SkillList)
			{
				_skillDatabase[skill.CommandName.ToLower()] = skill;
			}


			_currentHP = MaxHP;
			_currentAP = MaxAP;

			// Setup References
			if (GetParent().HasNode("MapManager"))
				_mapManager = GetParent().GetNode<MapManager>("MapManager");

			if (GetParent().HasNode("TurnManager"))
			{
				_turnManager = GetParent().GetNode<TurnManager>("TurnManager");
				_turnManager.PlayerTurnStarted += OnPlayerTurnStart;
			}

			if (GetParent().HasNode("BattleHUD"))
			{
				_hud = GetParent().GetNode<BattleHUD>("BattleHUD");
				_hud.CommandSubmitted += ExecuteCombatCommand;
				_hud.UpdateAP(_currentAP, MaxAP);
			}



			// Snap posisi awal (Tunggu 1 frame biar aman)
			CallDeferred(nameof(SnapToNearestGrid));
		}

		public override void _Process(double delta)
		{
			if (_isMoving || _isAttacking || _isHurt)
				return;

	if (_lockFacing)
    {
        _lookDir = _lockedAttackDir;
    }
			if (_isTargetLocked && !GodotObject.IsInstanceValid(_targetEnemy))
			{
				_isTargetLocked = false;
				_targetEnemy = null;
			}

			if (_isTargetLocked && GodotObject.IsInstanceValid(_targetEnemy))
			{
				Vector2 rawDir = _targetEnemy.GlobalPosition - GlobalPosition;
				_lookDir = QuantizeDirection8(rawDir);
			}
			else
			{
				Vector2 rawDir = GetGlobalMousePosition() - GlobalPosition;
				_lookDir = QuantizeDirection8(rawDir);
			}
			_facingDir = _lookDir;

			string suffix = GetDirectionSuffix(_lookDir);
			_lastLookDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();

			string idleAnim = $"idle_{suffix}";

			if (Sprite.Animation != idleAnim)
				Sprite.Play(idleAnim);
		}





		private void SnapToNearestGrid()
		{
			if (MapManager.Instance == null) return;

			// Update posisi Grid & World
			_currentGridPos = MapManager.Instance.WorldToGrid(GlobalPosition);
			GlobalPosition = MapManager.Instance.GridToWorld(_currentGridPos);

			GD.Print($"[PLAYER] Ready at Grid: {_currentGridPos}");
		}

		private Vector2 QuantizeDirection8(Vector2 dir)
		{
			if (dir == Vector2.Zero)
				return Vector2.Down;

			dir = dir.Normalized();

			float angle = Mathf.Atan2(dir.Y, dir.X);
			float step = Mathf.Pi / 4f; // 45 derajat
			angle = Mathf.Round(angle / step) * step;

			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized();
		}


		// --- INPUT HANDLING (SATPAM) ---
		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
			{
				// 1. VALIDASI DASAR (Sesuai kode lama Anda)
				if (_turnManager?.CurrentTurn != TurnManager.TurnState.Player) return;
				if (_isMoving || _isAttacking) return;
				if (_currentHP <= 0) return;

				Vector2 mousePos = GetGlobalMousePosition();

				// 2. PRIORITAS 1: CEK ENEMY DULU!
				// Jika kita berhasil mengklik musuh, hentikan proses (return true)
				if (TryHandleEnemyClick(mousePos))
				{
					// Bersihkan highlight gerakan jika kita memilih musuh
					_mapManager?.ClearMovementOptions();
					return;
				}

				// 3. PRIORITAS 2: LOGIKA GRID (Visualisasi & Gerak)
				if (_mapManager == null) return;
				Vector2I clickedGrid = _mapManager.GetGridCoordinates(mousePos);

				GD.Print($"[INPUT] Clicked Grid: {clickedGrid}");

				// A. Klik Diri Sendiri -> Tampilkan Range
				if (clickedGrid == _currentGridPos)
				{
					_mapManager.ShowNeighborHighlight(_currentGridPos);
				}
				// B. Klik Tetangga Valid -> Bergerak
				else if (_mapManager.IsNeighbor(_currentGridPos, clickedGrid))
				{
					if (_mapManager.IsTileWalkable(clickedGrid) && !_mapManager.IsTileOccupied(clickedGrid))
					{
						TryMoveToTile(mousePos);
					}
					else
					{
						_hud?.LogToTerminal("Cannot move there!", Colors.Red);
					}
				}
				// C. Klik Sembarang -> Hilangkan Highlight
				else
				{
					_mapManager.ClearHighlight();
					// Jika klik tanah kosong jauh, kita juga bisa unlock target (opsional)
					// _isTargetLocked = false; 
					// _targetEnemy = null;
				}
			}
		}

		private bool TryHandleEnemyClick(Vector2 mousePos)
		{
			var spaceState = GetWorld2D().DirectSpaceState;

			var query = new PhysicsPointQueryParameters2D
			{
				Position = mousePos,
				CollideWithBodies = true,
				CollideWithAreas = true
			};

			var result = spaceState.IntersectPoint(query);

			if (result.Count > 0)
			{
				// Ambil objek teratas
				Node collider = (Node)result[0]["collider"];

				// Cek apakah itu Enemy
				if (collider is EnemyController enemy)
				{
					// LOGIKA LOCK / UNLOCK TARGET
					if (_isTargetLocked && _targetEnemy == enemy)
					{
						// Klik enemy yang sama -> Unlock
						_isTargetLocked = false;
						_targetEnemy = null;
						_hud?.ShowCombatPanel(false);
						_hud?.LogToTerminal("> TARGET UNLOCKED", Colors.Gray);
					}
					else
					{
						// Lock target baru
						_targetEnemy = enemy;
						_isTargetLocked = true;

						// Update arah pandang player ke musuh
						_lookDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
						_facingDir = _lookDir; // Paksa update visual

						_hud?.ShowCombatPanel(true);
						_hud?.LogToTerminal($"> TARGET LOCKED: {enemy.Name}", Colors.Yellow);
					}

					return true; // Enemy ditemukan dan diproses!
				}
			}

			return false; // Tidak ada enemy yang diklik
		}
		public IEnumerable<PlayerSkill> GetAvailableSkills()
		{

			return SkillList;
		}

		public IEnumerable<string> GetAvailableSkillCommands()
		{
			return SkillList
				.Where(skill => skill != null)
				.Select(skill => skill.CommandName.ToLower());
		}

		private string GetDirectionSuffix(Vector2 dir)
		{
			if (dir.Y < -0.5f)
			{
				if (dir.X < -0.5f) return "up_left";
				if (dir.X > 0.5f) return "up_right";
				return "up";
			}
			else if (dir.Y > 0.5f)
			{
				if (dir.X < -0.5f) return "down_left";
				if (dir.X > 0.5f) return "down_right";
				return "down";
			}
			else
			{
				if (dir.X < 0) return "left";
				return "right";
			}
		}

		private string GetDirectionSuffixCombat(Vector2 dir)
		{
			dir = dir.Normalized();

			const float DIAG = 0.25f; // 🔑 lebih kecil dari 0.5

			if (dir.Y < -DIAG)
			{
				if (dir.X < -DIAG) return "up_left";
				if (dir.X > DIAG) return "up_right";
				return "up";
			}
			else if (dir.Y > DIAG)
			{
				if (dir.X < -DIAG) return "down_left";
				if (dir.X > DIAG) return "down_right";
				return "down";
			}
			else
			{
				if (dir.X < 0) return "left";
				return "right";
			}
		}

		private void CheckEnemyClick()
		{
			Vector2 mousePos = GetGlobalMousePosition();
			var spaceState = GetWorld2D().DirectSpaceState;

			var query = new PhysicsPointQueryParameters2D
			{
				Position = mousePos,
				CollideWithBodies = true,
				CollideWithAreas = true
			};

			var result = spaceState.IntersectPoint(query);

			// ============================
			// 1. KLIK ENEMY
			// ============================
			if (result.Count > 0)
			{
				Node collider = (Node)result[0]["collider"];


				if (collider is EnemyController enemy)
				{
					// Klik enemy yang sama → UNLOCK
					if (_isTargetLocked && _targetEnemy == enemy)
					{
						_isTargetLocked = false;
						_targetEnemy = null;
						_hud?.ShowCombatPanel(false);

						_hud?.LogToTerminal("> TARGET UNLOCKED", Colors.Gray);
						return;
					}

					// Lock / ganti target
					_targetEnemy = enemy;
					_isTargetLocked = true;

					_lookDir = (enemy.GlobalPosition - GlobalPosition).Normalized();


					_hud?.ShowCombatPanel(true);
					_hud?.LogToTerminal($"> TARGET LOCKED: {enemy.Name}", Colors.Yellow);
					return;
				}
			}

			// ============================
			// 2. KLIK TILE / TANAH KOSONG
			// ============================
			// ⚠️ JANGAN unlock target di sini
			// Player tetap bisa jalan meskipun lock

			TryMoveToTile(mousePos);
		}


		// --- MOVEMENT LOGIC ---
		// --- MOVEMENT LOGIC ---
		private async void TryMoveToTile(Vector2 mousePos)
		{
			if (_mapManager == null) return;

			Vector2I targetGrid = _mapManager.GetGridCoordinates(mousePos);

			// Validasi Gerak
			if (!_mapManager.IsNeighbor(_currentGridPos, targetGrid)) return;
			if (!_mapManager.IsTileWalkable(targetGrid)) return;
			if (_mapManager.IsTileOccupied(targetGrid)) return;

			int moveCost = 1;
			if (_currentAP >= moveCost)
			{
				_currentAP -= moveCost;
				_hud?.UpdateAP(_currentAP, MaxAP);

				await MoveToGrid(targetGrid);
			}
			else
			{
				_hud?.LogToTerminal("ERROR: NOT ENOUGH AP!", Colors.Red);
			}
		}

		private async Task MoveToGrid(Vector2I targetGrid)
		{
			_mapManager?.ClearHighlight();
			_mapManager?.ClearMovementOptions();
			if (_isMoving) return;
			_isMoving = true;

			Vector2 targetWorldPos = MapManager.Instance.GridToWorld(targetGrid);
			Vector2 dir = (targetWorldPos - GlobalPosition).Normalized();


			string suffix = GetDirectionSuffix(dir);
			Sprite.Play($"walk_{suffix}");

			// Setup Tween
			if (_activeTween != null && _activeTween.IsValid()) _activeTween.Kill();

			_activeTween = CreateTween();

			// Tween ke Global Position yang sudah benar
			_activeTween.TweenProperty(this, "global_position", targetWorldPos, 0.3f)
						.SetTrans(Tween.TransitionType.Sine)
						.SetEase(Tween.EaseType.Out);

			await ToSignal(_activeTween, "finished");
			Sprite.Play($"idle_{suffix}");
			_isMoving = false;

			_currentGridPos = targetGrid;
			_isMoving = false;

			// [SAFETY CHECK]
			// Kadang animasi meleset 0.001 pixel, kita paksa snap lagi di akhir gerakan biar sempurna
			GlobalPosition = targetWorldPos;
		}

		// --- COMBAT LOGIC ---
		private async void ExecuteCombatCommand(string command)
		{
			_mapManager?.ClearMovementOptions();
			// Validasi Target
			if (_targetEnemy == null || !GodotObject.IsInstanceValid(_targetEnemy))
			{
				_hud?.LogToTerminal("ERROR: NO VALID TARGET.", Colors.Red);
				return;
			}

			// Validasi Giliran (Double check)
			if (_turnManager.CurrentTurn != TurnManager.TurnState.Player) return;

			command = command.ToLower().Trim();

			// SKENARIO SUKSES (Skill Ditemukan)
			if (_skillDatabase.ContainsKey(command))
			{
				var skill = _skillDatabase[command];

				if (_currentAP >= skill.ApCost)
				{
					// 1. Kurangi AP & Regen
					_currentAP -= skill.ApCost;
					_currentAP = Math.Min(_currentAP + SuccessRegenAP, MaxAP); // Regen + Cap Max
					_hud?.UpdateAP(_currentAP, MaxAP);

					_hud?.LogToTerminal($"> EXECUTING '{command.ToUpper()}'...", Colors.Green);

					// 2. [SCORING] Tambah Skor & Combo!
					// Skor dasar 100 per serangan sukses (bisa diubah sesuai damage skill)
					ScoreManager.Instance?.AddScore(100);

					// 3. Eksekusi Serangan (Animasi & Damage)
					await PerformAttackAnimation(skill.Damage, skill.AttackType);

				}
				else
				{
					_hud?.LogToTerminal($"ERROR: NEED {skill.ApCost} AP.", Colors.Red);
					// (Opsional) Tidak reset combo kalau cuma kurang AP, tapi terserah desain game-mu
				}
			}
			// SKENARIO TYPO / GAGAL (Skill Tidak Ditemukan)
			else
			{
				// 1. Hukuman AP
				_currentAP = Math.Max(0, _currentAP - TypoPenaltyAP);
				_hud?.UpdateAP(_currentAP, MaxAP);

				// 2. [SCORING] Reset Combo karena Typo!
				ScoreManager.Instance?.ResetCombo();

				_hud?.LogToTerminal($"> SYNTAX ERROR: '{command}'", Colors.Red);

				await EndCombatSession("SYSTEM HALTED.");
			}
		}

		private async Task PerformAttackAnimation(int damage, AttackType attackType)
		{
			if (_isAttacking) return;
			_isAttacking = true;

			Vector2 origin = GlobalPosition;

			// ==============================
			// TENTUKAN ARAH SERANG
			// ==============================
			Vector2 attackDir;
			if (_isTargetLocked && GodotObject.IsInstanceValid(_targetEnemy))
			{
				attackDir = (_targetEnemy.GlobalPosition - GlobalPosition).Normalized();
			}
			else
			{
				attackDir = _lookDir;
			}

			// 🔑 Sinkronkan arah look
			_lookDir = attackDir;

			string suffix = GetDirectionSuffix(attackDir);

			// Hentikan tween lama (safety)
			if (_activeTween != null && _activeTween.IsValid())
				_activeTween.Kill();

			// ==============================
			// MELEE / UNARMED
			// ==============================
			if (attackType == AttackType.Unarmed || attackType == AttackType.Melee)
			{
				string animName =
					attackType == AttackType.Melee
						? $"attack_melee_{suffix}"
						: $"attack_unarmed_{suffix}";

				if (Sprite.SpriteFrames.HasAnimation(animName))
					Sprite.Play(animName);
				else
					GD.PrintErr($"[ANIM] Missing: {animName}");

				Vector2 lungePos = origin + attackDir * 40f;

				_activeTween = CreateTween();

				// ancang-ancang
				_activeTween.TweenProperty(
					this,
					"global_position",
					origin - attackDir * 10f,
					0.08f
				);

				// maju hit
				_activeTween.TweenProperty(
					this,
					"global_position",
					lungePos,
					0.12f
				)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);

				await ToSignal(_activeTween, Tween.SignalName.Finished);

				if (GodotObject.IsInstanceValid(_targetEnemy))
					_targetEnemy.TakeDamage(damage);

				// balik ke posisi awal
				_activeTween = CreateTween();
				_activeTween.TweenProperty(this, "global_position", origin, 0.18f);
				await ToSignal(_activeTween, Tween.SignalName.Finished);
			}
			// ==============================
			// RANGED (BOW / MAGIC)
			// ==============================
			else
			{
				Vector2 rangedDir = _facingDir.Normalized();
				string rangedSuffix = GetDirectionSuffix(rangedDir);

				string animName =
					attackType == AttackType.RangedBow
						? $"attack_bow_{rangedSuffix}"
						: $"attack_magic_{rangedSuffix}";

				if (Sprite.SpriteFrames.HasAnimation(animName))
				{
					Sprite.Play(animName);
					await ToSignal(Sprite, AnimatedSprite2D.SignalName.AnimationFinished);
				}
				else
				{
					GD.PrintErr($"[ANIM] Missing: {animName}");
				}

				SpawnProjectile(attackType, damage, rangedDir);
			}



			// ==============================
			// KEMBALI KE IDLE
			// ==============================
			string idleAnim = $"idle_{suffix}";
			if (Sprite.SpriteFrames.HasAnimation(idleAnim))
				Sprite.Play(idleAnim);

			_isAttacking = false;

			await EndCombatSession("TARGET HIT.");
		}



		private void SpawnProjectile(AttackType attackType, int damage, Vector2 dir)
		{
			PackedScene scene =
				attackType == AttackType.RangedBow
					? BowProjectileScene
					: MagicProjectileScene;

			if (scene == null)
			{
				GD.PrintErr("[PROJECTILE] Scene belum di-assign");
				return;
			}

			var projectile = scene.Instantiate<Projectile>();

			projectile.GlobalPosition = GlobalPosition;
			GetTree().CurrentScene.AddChild(projectile);

			projectile.Init(
				damage,
				dir,
				_isTargetLocked ? _targetEnemy : null
			);
		}




		private void FaceDirection(Vector2 dir)
		{
			if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
			{
				Sprite.FlipH = dir.X < 0;
			}
		}
		private void UpdateLookDirection()
		{
			if (_isTargetLocked && GodotObject.IsInstanceValid(_targetEnemy))
			{
				_lookDir = (_targetEnemy.GlobalPosition - GlobalPosition).Normalized();
			}
			else
			{
				_lookDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
			}
		}




		private async Task EndCombatSession(string message)
		{
			_mapManager?.ClearMovementOptions();

			_hud?.LogToTerminal($"> {message}", Colors.Gray);
			await ToSignal(GetTree().CreateTimer(1.0f), "timeout"); // Delay biar kebaca

			_hud?.ShowCombatPanel(false);

			// Akhiri Giliran Player
			_turnManager?.ForceEndPlayerTurn();
		}

		// --- TURN & STATUS ---

		private void OnPlayerTurnStart()
		{
			_currentAP = Math.Min(_currentAP + TurnStartBaseRegen, MaxAP);
			_hud?.UpdateAP(_currentAP, MaxAP);
			_hud?.LogToTerminal($"--- AP RECHARGED (+{TurnStartBaseRegen}) ---", Colors.Cyan);

			_mapManager?.ClearMovementOptions();

			// Re-open combat panel kalau masih ada target
			if (_targetEnemy != null && GodotObject.IsInstanceValid(_targetEnemy))
			{
				// Jika masih ada musuh -> Buka Panel Combat & Ambil Fokus Keyboard
				if (_hud != null)
				{
					_hud.ShowCombatPanel(true);

					// [FIX BUG ENTER] Panggil fungsi yang baru kita buat
					_hud.EnablePlayerInput();
				}
			}
			else
			{
				// Jika tidak ada musuh -> Tutup Panel
				_targetEnemy = null;
				if (_hud != null) _hud.ShowCombatPanel(false);
			}
		}

		public void TakeDamage(int damage)
		{
			// === PARRY / BLOCK / MISS ===
			if (damage <= 0)
			{
				ShowDamagePopup(0); // optional (misal "PARRY")
				return; // ⛔ STOP di sini
			}

			// === DAMAGE VALID ===
			_currentHP -= damage;
			ShowDamagePopup(damage);

			_hud?.UpdateHP(_currentHP, MaxHP);

			PlayHurtAnimation();

			Modulate = Colors.Red;
			CreateTween().TweenProperty(this, "modulate", Colors.White, 0.3f);

			ScoreManager.Instance.ResetCombo();

			if (_currentHP <= 0)
				Die();
		}

		private async void PlayHurtAnimation()
		{
			if (_isHurt || _isAttacking) return;
			
	

			_isHurt = true;

			// 🔑 PAKAI ARAH HADAP TERAKHIR
			string suffix = GetDirectionSuffix(_lastLookDir);
			string animName = $"hurt_{suffix}";

			if (Sprite.SpriteFrames.HasAnimation(animName))
			{
				Sprite.Play(animName);
				await ToSignal(Sprite, AnimatedSprite2D.SignalName.AnimationFinished);
			}
			else
			{
				GD.PrintErr($"[ANIM ERROR] Missing hurt animation: {animName}");
			}

			_isHurt = false;

			// Balik ke idle sesuai arah terakhir
			string idleAnim = $"idle_{suffix}";
			if (Sprite.SpriteFrames.HasAnimation(idleAnim))
				Sprite.Play(idleAnim);
		}


		private void ShowDamagePopup(int amount)
		{
			if (DamagePopupScene == null) return;

			var popup = DamagePopupScene.Instantiate<DamagePopup>();
			AddChild(popup);

			popup.SetupAndAnimate(
				amount,
				GlobalPosition + new Vector2(0, -30),
				Colors.Red
			);
		}

		private void Die()
		{
			GD.Print("GAME OVER");
			SetPhysicsProcess(false);
			SetProcessInput(false); // Matikan input
		}


	}


}