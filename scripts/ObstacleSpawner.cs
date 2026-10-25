using Godot;
using System.Collections.Generic;

public partial class ObstacleSpawner : Node
{
    [Export]
    public PackedScene itemScene;

    private Timer spawnTimer;

    private readonly RandomNumberGenerator random = new();

    [Export] private Godot.Collections.Array<Marker3D> spawnPoints = new();

    private readonly Dictionary<Marker3D, Node3D> occupiedPoints = new();

    [Export] public Godot.Collections.Array<ObstacleData> ObstaclePool = new();

    public override void _Ready()
    {
        // Get our timer
        spawnTimer = GetNode<Timer>("SpawnTimer");

        // Connect timer signal
        spawnTimer.Timeout += SpawnObstacle;

        // Start spawning
        spawnTimer.Start();

        /*for(int i = 0; i < spawnPoints.Count; i++)
        {
            spawnPoints[i].Position = new Vector3(
                spawnPoints[i].Position.X,
                0,
                spawnPoints[i].Position.Z
            );
            GD.Print(spawnPoints[i].Position);
        }*/
    }

    public void SpawnObstacle()
    {
        // Only spawn on the server after multiplayer starts
        if (!Multiplayer.IsServer() ||
            Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer)
        {
            return;
        }

        if (itemScene == null || spawnPoints.Count == 0 || ObstaclePool.Count == 0)
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

        // Create the Obstacle
        ObstacleData Data = GetRandomObstacle();
        Node3D ObstacleRoot = itemScene.Instantiate<Node3D>();
        Obstacles Obstacle = ObstacleRoot.GetNode<Obstacles>("Area3D");
        // Add Obstacle to the scene
        Obstacle.FindData(Data);
        GetTree().CurrentScene.AddChild(ObstacleRoot);
        // Move Obstacle to the selected spawn point
        ObstacleRoot.GlobalPosition = spawnPoint.GlobalPosition;

        GD.Print($"Item spawned at {spawnPoint.Name}");
    }

    public ObstacleData GetRandomObstacle()
    {
        
        int index = random.RandiRange(0, ObstaclePool.Count - 1);
        return ObstaclePool[index];
    }
}
