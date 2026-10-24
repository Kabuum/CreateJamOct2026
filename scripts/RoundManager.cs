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

    private bool roundStarted = false;
    private bool roundEnded = false;

    public override void _Ready()
    {
        Instance = this;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
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

        StartSpectating();
    }

    private void StartSpectating()
    {
        if (spectatorCamera != null)
        {
            return;
        }

        spectatorCamera = new Camera3D();

        GetTree().CurrentScene.AddChild(spectatorCamera);

        spectatorCamera.MakeCurrent();

        GD.Print("Spectator mode enabled!");
    }

    public override void _Process(double delta)
    {
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

        if (spectatorCamera != null)
        {
            spectatorCamera.QueueFree();
            spectatorCamera = null;
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

        // Disconnect from multiplayer
        Multiplayer.MultiplayerPeer = null;

        // Clear scores and inventories for the next round
        GameManager.Instance.ResetRoundData();

        // Reload NetworkScene, including its existing Host/Join menu
        GetTree().ReloadCurrentScene();
    }
}