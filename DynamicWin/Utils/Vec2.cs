using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    public class Vec2
    {
        private float x, y;

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }

        public static Vec2 zero { get => new Vec2(0, 0); }
        public static Vec2 one { get => new Vec2(1, 1); }

        public float Magnitude { get => (Math.Abs(x) + Math.Abs(y)) / 2; }

        public Vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static float Distance(Vec2 v1, Vec2 v2)
        {
            return (float)Math.Sqrt(Math.Pow(v2.x - v1.x, 2) + Math.Pow(v2.y - v1.y, 2));
        }

        public Vec2 Normalized()
        {
            float length = (float)Math.Sqrt(x * x + y * y);

            if (length == 0)
            {
                return Vec2.zero;
            }

            return new Vec2(x / length, y / length);
        }

        public static Vec2 lerp(Vec2 a, Vec2 b, float t)
        {
            return new Vec2(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t));
        }

        // Operators

        // Vec2 and Vec2

        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            if (a == null && b != null) return b;
            else if (a != null && b == null) return a;
            else if (a == null && b == null) return Vec2.zero;

            return new Vec2(a.x + b.x, a.y + b.y);
        }

        public static Vec2 operator *(Vec2 a, Vec2 b)
        {
            if (a == null && b != null) return b;
            else if (a != null && b == null) return a;
            else if (a == null && b == null) return Vec2.zero;

            return new Vec2(a.x * b.x, a.y * b.y);
        }

        public static Vec2 operator /(Vec2 a, Vec2 b)
        {
            if (a == null && b != null) return b;
            else if (a != null && b == null) return a;
            else if (a == null && b == null) return Vec2.zero;

            return new Vec2(a.x / b.x, a.y / b.y);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            if (a == null && b != null) return b;
            else if (a != null && b == null) return a;
            else if (a == null && b == null) return Vec2.zero;

            return new Vec2(a.x - b.x, a.y - b.y);
        }

        // Vec2 and float

        public static Vec2 operator +(Vec2 a, float b)
        {
            return new Vec2(a.x + b, a.y + b);
        }

        public static Vec2 operator *(Vec2 a, float b)
        {
            return new Vec2(a.x * b, a.y * b);
        }

        public static Vec2 operator /(Vec2 a, float b)
        {
            return new Vec2(a.x / b, a.y / b);
        }

        public static Vec2 operator -(Vec2 a, float b)
        {
            return new Vec2(a.x - b, a.y - b);
        }

        // The other way around (Needed for some reason, WTF CSharp?)

        public static Vec2 operator +(float b, Vec2 a)
        {
            return new Vec2(a.x + b, a.y + b);
        }

        public static Vec2 operator *(float b, Vec2 a)
        {
            return new Vec2(a.x * b, a.y * b);
        }

        public static Vec2 operator /(float b, Vec2 a)
        {
            return new Vec2(a.x / b, a.y / b);
        }

        public static Vec2 operator -(float b, Vec2 a)
        {
            return new Vec2(a.x - b, a.y - b);
        }
    }
}
