using Godot;
using System;

namespace MementoTest.Resources
{
	[GlobalClass]
	public partial class PlayerSkill : Resource
	{
		[Export] public string CommandName { get; set; } = "ping";
		[Export] public int ApCost { get; set; } = 2;
		[Export] public int Damage { get; set; } = 10;
		[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "Basic attack";
	}
}