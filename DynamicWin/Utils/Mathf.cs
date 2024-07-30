using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    public class Mathf
    {
        public static float Lerp(float a, float b, float f)
        {
            float fClamped = Clamp(f, 0, 1);

            return LimitDecimalPoints(a * (1.0f - fClamped) + (b * fClamped), 4);
        }

        public static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        public static float LimitDecimalPoints(float value, int decimalPlaces)
        {
            return (float)Math.Round(value, decimalPlaces);
        }
    }
}
