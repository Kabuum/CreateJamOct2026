using Godot;
using System.Collections.Generic;

public partial class ItemSpawner : Node3D
{
    [Export]
    public PackedScene itemScene;

    private Timer spawnTimer;
    private MultiplayerSpawner itemMultiplayerSpawner;

    private readonly RandomNumberGenerator random = new();

    [Export] private Godot.Collections.Array<Marker3D> spawnPoints = new();

    private readonly Dictionary<Marker3D, Node3D> occupiedPoints = new();

    [Export] public Godot.Collections.Array<ItemData> ItemPool = new();

    public override void _Ready()
    {
        // Get our timer
        spawnTimer = GetNode<Timer>("SpawnTimer");

        // Get multiplayer spawner
        itemMultiplayerSpawner = GetNode<MultiplayerSpawner>("ItemMultiplayerSpawner");

        // Configure multiplayer spawning
        itemMultiplayerSpawner.SpawnFunction =
            new Callable(this, nameof(CreateNetworkItem));

        // Connect timer signal
        spawnTimer.Timeout += SpawnItem;

        // Start spawning
        spawnTimer.Start();

        // Set spawn points to Y = 0
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            spawnPoints[i].Position = new Vector3(
                spawnPoints[i].Position.X,
                0,
                spawnPoints[i].Position.Z
            );

            GD.Print(spawnPoints[i].Position);
        }
    }

    public void SpawnItem()
    {
        // Only spawn on the server after multiplayer starts
        if (!Multiplayer.IsServer() ||
            Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer)
        {
            return;
        }

        if (itemScene == null || spawnPoints.Count == 0 || ItemPool.Count == 0)
        {
            GD.PrintErr("Missing item scene, spawn points, or item pool!");
            return;
        }

        // Find all available spawn points
        List<Marker3D> availablePoints = new();

        foreach (Marker3D point in spawnPoints)
        {
            if (!occupiedPoints.TryGetValue(point, out Node3D existingItem)
                || !GodotObject.IsInstanceValid(existingItem))
            {
                availablePoints.Add(point);
            }
        }

        // Stop if all spawn points are occupied
        if (availablePoints.Count == 0)
        {
            GD.Print("All spawn points are occupied!");
            return;
        }

        // Select a random available spawn point
        int index = random.RandiRange(0, availablePoints.Count - 1);

        Marker3D spawnPoint = availablePoints[index];

        // Select a random item
        int itemIndex = random.RandiRange(0, ItemPool.Count - 1);

        // Create spawn data for multiplayer
        var spawnData = new Godot.Collections.Dictionary
        {
            ["itemIndex"] = itemIndex,
            ["position"] = ToLocal(spawnPoint.GlobalPosition)
        };

        // Spawn item across multiplayer
        Node3D itemRoot = (Node3D)itemMultiplayerSpawner.Spawn(spawnData);

        // Remember which item occupies this spawn point
        occupiedPoints[spawnPoint] = itemRoot;

        GD.Print($"Item spawned at {spawnPoint.Name}");
    }

    public Node CreateNetworkItem(Variant spawnData)
    {
        var data = spawnData.AsGodotDictionary();

        int itemIndex = data["itemIndex"].AsInt32();
        Vector3 position = data["position"].AsVector3();

        // Create the item
        Node3D itemRoot = itemScene.Instantiate<Node3D>();

        Item item = itemRoot.GetNode<Item>("Area3D");

        // Assign the item data
        item.FindData(ItemPool[itemIndex]);

        // Move item to selected spawn point
        itemRoot.Position = position;

        return itemRoot;
    }

    public ItemData GetRandomItem()
    {
        int index = random.RandiRange(0, ItemPool.Count - 1);

        return ItemPool[index];
    }
}