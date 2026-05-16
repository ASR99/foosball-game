using UnityEngine;

public struct FoosballPlayerInput
{
    public bool p1Up;
    public bool p1Down;
    public bool p1Kick;
    public bool p2Up;
    public bool p2Down;
    public bool p2Kick;
    public bool pausePressed;
}

public class FoosballInputManager : MonoBehaviour
{
    public FoosballPlayerInput CollectInput(FoosballPlatform platform, FoosballMode mode, int myPlayerId)
    {
        var input = new FoosballPlayerInput();

        if (platform == FoosballPlatform.Desktop)
        {
            input.p1Up = Input.GetKey(KeyCode.W);
            input.p1Down = Input.GetKey(KeyCode.S);
            input.p1Kick = Input.GetKey(KeyCode.Space);

            input.p2Up = Input.GetKey(KeyCode.UpArrow);
            input.p2Down = Input.GetKey(KeyCode.DownArrow);
            input.p2Kick = Input.GetKey(KeyCode.Return);

            input.pausePressed = Input.GetKeyDown(KeyCode.Escape);
        }
        else
        {
            // Mobile UI should call public methods or set these flags from UI events.
            // Keep this as desktop fallback while wiring touch buttons in Unity Canvas.
            input.p1Up = Input.GetKey(KeyCode.W);
            input.p1Down = Input.GetKey(KeyCode.S);
            input.p1Kick = Input.GetKey(KeyCode.Space);
            input.p2Up = Input.GetKey(KeyCode.UpArrow);
            input.p2Down = Input.GetKey(KeyCode.DownArrow);
            input.p2Kick = Input.GetKey(KeyCode.Return);
            input.pausePressed = Input.GetKeyDown(KeyCode.Escape);
        }

        if (mode == FoosballMode.Online)
        {
            if (myPlayerId == 1)
            {
                input.p2Up = false;
                input.p2Down = false;
                input.p2Kick = false;
            }
            else if (myPlayerId == 2)
            {
                input.p1Up = false;
                input.p1Down = false;
                input.p1Kick = false;
            }
        }
        else if (mode == FoosballMode.SinglePlayer)
        {
            input.p2Up = false;
            input.p2Down = false;
            input.p2Kick = false;
        }

        return input;
    }
}
