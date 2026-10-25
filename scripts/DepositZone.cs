using Godot;

public partial class DepositZone : Area3D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}

		if (body is not CharacterBody3D player || !player.GetCollisionLayerValue(3))
		{
			return;
		}

		int playerId = player.GetMultiplayerAuthority();

		GD.Print($"Player {playerId} entered deposit zone!");
		player.Rpc("play_cashier");
		GameManager.Instance.DepositItems(playerId);
	}
}
