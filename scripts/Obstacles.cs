using Godot;

public partial class Obstacles : Area3D
{
	[Export] public Texture2D ObsticleSprite {get; set;}
	private Sprite3D sprite;
	GameManager gameManager;
	bool hit = false;
	bool IsSlowness;
 	public ObstacleData Data {get; set;}
	[Export] int spin_speed;
	private Camera3D camera;
	private float spinAngle = 0f;
	[Export] public float PushSpeed = 8f;
	private Vector3 pushDirection;
	[Export] private AudioStreamPlayer3D AudioManager;
	[Export] private AudioStream[] Sound;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		Monitoring = true;
    	Monitorable = true;
    	BodyEntered += GotHit;
		sprite = GetNode<Sprite3D>("../Sprite3D");
		camera = GetViewport().GetCamera3D();

		GD.Print("Sprite: ", sprite);
    	GD.Print("Camera: ", camera);
		RemoveAfterDelay(25);
	}

	public void FindData(ObstacleData data)
	{
		Data = data;
		GetNode<Sprite3D>("../Sprite3D").Texture = data.ObstacleSprite;
		ObsticleSprite = data.ObstacleSprite;
	}

	public override void _Process(double delta)
	{
		if(hit == true)
		{
			sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
			if(camera != null){
			sprite.LookAt(camera.GlobalPosition, Vector3.Up);
			}
			spinAngle += spin_speed * (float)delta;
			sprite.LookAt(camera.GlobalPosition, Vector3.Up);
    		sprite.RotateObjectLocal(new Vector3(0, 0, 1), spinAngle);
			GetParent<Node3D>().GlobalPosition +=
       		pushDirection * PushSpeed * (float)delta;
		}
	}

	public void GotHit(Node3D body)
	{	
		GD.Print("Hit ", body.Name);

		if(Data.ObstacleName == "Bomb")
		{
			AudioManager.Stream = Sound[1];
			AudioManager.Play();
			sprite.Texture = GD.Load<Texture2D>("res://Art/Effects/Explosion.png");
			RemoveAfterDelay(0.8f);
		}
		else
		{
			AudioManager.Stream = Sound[0];
			AudioManager.Play();
			hit = true;
			RemoveAfterDelay(3f);
		}
		pushDirection =	(GlobalPosition - body.GlobalPosition).Normalized();
		pushDirection.Y = Mathf.Max(pushDirection.Y, 0.3f);
		pushDirection.Normalized();
		gameManager.ObstacleHit(body, this, Data);;
	}

	public async void RemoveAfterDelay(float timer)
{
    await ToSignal(
        GetTree().CreateTimer(timer),
        SceneTreeTimer.SignalName.Timeout
    );

    GetParent().QueueFree();
}
}
