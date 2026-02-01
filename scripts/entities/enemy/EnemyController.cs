using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks; // [WAJIB] Tambahkan baris ini agar 'Task' dikenali!

using MementoTest.Core;

namespace MementoTest.Entities
{
	public partial class EnemyController : CharacterBody2D
	{
		[Export] public float MoveDuration = 0.3f;

		// Hapus variabel Timer lama jika masih ada
		// private Timer _moveTimer; 

		private MapManager _mapManager;
		private Vector2I _currentGridPos;
		private bool _isMoving = false;

		private readonly TileSet.CellNeighbor[] _hexNeighbors = {
			TileSet.CellNeighbor.TopSide,
			TileSet.CellNeighbor.BottomSide,
			TileSet.CellNeighbor.TopLeftSide,
			TileSet.CellNeighbor.TopRightSide,
			TileSet.CellNeighbor.BottomLeftSide,
			TileSet.CellNeighbor.BottomRightSide
		};

		public override void _Ready()
		{
			if (GetParent().HasNode("MapManager"))
			{
				_mapManager = GetParent().GetNode<MapManager>("MapManager");
				_currentGridPos = _mapManager.GetGridCoordinates(GlobalPosition);
				GlobalPosition = _mapManager.GetSnappedWorldPosition(GlobalPosition);
			}

			// [PENTING]
			// HAPUS atau COMMENT baris di bawah ini karena kita sudah ganti sistem ke TurnManager
			// SetupAITimer(); 

			AddToGroup("Enemy");
		}

		// Fungsi ini dipanggil oleh TurnManager.cs
		// Kata kunci 'async Task' membuat fungsi ini bisa ditunggu (await)
		public async Task DoTurnAction()
		{
			List<Vector2I> validMoves = GetValidNeighbors();

			if (validMoves.Count > 0)
			{
				int randomIndex = GD.RandRange(0, validMoves.Count - 1);
				Vector2I targetGrid = validMoves[randomIndex];

				// Tunggu sampai animasi jalan selesai baru lanjut
				await MoveToGrid(targetGrid);
			}
			else
			{
				// Jika macet, diam sebentar (0.5 detik) seolah-olah mikir
				await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
			}
		}

		private List<Vector2I> GetValidNeighbors()
		{
			List<Vector2I> neighbors = new List<Vector2I>();

			foreach (var direction in _hexNeighbors)
			{
				Vector2I neighborCell = _mapManager.GetNeighborCell(_currentGridPos, direction);

				// Cek 1: Apakah tanahnya bisa diinjak?
				if (_mapManager.IsTileWalkable(neighborCell))
				{
					// Cek 2: Apakah ada unit lain di sana? (Fitur baru)
					if (!_mapManager.IsTileOccupied(neighborCell))
					{
						neighbors.Add(neighborCell);
					}
				}
			}
			return neighbors;
		}

		private async Task MoveToGrid(Vector2I targetGrid)
		{
			_isMoving = true;

			Vector2 targetWorldPos = _mapManager.MapToLocal(targetGrid);

			Tween tween = CreateTween();
			tween.TweenProperty(this, "global_position", targetWorldPos, MoveDuration)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);

			// Tunggu sinyal 'finished' dari tween
			await ToSignal(tween, "finished");

			_currentGridPos = targetGrid;
			_isMoving = false;
		}
	}
}