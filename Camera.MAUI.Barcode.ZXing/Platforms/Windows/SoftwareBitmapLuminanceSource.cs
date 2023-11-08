using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using ZXing;

namespace Camera.MAUI.Barcode.ZXing.Platforms.Windows;

internal class SoftwareBitmapLuminanceSource : BaseLuminanceSource
{
    protected SoftwareBitmapLuminanceSource(int width, int height)
       : base(width, height)
    {
    }
    public SoftwareBitmapLuminanceSource(SoftwareBitmap softwareBitmap)
       : base(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight)
    {
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Gray8)
        {
            using SoftwareBitmap convertedSoftwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Gray8);
            convertedSoftwareBitmap.CopyToBuffer(luminances.AsBuffer());
        }
        else
        {
            softwareBitmap.CopyToBuffer(luminances.AsBuffer());
        }
    }
    protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
    {
        return new SoftwareBitmapLuminanceSource(width, height) { luminances = newLuminances };
    }
}
