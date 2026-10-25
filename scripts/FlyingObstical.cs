using Godot;
using System.Linq;


public partial class FlyingObstical : Node3D
{
	private float spin_speed = 100;
	[Export] public float speed = 10f;
	private Vector3 direction;
	private double Lifetime = 20;
	
	public void Setup(Vector3 startPosition, Vector3 flyDirection, Texture2D OriginalSprite)
    {
        GlobalPosition = startPosition;
        direction = flyDirection.Normalized();
		Sprite3D sprite = GetNode<Sprite3D>("Sprite3D");
		GD.Print(OriginalSprite);
        sprite.Texture = OriginalSprite;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		GlobalPosition += direction * speed * (float)delta;
		Lifetime -= delta;
		if (Lifetime < 0)
		{
			QueueFree();
		}
	}
}
