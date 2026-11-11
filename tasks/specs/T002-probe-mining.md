# T002 — Probe mining

**Goal:** A probe mining an asteroid adds ore to its cargo and removes it from the asteroid, never exceeding cargo capacity or the asteroid's remaining ore.

**Depends on:** T001 (`Probe` exists and passes its tests).

## Context

Same rules as T001: pure C# logic in `Seedfall.Core`, with time passed in by the caller. One resource type, "ore", for now.

## Files

- Create `unity/Assets/Seedfall/Core/Asteroid.cs`
- Modify `unity/Assets/Seedfall/Core/Probe.cs`: **add** the mining members below. Do not change the T001 members.
- Create `unity/Assets/Seedfall/Tests/EditMode/ProbeMiningTests.cs`
- Do not modify any other file.

## Contract

```csharp
public class Asteroid
{
    public Asteroid(string id, Vector3 position, float ore);  // ore >= 0
    public string Id { get; }
    public Vector3 Position { get; }
    public float RemainingOre { get; }
    public bool IsDepleted { get; }                            // RemainingOre <= 0
    public float Extract(float requested);                     // removes up to `requested`; returns the amount actually removed
}

// Added to Probe:
public float CargoCapacity { get; }            // set via a new constructor overload; the T001 constructor defaults it to 100
public float Ore { get; }                      // starts at 0
public float MiningRate { get; }               // ore per second; set via the new overload; the T001 constructor defaults it to 1
public float MiningRange { get; }              // set via the new overload; the T001 constructor defaults it to 5
public bool CanMine(Asteroid asteroid);        // true if within MiningRange (inclusive), the asteroid is not depleted and the cargo is not full
public float Mine(Asteroid asteroid, float deltaSeconds); // returns the ore actually gained this call
```

## Behaviour

1. `Mine` gains `min(MiningRate * dt, CargoCapacity - Ore, asteroid.RemainingOre)`. The ore comes from `asteroid.Extract`, so the total is conserved.
2. If `CanMine` is false, `Mine` returns 0 and changes nothing.
3. The probe cannot mine while `IsMoving`. In that case `Mine` returns 0.
4. `Extract` with a negative request throws `ArgumentOutOfRangeException`. `Mine` with negative `dt` throws the same.
5. A null asteroid throws `ArgumentNullException`.

## Required tests (minimum)

- Mining for 3 s at rate 1 moves 3 ore from the asteroid to the probe.
- Mining stops at cargo capacity, and the excess stays in the asteroid.
- Mining stops when the asteroid is depleted. `IsDepleted` becomes true and `CanMine` becomes false.
- Out of range: returns 0, and nothing changes.
- Exactly at `MiningRange`: mining is allowed.
- While moving: returns 0.
- Conservation: probe ore + asteroid ore is unchanged after any sequence of mine calls.
- T001's tests still pass.

## Out of scope

Multiple resource types, refining, printing, selling, and more than one probe mining the same asteroid at once.
