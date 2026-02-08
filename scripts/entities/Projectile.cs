using Godot;
using MementoTest.Entities;

public partial class Projectile : Area2D
{
	[Export] public float Speed = 400f;

	private int _damage;
	private Vector2 _direction;
	private EnemyController _target;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
	}

	public void Init(int damage, Vector2 direction, EnemyController target = null)
	{
		_damage = damage;
		_direction = direction.Normalized();
		_target = target;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_target != null && GodotObject.IsInstanceValid(_target))
		{
			// 🔥 HOMING
			_direction = (_target.GlobalPosition - GlobalPosition).Normalized();
		}

		GlobalPosition += _direction * Speed * (float)delta;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is EnemyController enemy)
		{
			enemy.TakeDamage(_damage);
			QueueFree();
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		// kalau enemy pakai Area2D
		if (area.GetParent() is EnemyController enemy)
		{
			enemy.TakeDamage(_damage);
			QueueFree();
		}
	}
}
