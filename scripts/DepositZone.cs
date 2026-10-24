using Godot;

public partial class DepositZone : Area3D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup("Player"))
        {
            return;
        }

        GD.Print("Player entered deposit zone!");

        GameManager.Instance.DepositItems();
    }
}