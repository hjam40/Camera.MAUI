using Android.Graphics;
using ZXing.Common;
using ZXing.Rendering;
using Color = Android.Graphics.Color;

namespace Camera.MAUI.Plugin.ZXing.Platforms.Android;

public class BitmapRenderer : IBarcodeRenderer<Bitmap>
{
    public Color Foreground { get; set; }

    public Color Background { get; set; }

    public BitmapRenderer()
    {
        Foreground = Color.Black;
        Background = Color.White;
    }

    public Bitmap Render(BitMatrix matrix, global::ZXing.BarcodeFormat format, string content)
    {
        return Render(matrix, format, content, new EncodingOptions());
    }

    public Bitmap Render(BitMatrix matrix, global::ZXing.BarcodeFormat format, string content, EncodingOptions options)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var pixels = new int[width * height];
        var outputIndex = 0;
        var fColor = Foreground.ToArgb();
        var bColor = Background.ToArgb();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                pixels[outputIndex] = matrix[x, y] ? fColor : bColor;
                outputIndex++;
            }
        }

        var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
        bitmap.SetPixels(pixels, 0, width, 0, 0, width, height);
        return bitmap;
    }
}