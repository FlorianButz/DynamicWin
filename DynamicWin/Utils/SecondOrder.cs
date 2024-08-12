using DynamicWin.Main;
using DynamicWin.Utils;
using System.Numerics;

namespace DynamicWin.Utils
{
    public class SecondOrder
    {
        private Vec2 xp;
        private Vec2 y, yd;
        private float k1, k2, k3;

        public SecondOrder(Vec2 x0, float f = 2f, float z = 0.4f, float r = 0.1f)
        {
            k1 = (float)(z / (Math.PI * f));
            k2 = (float)(1 / ((2 * Math.PI * f) * (2 * Math.PI * f)));
            k3 = (float)(r * z / (2 * Math.PI * f));

            xp = x0;
            y = x0;
            yd = new Vec2(0, 0);
        }

        public void SetValues(float f = 2f, float z = 0.4f, float r = 0.1f)
        {
            k1 = (float)(z / (Math.PI * f));
            k2 = (float)(1 / ((2 * Math.PI * f) * (2 * Math.PI * f)));
            k3 = (float)(r * z / (2 * Math.PI * f));
        }

        public Vec2 Update(float T, Vec2 x, Vec2? xd = null)
        {
            if (!Settings.AllowAnimation) return x;

            if (xd != null)
            {
                xd = (x - xp) / new Vec2(T, T);
                xp = x;
            }
            float k2_stable = (float)Math.Max(k2, Math.Max(T * T / 2 + T * k1 / 2, T * k1));
            y = y + new Vec2(T, T) * yd;
            yd = yd + T * (x + new Vec2(k3, k3) * xd - y - (k1 * yd)) / k2_stable;

            y.X = Mathf.LimitDecimalPoints(y.X, 1);
            y.Y = Mathf.LimitDecimalPoints(y.Y, 1);

            return y;
        }
    }
}
