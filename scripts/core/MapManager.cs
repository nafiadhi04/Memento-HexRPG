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
		[Export] public float LineThickness = 1.5f;
		[Export(PropertyHint.Range, "0.1, 1.0")]
		public float NeighborScale = 0.85f;
		private Vector2 _visualOffset = new Vector2(0, 7);

		[ExportSubgroup("Combat Visuals")]

		[Export]
		public Color TargetLockedColor = new Color(1, 0.5f, 0, 1.0f);

		[ExportSubgroup("Combat Visuals")]
		// [WARNA BARU - BISA DIEDIT DI INSPECTOR]
		[Export] public Color MovementColor = new Color(0, 0, 1, 0.4f);      // Biru Transparan
		[Export] public Color AttackRangeColor = new Color(1, 1, 0, 0.4f);   // Kuning Transparan
		[Export] public Color PlayerLockColor = new Color(0, 0.5f, 0, 0.6f); // Hijau Tua
		[Export] public Color EnemyLockColor = new Color(0.5f, 0, 0, 0.6f);
		private readonly Color _enemySightColor = new Color(0.6f, 0.2f, 0.8f, 0.4f);
		private List<Polygon2D> _enemySightHighlights = new List<Polygon2D>();// Merah Tua

		private List<Node2D> _activeSelectors = new List<Node2D>();
		// Variabel untuk Kursor Visual
		private Line2D _highlightCursor;


		private Line2D _mouseCursor; // Ganti nama biar jelas



   // Highlight Player saat Lock

		// List untuk Movement Highlight (Biru)
		private List<Polygon2D> _activeHighlights = new List<Polygon2D>();

		// List untuk Attack Range Highlight (Kuning)
		private List<Polygon2D> _activeAttackHighlights = new List<Polygon2D>();

		// Indicator Lock Musuh (Merah)
		private Polygon2D _targetCursorIndicator;

		// Indicator Lock Player (Hijau)
		private Polygon2D _playerLockIndicator;

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

		// --- VISUAL HEXAGON FLAT-TOP (DATAR ATAS) ---
		private Polygon2D CreateHexPolygonStyle(Color color, float scale = 0.9f)
		{
			Polygon2D poly = new Polygon2D();
			poly.Color = color;

			// Gunakan HexWidth / 2 sebagai radius dasar
			float radius = (HexWidth / 2f) * scale;

			Vector2[] points = new Vector2[6];
			for (int i = 0; i < 6; i++)
			{
				// Flat Top Angles: 0, 60, 120, 180, 240, 300
				// Sudut 0 ada di Kanan, membuat sisi atas datar.
				float angle_deg = 60 * i;
				float angle_rad = Mathf.DegToRad(angle_deg);

				points[i] = new Vector2(
					radius * Mathf.Cos(angle_rad),
					radius * Mathf.Sin(angle_rad)
				);
			}
			poly.Polygon = points;

			// --- ANIMASI KEDIP ---
			var tween = CreateTween().SetLoops();
			tween.BindNode(poly);

			float startAlpha = color.A;
			float lowAlpha = startAlpha * 0.4f;

			tween.TweenProperty(poly, "color:a", lowAlpha, 0.6f).SetTrans(Tween.TransitionType.Sine);
			tween.TweenProperty(poly, "color:a", startAlpha, 0.6f).SetTrans(Tween.TransitionType.Sine);

			return poly;
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

		// Versi Paling Stabil untuk MapManager.cs
		public int GetGridDistance(Vector2I gridA, Vector2I gridB)
		{
			if (gridA == gridB) return 0;
			Vector2 posA = GridToWorld(gridA);
			Vector2 posB = GridToWorld(gridB);

			// Membagi jarak pixel dengan konstanta ukuran hex
			// Angka 21.0f didapat dari (14.0 * 1.5). Sesuaikan jika gridmu lebih besar.
			return Mathf.RoundToInt(posA.DistanceTo(posB) / 21.0f);
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

		// --- 2. ATTACK RANGE HIGHLIGHT (KUNING) ---
		public void ShowAttackRange(Vector2I centerPos, int range)
		{
			ClearAttackHighlights();

			// Scan area kotak, lalu filter jarak hex
			for (int x = -range; x <= range; x++)
			{
				for (int y = -range; y <= range; y++)
				{
					Vector2I targetGrid = centerPos + new Vector2I(x, y);

					// Validasi Jarak & Tile
					if (GetGridDistance(centerPos, targetGrid) <= range)
					{
						if (GetCellSourceId(targetGrid) != -1) // Pastikan tile ada
						{
							Polygon2D hex = CreateHexPolygonStyle(AttackRangeColor, 1.8f);
							hex.Position = GridToWorld(targetGrid) + VisualOffset;

							AddChild(hex);
							_activeAttackHighlights.Add(hex);
						}
					}
				}
			}
		}

		// --- FUNGSI BARU: HIGHLIGHT MUSUH SPESIFIK (LOCK) ---
		// --- 3. TARGET LOCK HIGHLIGHT (MERAH TUA - MUSUH) ---
		public void ShowTargetHighlight(Vector2I enemyPos)
		{
			if (_targetCursorIndicator != null && IsInstanceValid(_targetCursorIndicator))
			{
				_targetCursorIndicator.QueueFree();
			}

			_targetCursorIndicator = CreateHexPolygonStyle(EnemyLockColor, 1.8f); // Full size
			_targetCursorIndicator.Position = GridToWorld(enemyPos) + VisualOffset;
			AddChild(_targetCursorIndicator);
		}
		public void ClearAttackHighlights() // Bersihkan Attack (Kuning) + Lock Indicators
		{
			foreach (var poly in _activeAttackHighlights)
			{
				if (IsInstanceValid(poly)) poly.QueueFree();
			}
			_activeAttackHighlights.Clear();

			if (_targetCursorIndicator != null && IsInstanceValid(_targetCursorIndicator))
			{
				_targetCursorIndicator.QueueFree();
				_targetCursorIndicator = null;
			}

			if (_playerLockIndicator != null && IsInstanceValid(_playerLockIndicator))
			{
				_playerLockIndicator.QueueFree();
				_playerLockIndicator = null;
			}
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

		// --- 4. PLAYER LOCK HIGHLIGHT (HIJAU TUA - PLAYER) ---
		public void ShowPlayerLockHighlight(Vector2I playerPos)
		{
			if (_playerLockIndicator != null && IsInstanceValid(_playerLockIndicator))
			{
				_playerLockIndicator.QueueFree();
			}

			_playerLockIndicator = CreateHexPolygonStyle(PlayerLockColor, 1.8f); // Full size
			_playerLockIndicator.Position = GridToWorld(playerPos) + VisualOffset;
			AddChild(_playerLockIndicator);
		}

		public void ShowNeighborHighlight(Vector2I currentGrid)
		{
			ClearHighlight(); // Bersihkan yang lama

			foreach (Vector2I neighbor in GetNeighbors(currentGrid))
			{
				if (IsTileWalkable(neighbor) && !IsTileOccupied(neighbor))
				{
					Polygon2D hex = CreateHexPolygonStyle(MovementColor, 1.80f); // Skala 0.8 agar ada celah sedikit
					hex.Position = GridToWorld(neighbor) + VisualOffset;

					AddChild(hex);
					_activeHighlights.Add(hex);
				}
			}
		}

		public void ShowEnemySight(Vector2I centerGrid, int range)
		{
			// Hapus highlight sebelumnya (jika ada)
			ClearEnemySight();

			// Dapatkan semua tile dalam radius sight
			List<Vector2I> tilesInRange = GetTilesInRange(centerGrid, range);

			foreach (var tile in tilesInRange)
			{
				// Gunakan fungsi pembuat polygon yang sudah ada (asumsi namanya CreateHexPolygonStyle)
				// Kita kirim warna ungu
				Polygon2D poly = CreateHexPolygonStyle(_enemySightColor, 0f); // Scale 1.0 biar penuh

				AddChild(poly);
				poly.GlobalPosition = GridToWorld(tile);

				// Simpan di list khusus enemy sight
				_enemySightHighlights.Add(poly);
			}
		}
		public List<Vector2I> GetTilesInRange(Vector2I center, int range)
		{
			List<Vector2I> results = new List<Vector2I>();

			// Gunakan BFS untuk menyebar dari tengah
			Queue<Vector2I> queue = new Queue<Vector2I>();
			Dictionary<Vector2I, int> distMap = new Dictionary<Vector2I, int>();

			queue.Enqueue(center);
			distMap[center] = 0;
			results.Add(center);

			while (queue.Count > 0)
			{
				Vector2I current = queue.Dequeue();
				int currentDist = distMap[current];

				// Jika sudah mencapai batas range, jangan cari tetangganya lagi
				if (currentDist >= range) continue;

				// Cek semua tetangga
				foreach (Vector2I neighbor in GetNeighbors(current))
				{
					if (!distMap.ContainsKey(neighbor))
					{
						distMap[neighbor] = currentDist + 1;
						queue.Enqueue(neighbor);
						results.Add(neighbor);
					}
				}
			}

			return results;
		}
		public void UpdateEnemySight(Vector2I oldPos, Vector2I newPos, int range)
		{

			if (_enemySightHighlights.Count > 0)
			{
				ClearEnemySight();
				ShowEnemySight(newPos, range);
			}
		}
		// Update posisi lingkaran merah
		public void MoveTargetHighlight(Vector2I newTargetPos)
		{
			// Asumsi Anda punya variabel _targetHighlightPolygon atau sejenisnya
			// Jika tidak, panggil ulang ShowTargetHighlight

			// Hapus yang lama (jika fungsi ShowTargetHighlight Anda tidak otomatis menghapus)
			// ClearTargetHighlight(); 

			// Buat baru di posisi baru
			ShowTargetHighlight(newTargetPos);
		}

		// Fungsi untuk mengecek apakah sedang ada highlight sight aktif
		public bool IsEnemySightActive()
		{
			return _enemySightHighlights.Count > 0;
		}

		// Dipanggil untuk menghilangkan highlight ungu
		public void ClearEnemySight()
		{
			foreach (var poly in _enemySightHighlights)
			{
				if (GodotObject.IsInstanceValid(poly))
				{
					poly.QueueFree();
				}
			}
			_enemySightHighlights.Clear();
		}
		public List<Vector2I> GetNeighbors(Vector2I cell)
    {
        List<Vector2I> list = new List<Vector2I>();

        // Daftar sisi tetangga untuk Flat-Top
        TileSet.CellNeighbor[] sides = {
            TileSet.CellNeighbor.TopSide, 
            TileSet.CellNeighbor.BottomSide,
            TileSet.CellNeighbor.TopLeftSide, 
            TileSet.CellNeighbor.TopRightSide,
            TileSet.CellNeighbor.BottomLeftSide, 
            TileSet.CellNeighbor.BottomRightSide
        };

        foreach (var side in sides)
        {
            // GetNeighborCell adalah fungsi bawaan TileMapLayer.
            // Dia otomatis menghitung berdasarkan settingan Inspector (Vertical/Horizontal/Odd/Even)
            Vector2I neighbor = GetNeighborCell(cell, side);
            
            // Tambahkan ke list
            list.Add(neighbor);
        }

        return list;
    }

		public void ClearHighlight() // Bersihkan Movement (Biru)
		{
			foreach (var poly in _activeHighlights)
			{
				if (IsInstanceValid(poly)) poly.QueueFree();
			}
			_activeHighlights.Clear();
		}

	}
}