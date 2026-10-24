using Godot;

public partial class Main : Node3D
{
    private Timer _gameTimer;
    private Label _timerLabel;
    private Label scoreLabel;

    public override void _Ready()
    {
        // Get references to our nodes
        _gameTimer = GetNode<Timer>("GameTimer");
        _timerLabel = GetNode<Label>("CanvasLayer/TimerLabel");
        scoreLabel = GetNode<Label>("CanvasLayer/ScoreLabel");

        // Subscribe to the timer's timeout signal
        _gameTimer.Timeout += OnTimerTimeout;

        // Start the countdown
        _gameTimer.Start();
    }

    public override void _Process(double delta)
    {
        // Get remaining seconds
        int secondsLeft = Mathf.CeilToInt(_gameTimer.TimeLeft);

        // Convert seconds into minutes and seconds
        int minutes = secondsLeft / 60;
        int seconds = secondsLeft % 60;

        // Update our UI
        _timerLabel.Text = $"{minutes:00}:{seconds:00}";
        scoreLabel.Text = $"Score: {GameManager.Instance.Score}";
    }

    private void OnTimerTimeout()
    {
        GD.Print("TIME'S UP! BLACK FRIDAY HAS BEGUN!");
    }
}