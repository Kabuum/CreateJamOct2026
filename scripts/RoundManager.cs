using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class RoundManager : Node
{
    public static RoundManager Instance { get; private set; }

    // Players participating in the round
    private readonly HashSet<int> players = new();

    // True = escaped, False = caught
    private readonly Dictionary<int, bool> finishedPlayers = new();

    private Camera3D spectatorCamera;
	private CanvasLayer spectatorHud;
	private Label spectatorLabel;
	private bool spectatorEscaped;
	private Timer gameTimer;
	private bool gatesOpened = false;
    private bool roundStarted = false;
    private bool roundEnded = false;

    private bool hudActive = false;
    private int lastSentSecond = -1;
	
	private CanvasLayer gameHud;
	private Label timerLabel;
	private Label scoreLabel;
	private Label gatesLabel;

    public override void _Ready()
    {
        Instance = this;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
		gameTimer = GetNode<Timer>("GameTimer");
        gameTimer.OneShot = true;
        gameTimer.Autostart = false;
        gameTimer.Stop();

		// Get existing HUD
		gameHud = GetNode<CanvasLayer>("../HUD");

		timerLabel = gameHud.GetNode<Label>("TimerLabel");
		scoreLabel = gameHud.GetNode<Label>("ScoreLabel");
		gatesLabel = gameHud.GetNodeOrNull<Label>("GatesLabel");

        if (gatesLabel == null)
        {
            GD.PushWarning("RoundManager: HUD/GatesLabel is missing. Timer and score will still work.");
        }

        gameTimer.Timeout += OnGameTimerTimeout;

		// Hide HUD until the round starts
		gameHud.Visible = false;
    }

    public override void _ExitTree()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
    }

    public void StartRound()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        // Prevent the same round from starting twice
        if (roundStarted)
        {
            return;
        }
        
        players.Clear();
        finishedPlayers.Clear();

        roundStarted = true;
        roundEnded = false;

        // Register the players already spawned
        Node playerContainer = GetNode<Node>("../Players");

        foreach (Node player in playerContainer.GetChildren())
        {
            if (int.TryParse(player.Name.ToString(), out int playerId))
            {
                players.Add(playerId);
            }
        }
		// Reset the gates for the round
		gatesOpened = false;

		// Start the match countdown
        lastSentSecond = -1;
		gameTimer.Start();

        // Tell every client (and the host) to display the HUD
        Rpc(MethodName.ShowGameHud, Mathf.CeilToInt(gameTimer.TimeLeft));

		GD.Print("Round started! Countdown is running.");
    }

    private void OnPeerConnected(long peerId)
    {
        if (!Multiplayer.IsServer() || !roundStarted || roundEnded)
        {
            return;
        }

        players.Add((int)peerId);
    }

    private void OnPeerDisconnected(long peerId)
    {
        if (!Multiplayer.IsServer() || !roundStarted || roundEnded)
        {
            return;
        }

        // Disconnected players no longer participate
        players.Remove((int)peerId);
        finishedPlayers.Remove((int)peerId);

        CheckRoundFinished();
    }

    public void PlayerExited(int playerId)
    {
        FinishPlayer(playerId, true);
    }

    public void PlayerCaught(int playerId)
    {
        FinishPlayer(playerId, false);
    }

    private void FinishPlayer(int playerId, bool escaped)
    {
        if (!Multiplayer.IsServer() || !roundStarted || roundEnded)
        {
            return;
        }

        if (!players.Contains(playerId) ||
            finishedPlayers.ContainsKey(playerId))
        {
            return;
        }

        finishedPlayers[playerId] = escaped;

        GD.Print(
            escaped
                ? $"Player {playerId} escaped!"
                : $"Player {playerId} was caught!"
        );

        // Notify everybody that this player finished
        Rpc(MethodName.OnPlayerFinished, playerId, escaped);

        // Remove the finished player from the level
        CharacterBody3D player =
            GetNodeOrNull<CharacterBody3D>($"../Players/{playerId}");

        player?.QueueFree();

        CheckRoundFinished();
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void OnPlayerFinished(int playerId, bool escaped)
    {
        finishedPlayers[playerId] = escaped;

        // Only the player who finished starts spectating
		if (playerId != Multiplayer.GetUniqueId())
		{
			return;
		}

		StartSpectating(escaped);
    }

	private void StartSpectating(bool escaped)
	{
		if (spectatorCamera != null)
		{
			return;
		}

		spectatorEscaped = escaped;

		// Create spectator camera
		spectatorCamera = new Camera3D();

		GetTree().CurrentScene.AddChild(spectatorCamera);

		spectatorCamera.MakeCurrent();

		// Create spectator HUD
		spectatorHud = new CanvasLayer();
		spectatorHud.Layer = 10;

		AddChild(spectatorHud);

		// Create notification label
		spectatorLabel = new Label();

		spectatorLabel.HorizontalAlignment = HorizontalAlignment.Center;
		spectatorLabel.VerticalAlignment = VerticalAlignment.Center;
		spectatorLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

		spectatorHud.AddChild(spectatorLabel);

		// Position notification at the top of the screen
		spectatorLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);

		spectatorLabel.OffsetTop = 20;
		spectatorLabel.OffsetBottom = 110;

		// Text appearance
		spectatorLabel.AddThemeFontSizeOverride("font_size", 32);
		spectatorLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		spectatorLabel.AddThemeConstantOverride("outline_size", 6);

		// Different colors depending on outcome
		if (escaped)
		{
			spectatorLabel.AddThemeColorOverride("font_color", Colors.LightGreen);
			spectatorLabel.Text = "YOU ESCAPED!\nSpectating...";
		}
		else
		{
			spectatorLabel.AddThemeColorOverride("font_color", Colors.OrangeRed);
			spectatorLabel.Text = "YOU WERE CAUGHT!\nSpectating...";
		}

		GD.Print("Spectator mode enabled!");
	}

    public override void _Process(double delta)
    {
        UpdateLocalScore();

        if (spectatorCamera == null || roundEnded)
        {
            return;
        }

        Node playerContainer = GetNode<Node>("../Players");

        CharacterBody3D target = null;

        foreach (Node node in playerContainer.GetChildren())
        {
            if (node is not CharacterBody3D player)
            {
                continue;
            }

            if (!int.TryParse(player.Name.ToString(), out int playerId))
            {
                continue;
            }

            if (finishedPlayers.ContainsKey(playerId) ||
                player.IsQueuedForDeletion())
            {
                continue;
            }

            target = player;
            break;
        }

		if (target == null)
		{
			return;
		}

		// Update spectator notification
		if (spectatorLabel != null)
		{
			string status = spectatorEscaped
				? "YOU ESCAPED!"
				: "YOU WERE CAUGHT!";

			spectatorLabel.Text = $"{status}\nSpectating Player {target.Name}";
		}

		// Position our spectator camera behind the target
		Vector3 offset = new Vector3(0, 3, 6);

		Vector3 cameraPosition =
			target.GlobalPosition +
			target.GlobalTransform.Basis * offset;

		spectatorCamera.GlobalPosition =
			spectatorCamera.GlobalPosition.Lerp(
				cameraPosition,
				Mathf.Min(1f, (float)delta * 10f)
			);

		spectatorCamera.LookAt(
			target.GlobalPosition + Vector3.Up * 1.5f,
			Vector3.Up
		);

    }

    private void CheckRoundFinished()
    {
        if (!Multiplayer.IsServer() || !roundStarted || roundEnded)
        {
            return;
        }

        if (players.Count == 0)
        {
            return;
        }

        // Don't finish until EVERY remaining player is resolved
        foreach (int playerId in players)
        {
            if (!finishedPlayers.ContainsKey(playerId))
            {
                return;
            }
        }

        roundEnded = true;
		gameTimer.Stop();

        StringBuilder results = new();

        results.AppendLine("BLACK FRIDAY RESULTS");
        results.AppendLine();

        // Highest score first
        foreach (int playerId in players.OrderByDescending(
            id => GameManager.Instance.GetScore(id)))
        {
            int score = GameManager.Instance.GetScore(playerId);

            string status =
                finishedPlayers[playerId] ? "ESCAPED" : "CAUGHT";

            results.AppendLine(
                $"Player {playerId}: {score} points - {status}"
            );
        }

        Rpc(MethodName.ShowResults, results.ToString());
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void ShowResults(string results)
    {
        roundEnded = true;
        hudActive = false;
        gameHud.Visible = false;

        if (spectatorCamera != null)
        {
            spectatorCamera.QueueFree();
            spectatorCamera = null;
        }

		if (spectatorHud != null)
		{
			spectatorHud.Hide();
			spectatorHud.QueueFree();

			spectatorHud = null;
			spectatorLabel = null;
		}

        // Results overlay
        CanvasLayer canvas = new();
        AddChild(canvas);

        ColorRect background = new()
        {
            Color = new Color(0.05f, 0.05f, 0.08f, 0.95f)
        };

        canvas.AddChild(background);
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        Label label = new()
        {
            Text = results + "\nReturning to menu in 10 seconds...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        background.AddChild(label);
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        label.AddThemeFontSizeOverride("font_size", 28);

        // Return everybody to the connection menu
        GetTree().CreateTimer(10.0).Timeout += ReturnToMenu;
    }

    private void ReturnToMenu()
    {
        if (!IsInsideTree())
        {
            return;
        }

        // Clear scores and inventories for the next round
        GameManager.Instance.ResetRoundData();

        // Let NetworkManager handle disconnecting and reloading
        GetNode("/root/NetworkManager").Call("return_to_menu");
    }
    
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void OpenGates()
	{
		// Open all gates
		foreach (Node node in GetTree().GetNodesInGroup("TimedGate"))
		{
			Node3D gate = node.GetNodeOrNull<Node3D>("exit_gate");

			if (gate != null)
			{
				gate.Hide();
			}
		}

		// Activate all exit zones
		foreach (Node node in GetTree().GetNodesInGroup("TimedExit"))
		{
			if (node is ExitZone exitZone)
			{
				exitZone.OpenExit();
			}
		}

        // The gates are open: warn all players through the existing HUD
        timerLabel.Modulate = Colors.Red;
        scoreLabel.Modulate = Colors.Red;

        if (gatesLabel != null)
        {
            gatesLabel.Text = "The gates are now open!";
            gatesLabel.Modulate = Colors.Red;
            gatesLabel.Show();
        }

		GD.Print("All gates and exits opened!");
	}
    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void ShowGameHud(int secondsLeft)
    {
        hudActive = true;

        timerLabel.Modulate = Colors.White;
        scoreLabel.Modulate = Colors.White;
        scoreLabel.Text = "Score: 0";

        if (gatesLabel != null)
        {
            gatesLabel.Hide();
        }

        UpdateGameHudTime(secondsLeft);
        gameHud.Visible = true;
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void UpdateGameHudTime(int secondsLeft)
    {
        timerLabel.Text = $"{secondsLeft / 60:00}:{secondsLeft % 60:00}";
    }

    private void UpdateLocalScore()
    {
        if (!hudActive || roundEnded ||
            Multiplayer.MultiplayerPeer == null ||
            Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer)
        {
            return;
        }

        int playerId = Multiplayer.GetUniqueId();
        scoreLabel.Text = $"Score: {GameManager.Instance.GetScore(playerId)}";
    }

    private void OnGameTimerTimeout()
    {
        if (!roundStarted || roundEnded ||
            Multiplayer.MultiplayerPeer == null ||
            !Multiplayer.IsServer())
        {
            return;
        }

        Rpc(MethodName.UpdateGameHudTime, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!roundStarted || roundEnded)
        {
            return;
        }

        if (Multiplayer.MultiplayerPeer == null ||
            Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer ||
            !Multiplayer.IsServer())
        {
            return;
        }

        if (gameTimer == null || gameTimer.IsStopped())
        {
            return;
        }

        int secondsLeft = Mathf.CeilToInt(gameTimer.TimeLeft);

        if (secondsLeft != lastSentSecond)
        {
            lastSentSecond = secondsLeft;
            Rpc(MethodName.UpdateGameHudTime, secondsLeft);
        }

        if (!gatesOpened && gameTimer.TimeLeft <= 20)
        {
            gatesOpened = true;
            Rpc(MethodName.OpenGates);
        }
    }
}
