using Godot;
using System;

public partial class Obstacles : Area3D
{
	[Export] public Sprite3D ObsticleSprite {get; set;}
	GameManager gameManager;
	bool IsSlowness;
 	public ObstacleData Data {get; set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		Monitoring = true;
    	Monitorable = true;
    	BodyEntered += GotHit;

    	GD.Print("Monitoring: ", Monitoring);
    	GD.Print("Collision children: ", GetChildren().Count);
	}

	public void FindData(ObstacleData data)
	{
		Data = data;
		GetNode<Sprite3D>("../Sprite3D").Texture = data.ObstacleSprite;
	}

	public override void _Process(double delta)
	{
		
	}

	public void GotHit(Node3D body)
	{

		GD.Print("Hit ", body.Name);
		gameManager.ObstacleHit(body, this, Data);
		GetParent().QueueFree();
		

	}
}
