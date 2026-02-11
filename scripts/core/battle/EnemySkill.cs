using Godot;
using System;

namespace MementoTest.Resources
{
    // Enum untuk membedakan tipe serangan
    public enum EnemyAttackType
    {
        Melee,
        Ranged
    }

    [GlobalClass]
    public partial class EnemySkill : Resource
    {
        [ExportGroup("General Info")]
        [Export] public string SkillName { get; set; } = "Attack";
        [Export] public EnemyAttackType AttackType { get; set; } = EnemyAttackType.Melee;
        [Export] public int Damage { get; set; } = 10;

        // [PERUBAHAN] Range sekarang Integer (Satuan Grid)
        [Export] public int Range { get; set; } = 1;

        [ExportGroup("Reaction Settings")]
        // Waktu yang diberikan ke player untuk mengetik
        [Export] public float ReactionTime { get; set; } = 1.5f;

        [ExportGroup("Visuals")]
        [Export] public string AnimationName { get; set; } = "attack";

        // [FITUR BARU] Projectile Khusus untuk skill ini
        [Export] public PackedScene ProjectilePrefab;

        // Helper: Otomatis tentukan command Parry/Dodge
        public string GetReactionCommand()
        {
            return AttackType == EnemyAttackType.Ranged ? "DODGE" : "PARRY";
        }
    }
}