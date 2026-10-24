using System.Collections.Generic;
using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    // Separate inventory and score for each player
    private readonly Dictionary<int, List<ItemData>> inventories = new();
    private readonly Dictionary<int, int> scores = new();
    private readonly Dictionary<int, int> inventoryCounts = new();

    private const string MainScene = "res://scenes/main.tscn";
    private const string DeathScene = "res://scenes/death screen.tscn";
    private const string WinScene = "res://scenes/win screen.tscn";

    public override void _Ready()
    {
        Instance = this;

        Multiplayer.PeerConnected += OnPeerConnected;
    }

    public override void _ExitTree()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
    }

    // Returns a specific player's inventory
    private List<ItemData> GetInventory(int playerId)
    {
        if (!inventories.ContainsKey(playerId))
        {
            inventories[playerId] = new List<ItemData>();
        }

        return inventories[playerId];
    }

    // Returns a specific player's score
    public int GetScore(int playerId)
    {
        return scores.TryGetValue(playerId, out int score) ? score : 0;
    }

    // Returns the number of items a player carries
    public int GetInventoryCount(int playerId)
    {
        return inventoryCounts.TryGetValue(playerId, out int count) ? count : 0;
    }

    public void WinGame()
    {
        GD.Print("GOT THEM DEALS");
        GetTree().ChangeSceneToFile(WinScene);
    }

    public void LoseGame()
    {
        GD.Print("THE HORDE GOT YOU!!");
        GetTree().ChangeSceneToFile(DeathScene);
    }

    public void RestartGame()
    {
        GD.Print("- RESTARTING GAME -");

        inventories.Clear();
        scores.Clear();
        inventoryCounts.Clear();

        GetTree().ChangeSceneToFile(MainScene);
    }

    // Collect an item for a specific player
    public void ItemCollected(int playerId, ItemData data)
    {
        if (!Multiplayer.IsServer() || data == null)
        {
            return;
        }

        List<ItemData> inventory = GetInventory(playerId);

        inventory.Add(data);

        GD.Print($"Player {playerId} collected {data.ItemName}");

        BroadcastPlayerState(playerId);
    }

    // Existing obstacle functionality
    public void ObstacleHit(Node3D player, Node3D obstacles, ObstacleData data)
    {
        if (data.IsSlowness)
        {
            player.Call("apply_slow", 3f, 0.5f);
        }
        else
        {
            Vector3 dir = (player.GlobalPosition - obstacles.GlobalPosition).Normalized();
            Vector3 impulse = dir * 30;

            player.Call("apply_knockback", impulse, 0.5f);
        }
    }

    // Add score to a specific player
    public void AddScore(int playerId, int amount)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        scores[playerId] = GetScore(playerId) + amount;

        GD.Print($"Player {playerId} score: {scores[playerId]}");

        BroadcastPlayerState(playerId);
    }

    // Deposit items belonging to a specific player
    public void DepositItems(int playerId)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        List<ItemData> inventory = GetInventory(playerId);

        if (inventory.Count == 0)
        {
            GD.Print($"Player {playerId} has no items to deposit!");
            return;
        }

        int totalValue = 0;

        foreach (ItemData item in inventory)
        {
            totalValue += item.ItemValue;
        }

        inventory.Clear();

        AddScore(playerId, totalValue);

        GD.Print($"Player {playerId} deposited items worth {totalValue} points!");
    }

    // Send updated score and inventory count to clients
    private void BroadcastPlayerState(int playerId)
    {
        int count = GetInventory(playerId).Count;

        inventoryCounts[playerId] = count;

        if (Multiplayer.GetPeers().Length > 0)
        {
            Rpc(
                MethodName.SyncPlayerState,
                playerId,
                GetScore(playerId),
                count
            );
        }
    }

    // Only the server can send these updates
    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void SyncPlayerState(int playerId, int score, int count)
    {
        scores[playerId] = score;
        inventoryCounts[playerId] = count;
    }

    // Give newly connected clients the existing scores
    private void OnPeerConnected(long peerId)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        foreach (var entry in scores)
        {
            RpcId(
                peerId,
                MethodName.SyncPlayerState,
                entry.Key,
                entry.Value,
                GetInventoryCount(entry.Key)
            );
        }
    }
}