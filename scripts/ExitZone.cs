using Godot;

public partial class ExitZone : Area3D
{
    public override void _Ready()
    {
        // Exit starts hidden and inactive
        Hide();
        Monitoring = false;

        BodyEntered += OnBodyEntered;
    }

    public void OpenExit()
    {
        // Make exit visible
        Show();

        // Enable player detection
        SetDeferred("monitoring", true);

        GD.Print($"Exit {Name} opened!");
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (body is not CharacterBody3D player ||
            !player.GetCollisionLayerValue(3))
        {
            return;
        }

        int playerId = player.GetMultiplayerAuthority();

        GD.Print($"Player {playerId} reached the exit!");

        RoundManager.Instance.PlayerExited(playerId);
    }
}