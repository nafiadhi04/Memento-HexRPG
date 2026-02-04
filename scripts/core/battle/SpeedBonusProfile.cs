using Godot;

[GlobalClass]
public partial class SpeedBonusProfile : Resource
{
	[Export] public float MaxReactionTime = 3.0f;
	[Export] public int MaxBonusScore = 100;
}
