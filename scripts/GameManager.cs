
using System.Collections.Generic;
using Godot;

public partial class GameManager : Node
{   
    public static GameManager Instance { get; private set; }
    public List<ItemData> Inventory = new List<ItemData>();
    public CharacterBody3D Player1;
    public int Score = 0;
    private const string MainScene = "res://scenes/main.tscn";
    private const string DeathScene = "res://scenes/death screen.tscn";
    private const string WinScene = "res://scenes/win screen.tscn";

    public override void _Ready()
    {
        Instance = this;
        Player1 = GetTree().GetFirstNodeInGroup("Player") as CharacterBody3D;
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
        GetTree().ChangeSceneToFile(MainScene);
        Score = 0;
    }

    public void ItemCollected(ItemData data)
    {
        Inventory.Add(data);   
        for(int i = 0; i < Inventory.Count; i++){
            GD.Print(Inventory[i].ItemName);
        }
    }

    public void ObstacleHit(Node3D Player, Node3D Obsicles, ObstacleData data)
    {
        if (data.IsSlowness){
        Player.Call("apply_slow", 3f, 0.5f);
        }
        else
        {
        Vector3 dir = (Player.GlobalPosition - Obsicles.GlobalPosition).Normalized();
        Vector3 impulse = dir * 30;
        Player.Call("apply_knockback", impulse, 0.5f);
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        GD.Print(Score);
    }

    public void DepositItems()
    {
        if (Inventory.Count == 0)
        {
            GD.Print("No items to deposit!");
            return;
        }

        int totalValue = 0;

        foreach (ItemData item in Inventory)
        {
            totalValue += item.ItemValue;
        }

        Inventory.Clear();

        AddScore(totalValue);

        GD.Print($"Deposited items worth {totalValue} points!");
    }
    
}