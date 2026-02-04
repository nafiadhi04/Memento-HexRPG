using Godot;

[GlobalClass]
public partial class TargetingProfile : Resource
{
	public enum TargetRule
	{
		Closest,
		LowestHP,
		Random
	}

	[Export] public TargetRule Rule;
}
