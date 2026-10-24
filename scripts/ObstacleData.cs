using Godot;

[GlobalClass]
public partial class ObstacleData : Resource
{
	// Called when the node enters the scene tree for the first time.
	[Export] public bool IsSlowness{get; set;}
	[Export] public Texture2D ObstacleSprite{get; set;}
	[Export] public string ObstacleName{get; set;}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public ObstacleData()
	{
	}
}
