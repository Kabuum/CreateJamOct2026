using Godot;
using System;

public partial class Item : Area3D
{
	[Export] public Sprite3D ItemSprite {get; set;}
	GameManager gameManager;
	int ItemValue;	
 	public ItemData Data {get; set;}

	[Export] public float spin_speed;

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
		((Node3D)GetParent()).RotateY(spin_speed * (float)delta);
	}

	public void Collected(Node3D body)
	{
		GD.Print($"Item touched by: {body.Name}");
		GD.Print($"Is server: {Multiplayer.IsServer()}");

		if (body is CollisionObject3D collider)
		{
			GD.Print($"Player layer 3: {collider.GetCollisionLayerValue(3)}");
		}
		
		if (!Multiplayer.IsServer())
		{
			return;
		}

		if (body is not CharacterBody3D player || !player.GetCollisionLayerValue(3))
		{
			return;
		}

		int playerId = player.GetMultiplayerAuthority();

		GD.Print($"Player {playerId} collected {Data.ItemName}");

		gameManager.ItemCollected(playerId, Data);

		GetParent().QueueFree();
	}
}
