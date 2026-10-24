using Godot;

public partial class ItemSpawner : Node3D
{
    [Export]
    public PackedScene itemScene;

    [Export]
    public float spawnRangeX = 10.0f;

    [Export]
    public float spawnRangeZ = 10.0f;

    [Export]
    public float spawnHeight = 1.0f;

    private Timer spawnTimer;

    private readonly RandomNumberGenerator random = new();

    public override void _Ready()
    {
        spawnTimer = GetNode<Timer>("SpawnTimer");

        spawnTimer.Timeout += SpawnItem;

        spawnTimer.Start();
    }

    public void SpawnItem()
    {
        if (itemScene == null)
        {
            GD.PrintErr("No item scene assigned!");
            return;
        }

        // Create a new instance of the item
        Node3D item = itemScene.Instantiate<Node3D>();

        // Generate a random position
        float x = random.RandfRange(-spawnRangeX, spawnRangeX);
        float z = random.RandfRange(-spawnRangeZ, spawnRangeZ);

        Vector3 spawnPosition = new Vector3(x, spawnHeight, z);

        // Add the item to the scene
        AddChild(item);

        // Position the item
        item.Position = spawnPosition;

        GD.Print("Item spawned!");
    }
}