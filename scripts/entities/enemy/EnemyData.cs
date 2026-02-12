using Godot;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string EnemyName;
	[Export] public int MaxHP = 50;
	[Export] public int Damage = 10;
	[Export] public int SightRange = 3;

	[Export] public SpriteFrames SpriteFrames;
}
