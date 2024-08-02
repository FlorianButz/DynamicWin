using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicWin.Resources
{
    internal class Resources
    {
        public static SKTypeface InterRegular { get => SKTypeface.FromFile("Resources\\Inter_24pt-Regular.ttf"); }
        public static SKTypeface InterBold { get => SKTypeface.FromFile("Resources\\Inter_24pt-ExtraBold.ttf"); }

        public static SKBitmap searchIcon;
        public static SKBitmap editIcon;

        public static SKBitmap Battery;
        public static SKBitmap BatteryCharging;
        public static SKBitmap BatteryLevel_10P;
        public static SKBitmap BatteryLevel_25P;
        public static SKBitmap BatteryLevel_50P;
        public static SKBitmap BatteryLevel_75P;
        public static SKBitmap BatteryLevel_Full;
        public static SKBitmap NoBattery;

        public static void Load()
        {
            editIcon = LoadImg("Resources\\icons\\edit.png");
            searchIcon = LoadImg("Resources\\icons\\search.png");

            Battery = LoadImg("Resources\\icons\\battery\\Battery.png");
            BatteryCharging = LoadImg("Resources\\icons\\battery\\BatteryCharging.png");
            BatteryLevel_10P = LoadImg("Resources\\icons\\battery\\BatteryLevel_10P.png");
            BatteryLevel_25P = LoadImg("Resources\\icons\\battery\\BatteryLevel_25P.png");
            BatteryLevel_50P = LoadImg("Resources\\icons\\battery\\BatteryLevel_50P.png");
            BatteryLevel_75P = LoadImg("Resources\\icons\\battery\\BatteryLevel_75P.png");
            BatteryLevel_Full = LoadImg("Resources\\icons\\battery\\BatteryLevel_Full.png");
            NoBattery = LoadImg("Resources\\icons\\battery\\NoBattery.png");

            System.Diagnostics.Debug.WriteLine("Loaded Resources");
        }

        private static SKBitmap LoadImg(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var image = SKImage.FromEncodedData(stream);
                return SKBitmap.FromImage(image);
            }
        }
    }
}
