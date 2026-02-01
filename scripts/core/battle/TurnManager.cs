using Godot;
using MementoTest.UI;
using System.Collections.Generic;
using MementoTest.Entities; // Import namespace Entity// Pastikan namespace UI di-include

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

		public async void StartEnemyTurn()
		{
			CurrentTurn = TurnState.Enemy;
			GD.Print("--- ENEMY PHASE START ---");
			if (_battleHUD != null) _battleHUD.UpdateTurnLabel("ENEMY TURN");

			// 1. Cari semua musuh yang hidup di scene
			// Kita cari child dari parent yang sama (biasanya root scene) yang tipe-nya EnemyController
			var allNodes = GetParent().GetChildren();
			List<EnemyController> enemies = new List<EnemyController>();

			foreach (var node in allNodes)
			{
				if (node is EnemyController enemy && !enemy.IsQueuedForDeletion())
				{
					enemies.Add(enemy);
				}
			}

			// 2. Eksekusi satu per satu (Sequential)
			if (enemies.Count > 0)
			{
				foreach (var enemy in enemies)
				{
					// Tunggu musuh selesai beraksi baru lanjut ke musuh berikutnya
					await enemy.ExecuteTurn();

					// Jeda sedikit antar musuh biar enak dilihat
					await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
				}
			}
			else
			{
				GD.Print("Tidak ada musuh tersisa! You Win? (Logic win nanti)");
				await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
			}

			// 3. Kembalikan giliran ke Player
			StartPlayerTurn();
		}


	}
}