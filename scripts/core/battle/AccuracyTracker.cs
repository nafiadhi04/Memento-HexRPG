using Godot;

public partial class AccuracyTracker : Node
{
	public int TotalCommands { get; private set; }
	public int SuccessfulCommands { get; private set; }

	public float Accuracy =>
		TotalCommands == 0 ? 1f : (float)SuccessfulCommands / TotalCommands;

	public void RegisterAttempt(bool success)
	{
		TotalCommands++;
		if (success)
			SuccessfulCommands++;
	}
}
