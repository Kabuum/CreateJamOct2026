using Godot;
using System.Collections.Generic;

public partial class ObstacleSpawner : Node
{
    [Export]
    public PackedScene itemScene;

    private Timer spawnTimer;

    private readonly RandomNumberGenerator random = new();

    [Export] private Godot.Collections.Array<Marker3D> spawnPoints = new();

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
        if (itemScene == null || spawnPoints.Count == 0)
        {
            GD.PrintErr("Missing item scene or spawn points!");
            return;
        }

        // Select a random spawn point
        int index = random.RandiRange(0, spawnPoints.Count - 1);

        Marker3D spawnPoint = spawnPoints[index];

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
