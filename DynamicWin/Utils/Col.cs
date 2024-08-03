using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    internal class Col
    {
        public float r, g, b, a;

        public static Col White { get => new Col(1, 1, 1); }
        public static Col Transparent { get => new Col(0, 0, 0, 0); }

        public Col(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public Col Override(float r = -1, float g = -1, float b = -1, float a = -1)
        {
            if (r != -1) this.r = r;
            if (g != -1) this.g = g;
            if (b != -1) this.b = b;
            if (a != -1) this.a = a;

            return new Col(this.r, this.g, this.b, this.a);
        }

        public static Col Lerp(Col a, Col b, float t)
        {
            return new Col(
                Mathf.Lerp(a.r, b.r, t),
                Mathf.Lerp(a.g, b.g, t),
                Mathf.Lerp(a.b, b.b, t),
                Mathf.Lerp(a.a, b.a, t)
                ); ;
        }

        public SkiaSharp.SKColor Value()
        {
            return new SkiaSharp.SKColor(
                (byte)(r * 255),
                (byte)(g * 255),
                (byte)(b * 255),
                (byte)(a * 255));
        }

        public Col Inverted()
        {
            return new Col(1f - r, 1f - g, 1f - g, a);
        }

        public static Col operator *(Col a, float b)
        {
            return new Col(a.r * b, a.g * b, a.b * b);
        }
    }
}
