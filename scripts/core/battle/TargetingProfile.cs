using Godot;

namespace MementoTest.Core
{
	[GlobalClass]
	public partial class TargetingProfile : Resource
	{
		public enum TargetRule
		{
			Closest,
			LowestHP,
			HighestCombo,
			Random
		}

		[Export] public TargetRule Rule = TargetRule.Closest;
	}
}
