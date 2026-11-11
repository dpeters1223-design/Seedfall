# T001 — Probe movement

**Goal:** A probe moves from one point toward another at a fixed speed and stops exactly on arrival.

## Context

This is the first piece of the core simulation. Keep it pure C# game logic: no `MonoBehaviour`, no scenes, no `Time.deltaTime`. The caller passes elapsed time in, so the class can be tested in edit mode and later run on the server.

## Files

- Create `unity/Assets/Seedfall/Core/Probe.cs`
- Create `unity/Assets/Seedfall/Tests/EditMode/ProbeMovementTests.cs`
- Do not modify any other file.

## Contract

Namespace `Seedfall.Core`. You may use `UnityEngine.Vector3`.

```csharp
public class Probe
{
    public Probe(string id, Vector3 startPosition, float speed); // speed in units per second, must be > 0
    public string Id { get; }
    public Vector3 Position { get; }
    public float Speed { get; }
    public Vector3? Destination { get; }      // null when idle
    public bool IsMoving { get; }             // true while Destination is set

    public void SetDestination(Vector3 target);
    public void Tick(float deltaSeconds);     // advance the simulation
}
```

## Behaviour

1. `SetDestination` stores the target. `IsMoving` becomes true, unless the target equals the current position. In that case nothing happens and the probe stays idle.
2. `Tick(dt)` moves the probe in a straight line toward `Destination` by `Speed * dt`.
3. If that step would reach or pass the destination, set `Position` exactly to the destination, clear `Destination` to null and set `IsMoving` to false. Never overshoot.
4. `Tick` with no destination does nothing.
5. The constructor throws `ArgumentOutOfRangeException` if `speed <= 0`, and `ArgumentException` if `id` is null or empty.
6. `Tick` throws `ArgumentOutOfRangeException` if `deltaSeconds < 0`. A `Tick(0)` does nothing.

## Required tests (minimum)

- A probe at (0,0,0) with speed 2 heading to (10,0,0): after `Tick(1)`, it is at (2,0,0) and still moving.
- The same probe after a total of 5 seconds of ticks is at exactly (10,0,0) and no longer moving.
- One big `Tick(100)` lands exactly on the destination, without overshooting.
- A diagonal move covers the correct distance per tick (compare with a tolerance of 1e-4).
- The constructor rejects speed 0, negative speed and an empty id.
- `Tick(-1)` throws.
- Changing the destination mid-flight redirects from the current position.

## Out of scope

Fuel, acceleration, collisions, pathfinding, networking, rendering.
