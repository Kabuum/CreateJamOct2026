using System;
using Godot;

public partial class ItemData : Node
{
	// Called when the node enters the scene tree for the first time.
	public int Value{get; set;}
	public Sprite3D Sprite{get; set;}
	public string Name{get; set;}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public ItemData(int value, Sprite3D sprite, string name)
	{
		Value = value;
		Sprite = sprite;
		Name = name;
	}
}
