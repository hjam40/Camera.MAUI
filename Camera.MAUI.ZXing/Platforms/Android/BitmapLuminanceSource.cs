using Android.Graphics;
using ZXing;

namespace Camera.MAUI.ZXing;

public class BitmapLuminanceSource : RGBLuminanceSource
{
    public BitmapLuminanceSource(Bitmap bitmap)
       : base(bitmap.Width, bitmap.Height)
    {
        var pixels = new int[bitmap.Width * bitmap.Height];
        bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);
        var pixelBytes = new byte[pixels.Length * 4];
        Buffer.BlockCopy(pixels, 0, pixelBytes, 0, pixelBytes.Length);
        if (bitmap.HasAlpha)
        {
            CalculateLuminance(pixelBytes, BitmapFormat.RGBA32);
        }
        else
        {
            CalculateLuminance(pixelBytes, BitmapFormat.RGB32);
        }
    }
    protected BitmapLuminanceSource(int width, int height)
       : base(width, height)
    {
    }
    protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
    {
        return new BitmapLuminanceSource(width, height) { luminances = newLuminances };
    }
}
