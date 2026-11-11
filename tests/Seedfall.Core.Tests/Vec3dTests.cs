using System.Globalization;
using Seedfall.Core;

namespace Seedfall.Core.Tests;

// Tests for T003 (tasks/specs/T003-vec3d.md). Written by the controller; builders must not modify.
public class Vec3dTests
{
    // --- construction ---

    [Test]
    public void ConstructorStoresComponents()
    {
        var v = new Vec3d(1.5, -2, 3.25);
        Assert.That(v.X, Is.EqualTo(1.5));
        Assert.That(v.Y, Is.EqualTo(-2));
        Assert.That(v.Z, Is.EqualTo(3.25));
    }

    [Test]
    public void ZeroIsAllZeros()
    {
        AssertVec.Near(new Vec3d(0, 0, 0), Vec3d.Zero, 0);
    }

    // --- arithmetic ---

    [Test]
    public void AddIsComponentWise()
    {
        AssertVec.Near(new Vec3d(5, 7, 9), new Vec3d(1, 2, 3) + new Vec3d(4, 5, 6));
    }

    [Test]
    public void SubtractIsComponentWise()
    {
        AssertVec.Near(new Vec3d(-3, -3, -3), new Vec3d(1, 2, 3) - new Vec3d(4, 5, 6));
    }

    [Test]
    public void UnaryMinusNegatesEveryComponent()
    {
        AssertVec.Near(new Vec3d(-1, 2, -3), -new Vec3d(1, -2, 3));
    }

    [Test]
    public void ScalarMultiplyWorksOnBothSides()
    {
        AssertVec.Near(new Vec3d(2, 4, 6), new Vec3d(1, 2, 3) * 2);
        AssertVec.Near(new Vec3d(2, 4, 6), 2 * new Vec3d(1, 2, 3));
    }

    [Test]
    public void ScalarDivideIsComponentWise()
    {
        AssertVec.Near(new Vec3d(0.5, 1, 1.5), new Vec3d(1, 2, 3) / 2);
    }

    [Test]
    public void DivideByZeroFollowsIeee()
    {
        var v = new Vec3d(1, -1, 0) / 0;
        Assert.That(double.IsPositiveInfinity(v.X), Is.True);
        Assert.That(double.IsNegativeInfinity(v.Y), Is.True);
        Assert.That(double.IsNaN(v.Z), Is.True);
    }

    // --- length ---

    [Test]
    public void LengthOfThreeFourZeroIsFive()
    {
        var v = new Vec3d(3, 4, 0);
        Assert.That(v.Length, Is.EqualTo(5).Within(1e-12));
        Assert.That(v.LengthSquared, Is.EqualTo(25).Within(1e-12));
    }

    [Test]
    public void LengthKeepsDoublePrecisionAtLargeScale()
    {
        // 1e15 + 1 is exactly representable in double but not in float.
        var a = new Vec3d(1e15, 0, 0);
        var b = new Vec3d(1e15 + 1, 0, 0);
        Assert.That(Vec3d.Distance(a, b), Is.EqualTo(1).Within(1e-6));
    }

    // --- normalize ---

    [Test]
    public void NormalizedThreeFourZero()
    {
        AssertVec.Near(new Vec3d(0.6, 0.8, 0), new Vec3d(3, 4, 0).Normalized(), 1e-12);
    }

    [Test]
    public void NormalizedHasUnitLength()
    {
        var n = new Vec3d(-7.25, 3, 1e6).Normalized();
        Assert.That(n.Length, Is.EqualTo(1).Within(1e-12));
    }

    [Test]
    public void NormalizedOfZeroThrows()
    {
        Assert.Throws<InvalidOperationException>(() => Vec3d.Zero.Normalized());
    }

    // --- products and distance ---

    [Test]
    public void DotProduct()
    {
        Assert.That(Vec3d.Dot(new Vec3d(1, 2, 3), new Vec3d(4, -5, 6)), Is.EqualTo(12).Within(1e-12));
    }

    [Test]
    public void CrossOfBasisIsRightHanded()
    {
        AssertVec.Near(new Vec3d(0, 0, 1), Vec3d.Cross(new Vec3d(1, 0, 0), new Vec3d(0, 1, 0)), 0);
        AssertVec.Near(new Vec3d(1, 0, 0), Vec3d.Cross(new Vec3d(0, 1, 0), new Vec3d(0, 0, 1)), 0);
    }

    [Test]
    public void CrossIsAntiCommutative()
    {
        var a = new Vec3d(1, 2, 3);
        var b = new Vec3d(-4, 0.5, 2);
        AssertVec.Near(-Vec3d.Cross(a, b), Vec3d.Cross(b, a), 1e-12);
    }

    [Test]
    public void DistanceBetweenPoints()
    {
        Assert.That(Vec3d.Distance(new Vec3d(1, 1, 1), new Vec3d(4, 5, 1)), Is.EqualTo(5).Within(1e-12));
    }

    // --- lerp ---

    [Test]
    public void LerpEndpointsAndMidpoint()
    {
        var a = new Vec3d(0, 10, -2);
        var b = new Vec3d(4, 20, 2);
        AssertVec.Near(a, Vec3d.Lerp(a, b, 0), 0);
        AssertVec.Near(b, Vec3d.Lerp(a, b, 1), 1e-12);
        AssertVec.Near(new Vec3d(2, 15, 0), Vec3d.Lerp(a, b, 0.5), 1e-12);
    }

    [Test]
    public void LerpIsNotClamped()
    {
        AssertVec.Near(new Vec3d(20, 0, 0), Vec3d.Lerp(Vec3d.Zero, new Vec3d(10, 0, 0), 2), 1e-12);
    }

    // --- equality ---

    [Test]
    public void EqualVectorsAreEqual()
    {
        var a = new Vec3d(1, 2, 3);
        var b = new Vec3d(1, 2, 3);
        Assert.That(a == b, Is.True);
        Assert.That(a != b, Is.False);
        Assert.That(a.Equals(b), Is.True);
        Assert.That(a.Equals((object)b), Is.True);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void EqualityIsExact()
    {
        var a = new Vec3d(1, 2, 3);
        var b = new Vec3d(1, 2, 3 + 1e-12);
        Assert.That(a == b, Is.False);
        Assert.That(a != b, Is.True);
        Assert.That(a.Equals(b), Is.False);
    }

    [Test]
    public void EqualsObjectOfOtherTypeIsFalse()
    {
        Assert.That(new Vec3d(1, 2, 3).Equals("(1, 2, 3)"), Is.False);
        Assert.That(new Vec3d(1, 2, 3).Equals(null), Is.False);
    }

    // --- formatting ---

    [Test]
    public void ToStringFormat()
    {
        Assert.That(new Vec3d(1, -2.5, 3).ToString(), Is.EqualTo("(1, -2.5, 3)"));
    }

    [Test]
    public void ToStringIgnoresCurrentCulture()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // uses ',' as the decimal separator
            Assert.That(new Vec3d(1, -2.5, 3).ToString(), Is.EqualTo("(1, -2.5, 3)"));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
