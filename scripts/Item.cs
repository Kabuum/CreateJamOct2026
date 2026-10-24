using Godot;
using System;

public partial class Item : Area3D
{
	[Export] public Sprite3D ItemSprite {get; set;}
	GameManager gameManager;
	int ItemValue;	
	public ItemData Data {get; set;}

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

	public void FindData(ItemData data)
	{
		Data = data;
		GetNode<Sprite3D>("../Sprite3D").Texture = data.ItemSprite;
	}

	public override void _Process(double delta)
	{
	}

	public void Collected(Node3D body)
	{

		GD.Print("Entered by: ", body.Name);
		gameManager.ItemCollected(Data);
		GetParent().QueueFree();
		

	}
}
