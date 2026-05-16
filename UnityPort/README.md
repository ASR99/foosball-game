# Foosball Unity Port

This folder contains a Unity-ready conversion scaffold for your `index.html` foosball game.

## What Is Ported

- Game states: `PlatformSelect`, `Menu`, `Waiting`, `Release`, `Playing`, `Goal`, `Paused`, `GameOver`
- Modes: `SinglePlayer`, `TwoPlayer`, `Online`
- Core mechanics:
  - Rod movement by team
  - Kick cooldown
  - Ball release types (`Fast`, `Slow`, `Spin`)
  - Ball physics + wall/goal collision
  - Goal scoring and win at 7
  - Basic AI for single-player

## What Is Not Included Yet

- Final UI art and polished animations
- Networking implementation (interface ready, provider TODO)
- Production touch UI visuals

## Unity Scene Setup

1. Create a new Unity 2D project.
2. Create an empty `GameObject` named `GameRoot`.
3. Attach these scripts:
   - `FoosballGameManager`
   - `FoosballInputManager`
   - `FoosballPhysics`
4. In Inspector:
   - Assign `FoosballInputManager` and `FoosballPhysics` references into `FoosballGameManager`.
   - Assign `FoosballGameManager` reference into `FoosballPhysics`.
   - Set table dimensions to mirror HTML values:
     - Width: `900`
     - Height: `540`
     - Table Rect: `x=85, y=72, w=730, h=396`
     - Goal height: `128`
5. Add simple UI Buttons and call:
   - `FoosballGameManager.SelectPlatformDesktop()`
   - `FoosballGameManager.SelectPlatformMobile()`
   - `FoosballGameManager.StartSinglePlayer()`
   - `FoosballGameManager.StartTwoPlayer()`
   - `FoosballGameManager.StartOnline()`
   - `FoosballGameManager.ReleaseFast() / ReleaseSlow() / ReleaseSpin()`
   - `FoosballGameManager.TogglePause()`
6. Render rods/players/ball with sprites or gizmos, using positions exposed by the scripts.

## Mapping From HTML

- `gst` -> `FoosballGameState`
- `mode` -> `FoosballMode`
- `releaseBall(type)` -> `FoosballPhysics.ReleaseBall(ReleaseType type)`
- `handleInput()` -> `FoosballInputManager.CollectInput()`
- `updateBall()` -> `FoosballPhysics.TickBall()`
- `aiUpdate()` -> `FoosballPhysics.TickAI()`

## Networking

Use `INetworkProvider` in `FoosballGameManager` for Online mode integration (Netcode for GameObjects, Mirror, Photon, etc).
