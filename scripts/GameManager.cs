using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    private const string MainScene = "res://scenes/main.tscn";
    private const string DeathScene = "res://scenes/death screen.tscn";
    private const string WinScene = "res://scenes/win screen.tscn";

    public override void _Ready()
    {
        Instance = this;
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
    }

	
}