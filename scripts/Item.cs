using Godot;
using System;

public partial class Item : Area3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Collected(Node3D body)
	{
		GD.Print("Entering");
		QueueFree();
		

	}
}
