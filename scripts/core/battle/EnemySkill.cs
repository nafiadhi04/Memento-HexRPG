using Godot;
using System;

namespace MementoTest.Resources
{
    [GlobalClass]
    public partial class EnemySkill : Resource
    {
        [Export] public string SkillName { get; set; } = "Unknown Attack";
        [Export] public int Damage { get; set; } = 10;

        // Jarak serangan dalam Pixel (agar mudah dihitung)
        // Misal: 100 (Dekat), 300 (Jauh/Tembak), 1000 (Sniper)
        [Export] public float AttackRange { get; set; } = 100f;

        [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "Attack Description";
    }
}