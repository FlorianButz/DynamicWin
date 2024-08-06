using System;
using System.Collections.Generic;
using System.IO;
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

        public static float Remap(float value, float start1, float end1, float start2, float end2)
        {
            return start2 + (end2 - start2) * ((value - start1) / (end1 - start1));
        }

        public static float LimitDecimalPoints(float value, int decimalPlaces)
        {
            return (float)Math.Round(value, decimalPlaces);
        }

        public static string GetFileSizeString(string filename)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            double len = 0;
            try
            {
                len = new FileInfo(filename).Length;
            }catch(FileNotFoundException e)
            {
                return "File not found!";
            }

            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return String.Format("{0:0.#} {1}", len, sizes[order]);
        }
    }
}
