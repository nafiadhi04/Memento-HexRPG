using Godot;

namespace MementoTest.Resources
{
	[GlobalClass]
	public partial class EnemyAIProfile : Resource
	{
		public enum BehaviorType
		{
			Aggressive,
			Kiting
		}

		[ExportGroup("Behavior")]
		[Export] public BehaviorType Behavior = BehaviorType.Aggressive;

		[ExportGroup("Distance Settings")]
		[Export] public float PreferredDistance = 120f;   // Ideal attack distance
		[Export] public float RetreatDistance = 80f;      // Kapan mundur (kiting)
		[Export] public float ChaseDistance = 200f;       // Maks jarak ngejar

		[ExportGroup("Reaction Threshold")]
		[Export] public float MeleeThreshold = 150f;      // <= ini dianggap melee
	}
}
