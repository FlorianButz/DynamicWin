using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing.Imaging;

namespace DynamicWin.Utils
{
    public static class BitmapExtensions
    {
        public static SKBitmap ToSKBitmap(this Bitmap bitmap)
        {
            var info = new SKImageInfo(bitmap.Width, bitmap.Height);
            var skiaBitmap = new SKBitmap(info);
            using (var pixmap = skiaBitmap.PeekPixels())
            {
                bitmap.ToSKPixmap(pixmap);
            }
            return skiaBitmap;
        }

        public static SKImage ToSKImage(this Bitmap bitmap)
        {
            var info = new SKImageInfo(bitmap.Width, bitmap.Height);
            var image = SKImage.Create(info);
            using (var pixmap = image.PeekPixels())
            {
                bitmap.ToSKPixmap(pixmap);
            }
            return image;
        }

        public static void ToSKPixmap(this Bitmap bitmap, SKPixmap pixmap)
        {
            if (pixmap.ColorType == SKImageInfo.PlatformColorType)
            {
                var info = pixmap.Info;
                using (var tempBitmap = new Bitmap(info.Width, info.Height, info.RowBytes, PixelFormat.Format32bppPArgb, pixmap.GetPixels()))
                using (var gr = Graphics.FromImage(tempBitmap))
                {
                    // Clear graphic to prevent display artifacts with transparent bitmaps
                    gr.Clear(Color.Transparent);

                    gr.DrawImageUnscaled(bitmap, 0, 0);
                }
            }
            else
            {
                // we have to copy the pixels into a format that we understand
                // and then into a desired format
                // we can still do a bit more for other cases where the color types are the same
                using (var tempImage = bitmap.ToSKImage())
                {
                    tempImage.ReadPixels(pixmap, 0, 0);
                }
            }
        }

    }
}
