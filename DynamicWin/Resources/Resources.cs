using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Resources
{
    internal class Resources
    {
        public static SKTypeface InterRegular { get => SKTypeface.FromFile("Resources\\Inter_24pt-Regular.ttf"); }
        public static SKTypeface InterBold { get => SKTypeface.FromFile("Resources\\Inter_24pt-ExtraBold.ttf"); }


        public static SKBitmap search;
        public static SKBitmap test;

        public static void Load()
        {
            using (var stream = File.OpenRead("Resources\\icons\\search.png"))
                search = SKBitmap.Decode(stream);

            using (var stream = File.OpenRead("Resources\\icons\\test.jpg"))
            {
                var image = SKImage.FromEncodedData(stream);
                test = SKBitmap.FromImage(image);
            }

            System.Diagnostics.Debug.WriteLine("Loaded Resources");
        }
    }
}
