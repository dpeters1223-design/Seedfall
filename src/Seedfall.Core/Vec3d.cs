using System;
using System.Globalization;

namespace Seedfall.Core
{
    public readonly struct Vec3d : System.IEquatable<Vec3d>
    {
        public Vec3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Vec3d Zero => new Vec3d(0, 0, 0);

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double LengthSquared => X * X + Y * Y + Z * Z;

        public Vec3d Normalized()
        {
            if (LengthSquared == 0)
                throw new InvalidOperationException("Cannot normalize a zero vector.");
            double factor = 1 / Length;
            return new Vec3d(X * factor, Y * factor, Z * factor);
        }

        public static double Dot(Vec3d a, Vec3d b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vec3d Cross(Vec3d a, Vec3d b)
        {
            return new Vec3d(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public static double Distance(Vec3d a, Vec3d b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
        }

        public static Vec3d Lerp(Vec3d a, Vec3d b, double t)
        {
            return new Vec3d(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        public static Vec3d operator +(Vec3d a, Vec3d b)
        {
            return new Vec3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3d operator -(Vec3d a, Vec3d b)
        {
            return new Vec3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3d operator -(Vec3d v)
        {
            return new Vec3d(-v.X, -v.Y, -v.Z);
        }

        public static Vec3d operator *(Vec3d v, double s)
        {
            return new Vec3d(v.X * s, v.Y * s, v.Z * s);
        }

        public static Vec3d operator *(double s, Vec3d v)
        {
            return new Vec3d(s * v.X, s * v.Y, s * v.Z);
        }

        public static Vec3d operator /(Vec3d v, double s)
        {
            return new Vec3d(v.X / s, v.Y / s, v.Z / s);
        }

        public static bool operator ==(Vec3d a, Vec3d b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vec3d a, Vec3d b)
        {
            return !(a == b);
        }

        public bool Equals(Vec3d other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Vec3d v)
            {
                return this == v;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X.GetHashCode(), Y.GetHashCode(), Z.GetHashCode());
        }

        public override string ToString()
        {
            return $"({X.ToString("R", CultureInfo.InvariantCulture)}, {Y.ToString("R", CultureInfo.InvariantCulture)}, {Z.ToString("R", CultureInfo.InvariantCulture)})";
        }
    }
}
