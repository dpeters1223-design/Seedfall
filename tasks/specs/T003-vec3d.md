# T003 — Vec3d

**Goal:** A double-precision 3D vector type for the game's core library.

## Context

Unity's `Vector3` is 32-bit float and can't represent interstellar distances precisely (architecture, section 2). Every position and velocity in the simulation uses `Vec3d`. The type is unitless; units are handled by the code that uses it. It is part of `Seedfall.Core` (netstandard2.1, C# 9, no dependencies).

## Files

- Create `src/Seedfall.Core/Vec3d.cs`
- Tests (already written, don't modify): `tests/Seedfall.Core.Tests/Vec3dTests.cs`, using the helper `tests/Seedfall.Core.Tests/AssertVec.cs`
- Do not modify any other file. Do not modify the tests.

## Contract

```csharp
namespace Seedfall.Core
{
    public readonly struct Vec3d : System.IEquatable<Vec3d>
    {
        public Vec3d(double x, double y, double z);
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Vec3d Zero { get; }                    // (0, 0, 0)

        public double Length { get; }                        // Euclidean length
        public double LengthSquared { get; }
        public Vec3d Normalized();                           // unit vector in the same direction
        public static double Dot(Vec3d a, Vec3d b);
        public static Vec3d Cross(Vec3d a, Vec3d b);         // right-handed: Cross((1,0,0),(0,1,0)) = (0,0,1)
        public static double Distance(Vec3d a, Vec3d b);
        public static Vec3d Lerp(Vec3d a, Vec3d b, double t); // a + (b - a) * t, t NOT clamped

        public static Vec3d operator +(Vec3d a, Vec3d b);
        public static Vec3d operator -(Vec3d a, Vec3d b);
        public static Vec3d operator -(Vec3d v);
        public static Vec3d operator *(Vec3d v, double s);
        public static Vec3d operator *(double s, Vec3d v);
        public static Vec3d operator /(Vec3d v, double s);
        public static bool operator ==(Vec3d a, Vec3d b);
        public static bool operator !=(Vec3d a, Vec3d b);

        public bool Equals(Vec3d other);
        public override bool Equals(object? obj);
        public override int GetHashCode();
        public override string ToString();                   // "(x, y, z)", invariant culture, round-trip format "R"
    }
}
```

## Behaviour

1. All arithmetic is component-wise in `double`.
2. `Normalized()` of the zero vector throws `InvalidOperationException`. Every other vector returns `this / Length`.
3. Division by zero is **not** special-cased: it follows IEEE doubles (infinities or NaN).
4. Equality (`==`, `Equals`) is **exact** component comparison. Approximate comparison is the tests' job, not the type's.
5. `GetHashCode` must be equal for equal vectors. Combine the three components (`HashCode.Combine` is fine: it *is* available in netstandard2.1. An earlier version of this spec wrongly said it wasn't).
6. `ToString()` uses `CultureInfo.InvariantCulture` and the `"R"` format for each component: `new Vec3d(1, -2.5, 3).ToString()` is `"(1, -2.5, 3)"`.

## Tests

`tests/Seedfall.Core.Tests/Vec3dTests.cs` covers:
- construction and component access, and `Zero`
- `+`, `-`, unary `-`, scalar `*` on both sides, `/`
- `Length` and `LengthSquared` (a 3-4-0 triangle), with large values (1e15) to confirm double precision
- `Normalized()` of a 3-4-0 vector and of an arbitrary vector (unit length), and the exception for zero
- `Dot`, `Cross` (right-handed basis and anti-commutativity), `Distance`
- `Lerp` at 0, 1, 0.5 and an extrapolated t = 2
- exact equality, inequality, `Equals(object)`, `GetHashCode` for equal vectors
- `ToString` format, unaffected by the current culture

## Out of scope

Units, quaternions, matrices, conversion to Unity types.

## Known follow-ups

- `Equals` uses `==` on doubles, so a vector containing NaN is not equal to itself, which breaks the .NET `Equals` convention. That's harmless unless `Vec3d` becomes a dictionary key. If it does, switch `Equals` to `X.Equals(other.X) && ...`.
