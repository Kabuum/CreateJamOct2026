using Godot;
using System;

public partial class Item : Area3D
{
	GameManager gameManager;
	int value;	
	Sprite3D sprite;
	string name;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		Monitoring = true;
    	Monitorable = true;
    	BodyEntered += Collected;

    	GD.Print("Monitoring: ", Monitoring);
    	GD.Print("Collision children: ", GetChildren().Count);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Collected(Node3D body)
	{

		GD.Print("Entered by: ", body.Name);
		ItemData data = new ItemData(value, sprite, Name);
		gameManager.ItemCollected(data);
		GetParent().QueueFree();
		

	}
}
