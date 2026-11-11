using Seedfall.Core;

namespace Seedfall.Core.Tests;

/// <summary>
/// Shared assertions for Vec3d. Owned by the controller. Builders use it, never edit it.
/// </summary>
public static class AssertVec
{
    /// <summary>Passes if every component of <paramref name="actual"/> is within <paramref name="tolerance"/> of <paramref name="expected"/>.</summary>
    public static void Near(Vec3d expected, Vec3d actual, double tolerance = 1e-9)
    {
        bool ok = Math.Abs(expected.X - actual.X) <= tolerance
               && Math.Abs(expected.Y - actual.Y) <= tolerance
               && Math.Abs(expected.Z - actual.Z) <= tolerance;
        if (!ok)
            Assert.Fail($"Expected {Fmt(expected)} within {tolerance}, but was {Fmt(actual)}");
    }

    // Formats without relying on Vec3d.ToString, so failures stay readable even if ToString is broken.
    private static string Fmt(Vec3d v) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"({v.X:R}, {v.Y:R}, {v.Z:R})");
}
