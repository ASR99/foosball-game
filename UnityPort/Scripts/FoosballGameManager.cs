using System;
using UnityEngine;

public enum FoosballPlatform
{
    None,
    Desktop,
    Mobile
}

public enum FoosballMode
{
    None,
    SinglePlayer,
    TwoPlayer,
    Online
}

public enum FoosballGameState
{
    PlatformSelect,
    Menu,
    Waiting,
    Release,
    Playing,
    Goal,
    Paused,
    GameOver
}

public enum ReleaseType
{
    Fast,
    Slow,
    Spin
}

public interface INetworkProvider
{
    bool IsConnected { get; }
    int MyPlayerId { get; } // 1 or 2
    void Connect();
    void Disconnect();
    void SendRodSync(float[] yOffsets);
    void SendBallSync(Vector2 pos, Vector2 vel, float spin);
    void SendReleasePick(ReleaseType type);
    void SendGoalSync(int scoreP1, int scoreP2, int goalTeam, int releaseTeam);
}

public class FoosballGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FoosballInputManager inputManager;
    [SerializeField] private FoosballPhysics physics;

    [Header("Match Rules")]
    [SerializeField] private int winScore = 7;

    public FoosballPlatform Platform { get; private set; } = FoosballPlatform.None;
    public FoosballMode Mode { get; private set; } = FoosballMode.None;
    public FoosballGameState State { get; private set; } = FoosballGameState.PlatformSelect;

    public int ScoreP1 { get; private set; }
    public int ScoreP2 { get; private set; }
    public int WinnerTeam { get; private set; } = -1; // 0 = P1, 1 = P2
    public int ReleaseTeam { get; private set; } // 0 = P1 side serves, 1 = P2 side serves

    public int MyPlayerId { get; private set; } // for online: 1 or 2
    public bool IsMyReleaseTurn =>
        Mode != FoosballMode.Online ||
        (MyPlayerId == 1 && ReleaseTeam == 0) ||
        (MyPlayerId == 2 && ReleaseTeam == 1);

    public event Action<FoosballGameState> OnStateChanged;
    public event Action<int, int> OnScoreChanged;

    public INetworkProvider NetworkProvider { get; set; }

    private float goalStateTimer;

    private void Awake()
    {
        if (inputManager == null) inputManager = GetComponent<FoosballInputManager>();
        if (physics == null) physics = GetComponent<FoosballPhysics>();
    }

    private void Start()
    {
        SetState(FoosballGameState.PlatformSelect);
    }

    private void Update()
    {
        if (State == FoosballGameState.Playing)
        {
            var playerInput = inputManager.CollectInput(Platform, Mode, MyPlayerId);
            physics.TickGameplay(playerInput, Mode, MyPlayerId);
        }
        else if (State == FoosballGameState.Release && Mode == FoosballMode.SinglePlayer && ReleaseTeam == 1)
        {
            physics.TickAIRelease();
        }
        else if (State == FoosballGameState.Goal)
        {
            goalStateTimer -= Time.deltaTime;
            if (goalStateTimer <= 0f)
            {
                SetState(FoosballGameState.Release);
            }
        }
    }

    public void SelectPlatformDesktop()
    {
        Platform = FoosballPlatform.Desktop;
        SetState(FoosballGameState.Menu);
    }

    public void SelectPlatformMobile()
    {
        Platform = FoosballPlatform.Mobile;
        SetState(FoosballGameState.Menu);
    }

    public void StartSinglePlayer() => StartGame(FoosballMode.SinglePlayer);
    public void StartTwoPlayer() => StartGame(FoosballMode.TwoPlayer);
    public void StartOnline() => StartGame(FoosballMode.Online);

    public void TogglePause()
    {
        if (State == FoosballGameState.Playing) SetState(FoosballGameState.Paused);
        else if (State == FoosballGameState.Paused) SetState(FoosballGameState.Playing);
    }

    public void ResumeFromPause() => SetState(FoosballGameState.Playing);

    public void ExitToMenu()
    {
        NetworkProvider?.Disconnect();
        Mode = FoosballMode.None;
        MyPlayerId = 0;
        SetState(FoosballGameState.Menu);
    }

    public void RestartMatch()
    {
        ScoreP1 = 0;
        ScoreP2 = 0;
        WinnerTeam = -1;
        ReleaseTeam = 0;
        physics.ResetField();
        OnScoreChanged?.Invoke(ScoreP1, ScoreP2);
        SetState(FoosballGameState.Release);
    }

    public void ReleaseFast() => TryRelease(ReleaseType.Fast);
    public void ReleaseSlow() => TryRelease(ReleaseType.Slow);
    public void ReleaseSpin() => TryRelease(ReleaseType.Spin);

    public void NotifyGoal(int teamScored)
    {
        if (teamScored == 0) ScoreP1++;
        else ScoreP2++;

        ReleaseTeam = teamScored;
        OnScoreChanged?.Invoke(ScoreP1, ScoreP2);

        if (Mode == FoosballMode.Online)
        {
            NetworkProvider?.SendGoalSync(ScoreP1, ScoreP2, teamScored, ReleaseTeam);
        }

        if (ScoreP1 >= winScore || ScoreP2 >= winScore)
        {
            WinnerTeam = ScoreP1 >= winScore ? 0 : 1;
            SetState(FoosballGameState.GameOver);
            return;
        }

        goalStateTimer = 1.6f;
        SetState(FoosballGameState.Goal);
    }

    private void StartGame(FoosballMode mode)
    {
        Mode = mode;
        ScoreP1 = 0;
        ScoreP2 = 0;
        WinnerTeam = -1;
        ReleaseTeam = 0;
        physics.ResetField();
        OnScoreChanged?.Invoke(ScoreP1, ScoreP2);

        if (Mode == FoosballMode.Online)
        {
            SetState(FoosballGameState.Waiting);
            NetworkProvider?.Connect();
            return;
        }

        SetState(FoosballGameState.Release);
    }

    private void TryRelease(ReleaseType type)
    {
        if (State != FoosballGameState.Release || !IsMyReleaseTurn) return;
        physics.ReleaseBall(type, ReleaseTeam);
        if (Mode == FoosballMode.Online) NetworkProvider?.SendReleasePick(type);
        SetState(FoosballGameState.Playing);
    }

    private void SetState(FoosballGameState nextState)
    {
        State = nextState;
        OnStateChanged?.Invoke(State);
    }
}
