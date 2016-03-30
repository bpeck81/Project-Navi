using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Runtime.InteropServices;

namespace KinectFaceFollower
{
    class ColorBitmapGenerator
    {
        byte[] pixels;
        int width;
        int height;
        public WriteableBitmap Bitmap { get; private set; }

        public void Update(ColorFrame frame)
        {
            if(Bitmap == null)
            {
                width = frame.FrameDescription.Width;
                height = frame.FrameDescription.Height;
                pixels = new byte[width * height * 4];
                Bitmap = new WriteableBitmap(width, height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr32, null);
            }
            if(frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }
            Bitmap.Lock();
            Marshal.Copy(pixels, 0, Bitmap.BackBuffer, pixels.Length);
            Bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            Bitmap.Unlock();

        }
        

    }
    public static class ColorExtensions
    {
        static ColorBitmapGenerator bitmapGenerator = new ColorBitmapGenerator();
        public static WriteableBitmap ToBitmap(this ColorFrame frame)
        {
            bitmapGenerator.Update(frame);
            return bitmapGenerator.Bitmap;
        }
    }
}

