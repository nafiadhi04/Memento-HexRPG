using Godot;
using MementoTest.UI; // Pastikan namespace UI di-include

namespace MementoTest.Core
{
	public partial class TurnManager : Node
	{
		[Signal] public delegate void PlayerTurnStartedEventHandler();
		[Signal] public delegate void EnemyTurnStartedEventHandler();

		public enum TurnState { Player, Enemy }
		public TurnState CurrentTurn { get; private set; }

		private BattleHUD _battleHUD;
		
		public override void _Ready()
		{
			if (GetParent().HasNode("BattleHUD"))
			{
				_battleHUD = GetParent().GetNode<BattleHUD>("BattleHUD");
				_battleHUD.EndTurnRequested += OnEndTurnPressed;
			}

			CallDeferred(MethodName.StartPlayerTurn);
		}

		private void StartPlayerTurn()
		{
			CurrentTurn = TurnState.Player;

			// Update Tampilan lewat HUD
			if (_battleHUD != null)
			{
				_battleHUD.SetEndTurnButtonInteractable(true);
				_battleHUD.UpdateTurnLabel("PLAYER PHASE");
			}

			EmitSignal(SignalName.PlayerTurnStarted);
		}

		public void ForceEndPlayerTurn()
		{
			// Kita panggil logika yang sama dengan saat tombol ditekan
			OnEndTurnPressed();
		}

		private void OnEndTurnPressed()
		{
			if (CurrentTurn != TurnState.Player) return;

			StartEnemyTurn();
		}

		private async void StartEnemyTurn()
		{
			CurrentTurn = TurnState.Enemy;

			// Update Tampilan lewat HUD
			if (_battleHUD != null)
			{
				_battleHUD.SetEndTurnButtonInteractable(false);
				_battleHUD.UpdateTurnLabel("ENEMY PHASE");
			}

			EmitSignal(SignalName.EnemyTurnStarted);

			var enemies = GetTree().GetNodesInGroup("Enemy");
			foreach (Node2D enemyNode in enemies)
			{
				if (enemyNode is MementoTest.Entities.EnemyController enemy)
				{
					await enemy.DoTurnAction();
					await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
				}
			}

			StartPlayerTurn();
		}


	}
}