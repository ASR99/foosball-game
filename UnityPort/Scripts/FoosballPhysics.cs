using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RodDefinition
{
    public float xOffset;
    public int team;
    public int figureCount;
}

public class FoosballPhysics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FoosballGameManager gameManager;

    [Header("World Size")]
    [SerializeField] private float worldWidth = 900f;
    [SerializeField] private float worldHeight = 540f;

    [Header("Table")]
    [SerializeField] private Rect tableRect = new Rect(85f, 72f, 730f, 396f);
    [SerializeField] private float goalHeight = 128f;

    [Header("Ball and Figures")]
    [SerializeField] private float ballRadius = 10f;
    [SerializeField] private float figureRadius = 11f;
    [SerializeField] private float maxBallSpeed = 17f;

    [Header("Rods")]
    [SerializeField] private List<RodDefinition> rodDefs = new()
    {
        new RodDefinition { xOffset = 52, team = 0, figureCount = 1 },
        new RodDefinition { xOffset = 167, team = 1, figureCount = 3 },
        new RodDefinition { xOffset = 287, team = 0, figureCount = 2 },
        new RodDefinition { xOffset = 443, team = 1, figureCount = 2 },
        new RodDefinition { xOffset = 563, team = 0, figureCount = 3 },
        new RodDefinition { xOffset = 678, team = 1, figureCount = 1 }
    };

    private class RodRuntime
    {
        public float x;
        public int team;
        public int count;
        public float spacing;
        public float yOffset;
        public float min;
        public float max;
    }

    private readonly List<RodRuntime> rods = new();
    private readonly int[] kickCooldown = new int[2];

    public Vector2 BallPosition { get; private set; }
    public Vector2 BallVelocity { get; private set; }
    public float BallSpin { get; private set; }

    private float goalY;
    private float aiReleaseTimer;

    private void Awake()
    {
        if (gameManager == null) gameManager = GetComponent<FoosballGameManager>();
        goalY = tableRect.y + (tableRect.height - goalHeight) * 0.5f;
        BuildRods();
        ResetField();
    }

    public void ResetField()
    {
        BuildRods();
        BallPosition = new Vector2(worldWidth * 0.5f, worldHeight * 0.5f);
        BallVelocity = Vector2.zero;
        BallSpin = 0f;
        kickCooldown[0] = 0;
        kickCooldown[1] = 0;
        aiReleaseTimer = 2.5f;
    }

    public void TickGameplay(FoosballPlayerInput input, FoosballMode mode, int myPlayerId)
    {
        if (input.pausePressed)
        {
            gameManager.TogglePause();
            return;
        }

        kickCooldown[0] = Mathf.Max(0, kickCooldown[0] - 1);
        kickCooldown[1] = Mathf.Max(0, kickCooldown[1] - 1);

        HandleMovementAndKick(input);

        if (mode == FoosballMode.SinglePlayer) TickAI();

        // Mirror HTML behavior: host/player-1 simulates ball during online.
        if (mode != FoosballMode.Online || myPlayerId == 1)
        {
            TickBall();
        }
    }

    public void TickAIRelease()
    {
        aiReleaseTimer -= Time.deltaTime;
        if (aiReleaseTimer > 0f) return;

        var pick = (ReleaseType)UnityEngine.Random.Range(0, 3);
        if (pick == ReleaseType.Fast) gameManager.ReleaseFast();
        else if (pick == ReleaseType.Slow) gameManager.ReleaseSlow();
        else gameManager.ReleaseSpin();

        aiReleaseTimer = 2.5f;
    }

    public void ReleaseBall(ReleaseType type, int releaseTeam)
    {
        int dir = releaseTeam == 0 ? 1 : -1;
        BallPosition = new Vector2(worldWidth * 0.5f, worldHeight * 0.5f);
        BallSpin = 0f;

        switch (type)
        {
            case ReleaseType.Fast:
                BallVelocity = new Vector2(dir * 12f, UnityEngine.Random.Range(-1f, 1f));
                break;
            case ReleaseType.Slow:
                BallVelocity = new Vector2(dir * 3f, UnityEngine.Random.Range(-1f, 1f));
                break;
            case ReleaseType.Spin:
                BallVelocity = new Vector2(dir * 6f, UnityEngine.Random.value < 0.5f ? 3.5f : -3.5f);
                BallSpin = UnityEngine.Random.value < 0.5f ? 0.14f : -0.14f;
                break;
        }
    }

    private void HandleMovementAndKick(FoosballPlayerInput input)
    {
        if (input.p1Up) MoveTeam(0, -1f);
        if (input.p1Down) MoveTeam(0, 1f);
        if (input.p1Kick) TryKick(0);

        if (input.p2Up) MoveTeam(1, -1f);
        if (input.p2Down) MoveTeam(1, 1f);
        if (input.p2Kick) TryKick(1);
    }

    private void TickAI()
    {
        foreach (var rod in rods)
        {
            if (rod.team != 1) continue;
            var closest = GetClosestFigureYToBall(rod);
            if (closest < BallPosition.y - 4f) rod.yOffset = Mathf.Clamp(rod.yOffset + 3.2f, rod.min, rod.max);
            else if (closest > BallPosition.y + 4f) rod.yOffset = Mathf.Clamp(rod.yOffset - 3.2f, rod.min, rod.max);
        }

        if (kickCooldown[1] > 0) return;
        foreach (var rod in rods)
        {
            if (rod.team != 1) continue;
            foreach (var figPos in EnumerateFigures(rod))
            {
                if (Mathf.Abs(BallPosition.x - figPos.x) < 28f &&
                    Mathf.Abs(BallPosition.y - figPos.y) < figureRadius + ballRadius + 8f)
                {
                    TryKick(1);
                    return;
                }
            }
        }
    }

    private void TickBall()
    {
        BallVelocity = new Vector2(BallVelocity.x * 0.997f, (BallVelocity.y + BallSpin) * 0.997f);
        BallSpin *= 0.97f;
        BallPosition += BallVelocity;

        // Top/bottom walls.
        if (BallPosition.y - ballRadius < tableRect.yMin)
        {
            BallPosition = new Vector2(BallPosition.x, tableRect.yMin + ballRadius);
            BallVelocity = new Vector2(BallVelocity.x, Mathf.Abs(BallVelocity.y));
        }
        if (BallPosition.y + ballRadius > tableRect.yMax)
        {
            BallPosition = new Vector2(BallPosition.x, tableRect.yMax - ballRadius);
            BallVelocity = new Vector2(BallVelocity.x, -Mathf.Abs(BallVelocity.y));
        }

        // Left goal/wall.
        if (BallPosition.x - ballRadius < tableRect.xMin)
        {
            if (BallPosition.y > goalY && BallPosition.y < goalY + goalHeight)
            {
                gameManager.NotifyGoal(1);
                return;
            }
            BallPosition = new Vector2(tableRect.xMin + ballRadius, BallPosition.y);
            BallVelocity = new Vector2(Mathf.Abs(BallVelocity.x), BallVelocity.y);
        }

        // Right goal/wall.
        if (BallPosition.x + ballRadius > tableRect.xMax)
        {
            if (BallPosition.y > goalY && BallPosition.y < goalY + goalHeight)
            {
                gameManager.NotifyGoal(0);
                return;
            }
            BallPosition = new Vector2(tableRect.xMax - ballRadius, BallPosition.y);
            BallVelocity = new Vector2(-Mathf.Abs(BallVelocity.x), BallVelocity.y);
        }

        // Figure collisions.
        foreach (var rod in rods)
        {
            foreach (var fig in EnumerateFigures(rod))
            {
                var delta = BallPosition - fig;
                float dist = delta.magnitude;
                float minDist = ballRadius + figureRadius;
                if (dist >= minDist || dist <= Mathf.Epsilon) continue;

                Vector2 n = delta / dist;
                float dot = Vector2.Dot(BallVelocity, n);
                if (dot < 0f)
                {
                    BallVelocity -= 2f * dot * n;
                }

                BallPosition += n * (minDist - dist);

                float speed = BallVelocity.magnitude;
                if (speed > 0f && speed < 3f)
                {
                    BallVelocity = BallVelocity.normalized * 3f;
                }
                CapBallSpeed();
            }
        }
    }

    private void MoveTeam(int team, float dir)
    {
        foreach (var rod in rods)
        {
            if (rod.team != team) continue;
            rod.yOffset = Mathf.Clamp(rod.yOffset + dir * 4f, rod.min, rod.max);
        }
    }

    private void TryKick(int team)
    {
        if (kickCooldown[team] > 0) return;

        float bestDist = float.PositiveInfinity;
        foreach (var rod in rods)
        {
            if (rod.team != team) continue;
            foreach (var fig in EnumerateFigures(rod))
            {
                float d = Vector2.Distance(BallPosition, fig);
                if (d < bestDist) bestDist = d;
            }
        }

        if (bestDist >= 72f) return;

        float dir = team == 0 ? 1f : -1f;
        BallVelocity = new Vector2(dir * (9f + UnityEngine.Random.value * 3f),
            BallVelocity.y + UnityEngine.Random.Range(-3f, 3f));
        CapBallSpeed();
        kickCooldown[team] = 22;
    }

    private void CapBallSpeed()
    {
        float speed = BallVelocity.magnitude;
        if (speed <= maxBallSpeed) return;
        BallVelocity = BallVelocity / speed * maxBallSpeed;
    }

    private void BuildRods()
    {
        rods.Clear();
        foreach (var def in rodDefs)
        {
            float spacing = tableRect.height / (def.figureCount + 1);
            rods.Add(new RodRuntime
            {
                x = tableRect.x + def.xOffset,
                team = def.team,
                count = def.figureCount,
                spacing = spacing,
                yOffset = 0f,
                min = -(spacing - 2f),
                max = spacing - 2f
            });
        }
    }

    private float GetClosestFigureYToBall(RodRuntime rod)
    {
        float bestDelta = float.PositiveInfinity;
        float bestY = BallPosition.y;
        foreach (var fig in EnumerateFigures(rod))
        {
            float d = Mathf.Abs(fig.y - BallPosition.y);
            if (d < bestDelta)
            {
                bestDelta = d;
                bestY = fig.y;
            }
        }
        return bestY;
    }

    private IEnumerable<Vector2> EnumerateFigures(RodRuntime rod)
    {
        for (int i = 0; i < rod.count; i++)
        {
            float y = tableRect.y + rod.spacing * (i + 1) + rod.yOffset;
            yield return new Vector2(rod.x, y);
        }
    }
}
