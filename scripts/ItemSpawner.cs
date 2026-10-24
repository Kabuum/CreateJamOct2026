using Godot;
using System;
using System.Collections.Generic;

public partial class ItemSpawner : Node3D
{
    [Export]
    public PackedScene itemScene;

    private Timer spawnTimer;

    private readonly RandomNumberGenerator random = new();

    private readonly List<Marker3D> spawnPoints = new();

    [Export] public Godot.Collections.Array<ItemData> ItemPool {get; set;} = new();

    public override void _Ready()
    {
        // Get our timer
        spawnTimer = GetNode<Timer>("SpawnTimer");

        // Find all Marker3D children
        foreach (Node child in GetChildren())
        {
            if (child is Marker3D marker)
            {
                spawnPoints.Add(marker);
            }
        }

        // Connect timer signal
        spawnTimer.Timeout += SpawnItem;

        // Start spawning
        spawnTimer.Start();
    }

    public void SpawnItem()
    {
        if (itemScene == null || spawnPoints.Count == 0)
        {
            GD.PrintErr("Missing item scene or spawn points!");
            return;
        }

        // Select a random spawn point
        int index = random.RandiRange(0, spawnPoints.Count - 1);

        Marker3D spawnPoint = spawnPoints[index];

        // Create the item
        ItemData Data = GetRandomItem();
        Node3D itemRoot = itemScene.Instantiate<Node3D>();
        Item item = itemRoot.GetNode<Item>("Area3D");
        // Add item to the scene
        item.FindData(Data);
        GetTree().CurrentScene.AddChild(itemRoot);
        // Move item to the selected spawn point
        itemRoot.GlobalPosition = spawnPoint.GlobalPosition;

        GD.Print($"Item spawned at {spawnPoint.Name}");
    }

    public ItemData GetRandomItem()
    {
        
        int index = random.RandiRange(0, ItemPool.Count - 1);
        return ItemPool[index];
    }
}