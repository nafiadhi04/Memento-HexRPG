using Godot;
using System;
using System.Collections.Generic;

namespace MementoTest.Core
{
	public partial class MapManager : TileMapLayer
	{
		public static MapManager Instance { get; private set; }
		[Export] public TileMapLayer GameBoard;

		// --- BAGIAN SETTING VISUAL (Editable di Inspector) ---
		[ExportGroup("Highlight Settings")]

		[ExportSubgroup("Colors")]
		[Export] public Color MoveHighlightColor = new Color(1, 1, 0, 0.7f); // Kuning
		[Export] public Color CursorValidColor = new Color(1, 1, 0, 0.8f);   // Kuning Terang
		[Export] public Color CursorInvalidColor = new Color(1, 0, 0, 0.8f); // Merah

		[ExportSubgroup("Dimensions")]
		[Export] public float HexWidth = 14.0f;       // Jarak Pusat ke Kanan (w)
		[Export] public float HexHeight = 12.0f;      // Jarak Pusat ke Bawah (h)
		[Export] public float HexTopFlat = 7.0f;      // Lebar sisi atas/bawah (m)
		[Export] public Vector2 VisualOffset = new Vector2(0, 7); // Geser visual agar pas di tengah

		[ExportSubgroup("Style")]
		[Export] public float LineThickness = 3.0f;
		[Export(PropertyHint.Range, "0.1, 1.0")]
		public float NeighborScale = 0.85f;
		private Vector2 _visualOffset = new Vector2(0, 7);

		private List<Node2D> _activeSelectors = new List<Node2D>();
		// Variabel untuk Kursor Visual
		private Line2D _highlightCursor;

		private List<Line2D> _activeHighlights = new List<Line2D>();

		private Line2D _mouseCursor; // Ganti nama biar jelas

		public override void _Ready()
		{
			Instance = this;

			// Buat cursor mouse (Default scale 1.0f)
			_mouseCursor = CreateHexLineStyle(CursorValidColor, 1.0f);
			_mouseCursor.Visible = false;
			AddChild(_mouseCursor);
		}
		public override void _Process(double delta)
		{
			UpdateMouseCursor();
		}

		private Line2D CreateHexLineStyle(Color color, float scale)
		{
			Line2D line = new Line2D();
			line.Width = LineThickness; // Menggunakan variabel export
			line.DefaultColor = color;
			line.Closed = true;
			line.ZIndex = 5;

			// Hitung dimensi berdasarkan Scale
			float w = HexWidth * scale;
			float h = HexHeight * scale;
			float m = HexTopFlat * scale;

			// Titik-titik Hexagon (Flat Top)
			Vector2[] hexPoints = new Vector2[]
			{
				new Vector2(-m, -h),
				new Vector2(m, -h),
				new Vector2(w, 0),
				new Vector2(m, h),
				new Vector2(-m, h),
				new Vector2(-w, 0)
			};

			line.Points = hexPoints;
			return line;
		}
		public Vector2I WorldToGrid(Vector2 worldPos)
		{
			if (GameBoard == null)
			{
				GD.PrintErr("[MAP ERROR] GameBoard belum di-assign di Inspector MapManager!");
				return Vector2I.Zero;
			}
			// Ubah posisi dunia ke lokal TileMap, lalu ke Grid Coordinate
			return GameBoard.LocalToMap(GameBoard.ToLocal(worldPos));
		}

		// 2. Mengubah Koordinat Grid (Kolom, Baris) -> Posisi Dunia (Pixel Tengah)
		public Vector2 GridToWorld(Vector2I gridPos)
		{
			if (GameBoard == null) return Vector2.Zero;

			// Ubah Grid ke Lokal, lalu ke Global World Position
			return GameBoard.ToGlobal(GameBoard.MapToLocal(gridPos));
		}
		private void CreateHighlightCursor()
		{
			// Membuat node Line2D secara coding (tanpa perlu add node di Scene)
			_highlightCursor = new Line2D();
			_highlightCursor.Width = 3.5f;           // Ketebalan garis
			_highlightCursor.DefaultColor = new Color(1, 1, 0, 0.7f); // Warna Kuning Transparan
			_highlightCursor.Closed = true;          // Agar garis nyambung jadi lingkaran/hex
			_highlightCursor.ZIndex = 5;             // Pastikan di atas player/tanah

			// Membuat bentuk Hexagon (Flat Top)
			// Ukuran disesuaikan dengan Tile Size kamu (32x28)
			// Kamu bisa ubah angka ini jika bentuknya kurang pas
			float w = 14.0f; // Setengah lebar (agak dikurangi biar border ada di dalam)
			float h = 12.0f; // Setengah tinggi
			float m = 7.0f;  // Kemiringan sudut (untuk hex)

			Vector2[] hexPoints = new Vector2[]
			{
				new Vector2(-m, -h), // Kiri Atas
                new Vector2(m, -h),  // Kanan Atas
                new Vector2(w, 0),   // Kanan Tengah
                new Vector2(m, h),   // Kanan Bawah
                new Vector2(-m, h),  // Kiri Bawah
                new Vector2(-w, 0)   // Kiri Tengah
            };

			_highlightCursor.Points = hexPoints;
			_highlightCursor.Visible = false; // Sembunyikan dulu

			AddChild(_highlightCursor); // Masukkan ke dalam Scene
		}

		private void UpdateCursorPosition()
		{
			Vector2 mousePos = GetGlobalMousePosition();
			Vector2I gridPos = GetGridCoordinates(mousePos);

			// LANGKAH 1: Cek Existensi Tile
			// GetCellSourceId mengembalikan -1 jika tidak ada tile di koordinat tersebut (Void)
			int sourceId = GetCellSourceId(gridPos);

			if (sourceId != -1)
			{
				// Jika tile ADA, kita tampilkan kursornya
				_highlightCursor.Visible = true;

				// Pasang posisi + offset visual yang tadi kita bahas
				Vector2 visualOffset = new Vector2(0, 7);
				_highlightCursor.Position = GetSnappedWorldPosition(mousePos) + visualOffset;

				// LANGKAH 2: Cek Walkability untuk menentukan Warna
				if (IsTileWalkable(gridPos))
				{
					// AREA AMAN (Bisa Jalan) -> Warna KUNING
					_highlightCursor.DefaultColor = new Color(1, 1, 0, 0.8f);
				}
				else
				{
					// AREA TERLARANG (Air/Tembok) -> Warna MERAH
					_highlightCursor.DefaultColor = new Color(1, 0, 0, 0.8f);
				}
			}
			else
			{
				// Jika mouse keluar dari area pulau (Void) -> Sembunyikan
				_highlightCursor.Visible = false;
			}
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

		public bool IsTileWalkable(Vector2I mapCoords)
		{
			if (GetCellSourceId(mapCoords) == -1) return false;

			TileData data = GetCellTileData(mapCoords);
			if (data == null) return false;

			Variant walkable = data.GetCustomData("is_walkable");
			if (walkable.VariantType == Variant.Type.Nil) return true;

			return walkable.AsBool();
		}

		public bool IsTileOccupied(Vector2I targetGridCoords)
		{
			var enemies = GetTree().GetNodesInGroup("Enemy");
			var players = GetTree().GetNodesInGroup("Player");

			foreach (Node2D entity in enemies)
			{
				// Cek jarak aman karena posisi float tidak selalu persis sama
				if (GetGridCoordinates(entity.GlobalPosition) == targetGridCoords) return true;
			}
			foreach (Node2D entity in players)
			{
				if (GetGridCoordinates(entity.GlobalPosition) == targetGridCoords) return true;
			}
			return false;
		}

		public bool IsNeighbor(Vector2I currentCoords, Vector2I targetCoords)
		{
			if (currentCoords == targetCoords) return false;
			TileSet.CellNeighbor[] neighbors = {
				TileSet.CellNeighbor.TopSide, TileSet.CellNeighbor.BottomSide,
				TileSet.CellNeighbor.TopLeftSide, TileSet.CellNeighbor.TopRightSide,
				TileSet.CellNeighbor.BottomLeftSide, TileSet.CellNeighbor.BottomRightSide
			};
			foreach (var side in neighbors)
			{
				if (GetNeighborCell(currentCoords, side) == targetCoords) return true;
			}
			return false;
		}


		public void ClearMovementOptions()
		{
			foreach (var selector in _activeSelectors)
			{
				if (GodotObject.IsInstanceValid(selector))
				{
					selector.QueueFree();
				}
			}
			_activeSelectors.Clear();
		}

		// --- LOGIC MOUSE CURSOR ---
		private void UpdateMouseCursor()
		{
			Vector2 mousePos = GetGlobalMousePosition();
			Vector2I gridPos = GetGridCoordinates(mousePos);
			int sourceId = GetCellSourceId(gridPos);

			if (sourceId != -1)
			{
				_mouseCursor.Visible = true;
				// Gunakan VisualOffset dari Inspector
				_mouseCursor.Position = GetSnappedWorldPosition(mousePos) + VisualOffset;

				// Update warna berdasarkan logic (Walkable/Occupied)
				if (IsTileWalkable(gridPos) && !IsTileOccupied(gridPos))
					_mouseCursor.DefaultColor = CursorValidColor;
				else
					_mouseCursor.DefaultColor = CursorInvalidColor;
			}
			else
			{
				_mouseCursor.Visible = false;
			}
		}

		public void ShowNeighborHighlight(Vector2I centerPos)
		{
			ClearHighlight();

			TileSet.CellNeighbor[] neighbors = {
				TileSet.CellNeighbor.TopSide, TileSet.CellNeighbor.BottomSide,
				TileSet.CellNeighbor.TopLeftSide, TileSet.CellNeighbor.TopRightSide,
				TileSet.CellNeighbor.BottomLeftSide, TileSet.CellNeighbor.BottomRightSide
			};

			foreach (var side in neighbors)
			{
				Vector2I neighborGrid = GetNeighborCell(centerPos, side);

				if (IsTileWalkable(neighborGrid) && !IsTileOccupied(neighborGrid))
				{
					// Gunakan MoveHighlightColor dan NeighborScale dari Inspector
					Line2D hexHighlight = CreateHexLineStyle(MoveHighlightColor, NeighborScale);

					// Posisi + VisualOffset dari Inspector
					hexHighlight.Position = GridToWorld(neighborGrid) + VisualOffset;

					AddChild(hexHighlight);
					_activeHighlights.Add(hexHighlight);
				}
			}
		}

		public void ClearHighlight()
		{
			foreach (var line in _activeHighlights)
			{
				if (GodotObject.IsInstanceValid(line)) line.QueueFree();
			}
			_activeHighlights.Clear();
		}
	}
}