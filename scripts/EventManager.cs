using Godot;
using System;

public partial class EventManager : Node
{	
	[Export]
	private PackedScene FlyingObstacleScene;

    private const string FlyingObjectPath =
        "res://scenes/FlyingObstacleScene.tscn";

    public override void _Ready()
    {
        FlyingObstacleScene =
            GD.Load<PackedScene>(FlyingObjectPath);

        if (FlyingObstacleScene == null)
            GD.PrintErr("Could not load: ", FlyingObjectPath);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void BlastingOff(Texture2D sprite, Node3D player, Node3D obstacles)
	{
		Vector3 dir = (player.GlobalPosition + obstacles.GlobalPosition).Normalized();
		FlyingObstical flaying = FlyingObstacleScene.Instantiate<FlyingObstical>();
		GetTree().CurrentScene.AddChild(flaying);
		GD.Print(sprite);
		flaying.Setup(obstacles.GlobalPosition, dir, sprite);
	}

	public override void _Process(double delta)
	{

	}

}
