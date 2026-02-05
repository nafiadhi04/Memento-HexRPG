using Godot;

[GlobalClass]
public partial class SpeedBonusProfile : Resource
{
	[Export] public float MaxReactionTime = 3.0f;
	[Export] public int MaxBonusScore = 100;

	public int CalculateBonus(float reactionTime)
	{
		float ratio = 1f - (reactionTime / MaxReactionTime);
		ratio = Mathf.Clamp(ratio, 0f, 1f);
		return Mathf.RoundToInt(ratio * MaxBonusScore);
	}
}
