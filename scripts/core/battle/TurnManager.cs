using Godot;
using MementoTest.UI;
using System.Collections.Generic;
using System.Threading.Tasks; // Perlu untuk Task
using MementoTest.Entities;

namespace MementoTest.Core
{
	public partial class TurnManager : Node
	{
		[Signal] public delegate void PlayerTurnStartedEventHandler();
		[Signal] public delegate void EnemyTurnStartedEventHandler();

		public enum TurnState { Player, Enemy }
		public TurnState CurrentTurn { get; private set; }

		private BattleHUD _battleHUD;

		// Referensi ke Player untuk cek jarak (Opsional tapi disarankan)
		private PlayerController _player;
		private bool _enemyPhaseRunning = false;


		public override void _Ready()
		{
			_battleHUD = GetTree().GetFirstNodeInGroup("HUD") as BattleHUD;

			var players = GetTree().GetNodesInGroup("Player");
			if (players.Count > 0)
				_player = players[0] as PlayerController;

			CallDeferred(MethodName.StartPlayerTurn);
		}

		private void StartPlayerTurn()
		{
			if (_player != null && _player.IsDead)
				return;

			CurrentTurn = TurnState.Player;

			if (_battleHUD != null)
			{
				// 🔥 Hanya tampil jika lock enemy
				if (_player != null && _player.IsTargetLocked)
				{
					_battleHUD.UpdateTurnLabel("PLAYER PHASE");
					_battleHUD.SetTurnLabelVisible(true);
				}
				else
				{
					_battleHUD.SetTurnLabelVisible(false);
				}

				_battleHUD.SetEndTurnButtonInteractable(true);
				_battleHUD.EnterPlayerCommandPhase(
					_player.GetAvailableSkillCommands()
				);
			}

			EmitSignal(SignalName.PlayerTurnStarted);
		}


		public void ForceEndPlayerTurn()
		{
			OnEndTurnPressed();
		}

		private void OnEndTurnPressed()
		{
			if (CurrentTurn != TurnState.Player) return;
			if (_player != null && _player.IsDead) return;
			StartEnemyTurn();
		}


		public async void StartEnemyTurn()
		{
			if (_enemyPhaseRunning) return;
			_enemyPhaseRunning = true;

			if (_player != null && _player.IsDead)
			{
				_enemyPhaseRunning = false;
				return;
			}

			CurrentTurn = TurnState.Enemy;

			if (_battleHUD != null)
			{
				_battleHUD.UpdateTurnLabel("ENEMY TURN");
				_battleHUD.SetTurnLabelVisible(true);
				_battleHUD.EnablePlayerInput(false); // ⛔ disable input saat enemy turn
			}

			var enemyNodes = GetTree().GetNodesInGroup("Enemy");

			bool anyEnemyActed = false;

			foreach (var node in enemyNodes)
			{
				if (node is EnemyController enemy)
				{
					if (!IsInstanceValid(enemy))
						continue;

					if (enemy.IsDead)
						continue;

					float dist = enemy.GlobalPosition.DistanceTo(_player.GlobalPosition);
					if (dist > 800)
						continue;

					anyEnemyActed = true;

					await enemy.ExecuteTurn();

					await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
				}
			}

			if (!anyEnemyActed)
				await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

			_battleHUD?.ExitPlayerCommandPhase();

			_enemyPhaseRunning = false;

			StartPlayerTurn(); // ✅ single source of truth
		}
		public void ForceReturnToPlayerTurn()
		{
			_enemyPhaseRunning = false;
			CurrentTurn = TurnState.Player;

			_battleHUD?.UpdateTurnLabel("PLAYER TURN");
			_battleHUD?.SetTurnLabelVisible(true);
			_battleHUD?.EnablePlayerInput(true);

			GD.Print("Force return to PLAYER TURN");
		}


	}
}