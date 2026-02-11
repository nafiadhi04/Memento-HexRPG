using Godot;

namespace MementoTest.Resources
{
	[GlobalClass]
	public partial class EnemyAIProfile : Resource
	{
		public enum BehaviorType
		{
			Aggressive, // Maju terus sampai jarak serang
			Kiting      // Jaga jarak aman, mundur jika terlalu dekat
		}

		[ExportGroup("Behavior")]
		[Export] public BehaviorType Behavior = BehaviorType.Aggressive;

		[ExportGroup("Grid Distance Settings")]
		// GANTI KE INT (SATUAN TILE)

		[Export] public int PreferredDistance = 1;   // Jarak ideal (1 = Melee, 3+ = Range)
		[Export] public int RetreatDistance = 2;     // Jika jarak < 2 tile, mundur (Khusus Kiting)
		[Export] public int ChaseDistance = 6;
		[Export] public int SightRange = 5; 
		[ExportGroup("Reaction Threshold")]
		[Export] public int MeleeThreshold = 1;      // Jarak <= 1 dianggap Melee (Parry)
	}
}