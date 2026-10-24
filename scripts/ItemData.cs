using Godot;

[GlobalClass]
public partial class ItemData : Resource
{
	// Called when the node enters the scene tree for the first time.
	[Export] public int ItemValue{get; set;} = 0;
	[Export] public Texture2D ItemSprite{get; set;}
	[Export] public string ItemName{get; set;} = "";
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public ItemData()
	{
	}
}
