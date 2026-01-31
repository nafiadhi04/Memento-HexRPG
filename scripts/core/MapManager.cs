using Godot;
using System;

namespace MementoTest.Core
{
	public partial class MapManager : TileMapLayer
	{
		public override void _Ready()
		{
			GD.Print("MapManager: Grid system initialized successfully.");
			// Bersihkan tile hantu sebelum game mulai sepenuhnya
			CleanInvalidTiles();

			GD.Print("MapManager: Ready and Cleaned.");
		}

		public Vector2 GetSnappedWorldPosition(Vector2 worldPos)
		{
			Vector2I mapCoords = LocalToMap(ToLocal(worldPos));
			return MapToLocal(mapCoords);
		}

		public Vector2I GetGridCoordinates(Vector2 worldPos)
		{
			return LocalToMap(ToLocal(worldPos));
		}

		// --- UPDATE PENTING DI SINI ---
		public bool IsTileWalkable(Vector2I mapCoords)
		{
			// 1. SAFETY CHECK (Pencegah Crash):
			// Cek apakah di koordinat ini benar-benar ada gambarnya?
			// Jika SourceId == -1, berarti kosong/void. Jangan dilanjut, langsung return false.
			if (GetCellSourceId(mapCoords) == -1)
			{
				return false;
			}

			TileData data = GetCellTileData(mapCoords);

			// 2. Double check jika data null
			if (data == null) return false;

			// 3. Ambil Custom Data
			Variant walkable = data.GetCustomData("is_walkable");

			// Cek tipe data sebelum konversi (Jaga-jaga jika lupa set di editor)
			if (walkable.VariantType == Variant.Type.Nil) return true; // Default true jika lupa set

			return walkable.AsBool();
		}

		// --- FITUR BARU: UNIT DETECTION ---
		/// <summary>
		/// Mengecek apakah ada Unit (Player/Enemy) yang sedang berdiri di tile ini.
		/// Mencegah unit saling tumpang tindih.
		/// </summary>
		public bool IsTileOccupied(Vector2I targetGridCoords)
		{
			// Ambil semua node dalam grup "Units" (Player & Enemy wajib masuk grup ini)
			// Atau cek grup "Player" dan "Enemy" terpisah
			var enemies = GetTree().GetNodesInGroup("Enemy");
			var players = GetTree().GetNodesInGroup("Player");

			foreach (Node2D enemy in enemies)
			{
				// Asumsi Enemy punya script yang mengekspos properti GridPosition
				// Tapi cara paling gampang: Cek posisi dunia yang sudah dikonversi
				Vector2I enemyGrid = GetGridCoordinates(enemy.GlobalPosition);
				if (enemyGrid == targetGridCoords) return true;
			}

			foreach (Node2D player in players)
			{
				Vector2I playerGrid = GetGridCoordinates(player.GlobalPosition);
				if (playerGrid == targetGridCoords) return true;
			}

			return false;
		}

		public bool IsNeighbor(Vector2I currentCoords, Vector2I targetCoords)
		{
			if (currentCoords == targetCoords) return false;

			TileSet.CellNeighbor[] neighbors = {
				TileSet.CellNeighbor.TopSide,
				TileSet.CellNeighbor.BottomSide,
				TileSet.CellNeighbor.TopLeftSide,
				TileSet.CellNeighbor.TopRightSide,
				TileSet.CellNeighbor.BottomLeftSide,
				TileSet.CellNeighbor.BottomRightSide
			};

			foreach (var side in neighbors)
			{
				if (GetNeighborCell(currentCoords, side) == targetCoords)
				{
					return true;
				}
			}
			return false;
		}
		private void CleanInvalidTiles()
		{
			var usedCells = GetUsedCells();
			int errorCount = 0;

			foreach (var cell in usedCells)
			{
				// Trik Debugging:
				// Coba ambil data tile. Jika null atau source ID-nya aneh, hapus.
				int sourceId = GetCellSourceId(cell);

				// Ganti '0' dengan ID source atlas kamu (biasanya 0, 1, atau 2)
				// Cek di panel TileSet > Source untuk melihat ID pastinya
				if (sourceId == -1)
				{
					// Tile hantu (terdata di memori tapi tidak ada visualnya)
					EraseCell(cell);
					errorCount++;
				}
				else
				{
					// Cek apakah koordinat atlas valid
					// Ini akan memicu error di output, tapi kita tangkap datanya
					// Sayangnya Godot C# tidak punya "TryGetTileData", 
					// jadi kita pastikan saja source ID valid.
				}
			}

			if (errorCount > 0)
			{
				GD.Print($"[AUTO-FIX] Removed {errorCount} invalid/ghost tiles.");
			}
		}
	}
}