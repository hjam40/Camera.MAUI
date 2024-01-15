using ZXing.Common;
using ZXing;
#if WINDOWS
using Windows.Graphics.Imaging;
using CustomRenderer = Camera.MAUI.ZXing.Platforms.Windows.SoftwareBitmapRenderer;
#elif IOS || MACCATALYST
using CustomRenderer = Camera.MAUI.ZXing.Platforms.Apple.BitmapRenderer;
#elif ANDROID
using CustomRenderer = Camera.MAUI.ZXing.Platforms.Android.BitmapRenderer;
#else
using CustomRenderer = System.Object;
#endif

namespace Camera.MAUI.ZXing;

public class ZXingBarcodeEncoder : IBarcodeEncoder
{
    public Color Foreground { get; set; } = Colors.Black;
    public Color Background { get; set; } = Colors.White;

    private readonly BarcodeWriterPixelData writer = new();
    private readonly CustomRenderer customRenderer = new();
    public MemoryStream EncodeBarcode(string code, BarcodeFormat format, int width, int height, int margin, Color Foreground, Color Background)
    {
        writer.Options = new EncodingOptions { Width = width, Height = height, Margin = margin };
        writer.Format = ZXingBarcodeDecoder.ToPlatform(format);
        MemoryStream stream = new();
        try
        {
            var bitMatrix = writer.Encode(code);
            if (bitMatrix != null)
            {

#if WINDOWS
                byte a, r, g, b;
                Foreground.ToRgba(out r, out g, out b, out a);
                customRenderer.Foreground = Windows.UI.Color.FromArgb(a, r, g, b);
                Background.ToRgba(out r, out g, out b, out a);
                customRenderer.Background = Windows.UI.Color.FromArgb(a, r, g, b);
                var bitmap = customRenderer.Render(bitMatrix, ZXingBarcodeDecoder.ToPlatform(format), code);
                BitmapEncoder encoder = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream()).GetAwaiter().GetResult();
                encoder.SetSoftwareBitmap(bitmap);
                encoder.FlushAsync().GetAwaiter().GetResult();
                stream.Position = 0;
#elif IOS || MACCATALYST
                customRenderer.Foreground = new CoreGraphics.CGColor(Foreground.Red, Foreground.Green, Foreground.Blue, Foreground.Alpha);
                customRenderer.Background = new CoreGraphics.CGColor(Background.Red, Background.Green, Background.Blue, Background.Alpha);
                var bitmap = customRenderer.Render(bitMatrix, ZXingBarcodeDecoder.ToPlatform(format), code);
                bitmap.AsPNG().AsStream().CopyTo(stream);
                stream.Position = 0;
#elif ANDROID
                byte a, r, g, b;
                Foreground.ToRgba(out r, out g, out b, out a);
                customRenderer.Foreground = new Android.Graphics.Color(r, g, b, a);
                Background.ToRgba(out r, out g, out b, out a);
                customRenderer.Background = new Android.Graphics.Color(r, g, b, a);
                var bitmap = customRenderer.Render(bitMatrix, ZXingBarcodeDecoder.ToPlatform(format), code);
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                stream.Position = 0;
#endif
            }
        }
        catch
        {
        }

        return stream;
    }
}
