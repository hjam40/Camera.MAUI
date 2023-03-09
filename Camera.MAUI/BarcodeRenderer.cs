using ZXing.Common;
using ZXing;
#if WINDOWS
using Windows.Graphics.Imaging;
using CustomRenderer = Camera.MAUI.Platforms.Windows.SoftwareBitmapRenderer;
#elif IOS || MACCATALYST
using CustomRenderer = Camera.MAUI.Platforms.Apple.BitmapRenderer;
#elif ANDROID
using CustomRenderer = Camera.MAUI.Platforms.Android.BitmapRenderer;
#else
using CustomRenderer = System.Object;
#endif

namespace Camera.MAUI;

public class BarcodeRenderer
{
    public Color Foreground { get; set; } = Colors.Black;
    public Color Background { get; set; } = Colors.White;

    private BarcodeWriterPixelData writer = new();
    private CustomRenderer customRenderer = new();
    public ImageSource EncodeBarcode(string code, BarcodeFormat format = BarcodeFormat.QR_CODE, int width = 400, int height = 400, int margin = 5)
    {
        ImageSource imageSource = null;
        writer.Options = new EncodingOptions { Width = width, Height = height, Margin = margin };
        writer.Format = format;
        try
        {
            var bitMatrix = writer.Encode(code);
            if (bitMatrix != null)
            {
                MemoryStream stream = new MemoryStream();
#if WINDOWS
                byte a, r, g, b;
                Foreground.ToRgba(out r, out g, out b, out a);
                customRenderer.Foreground = Windows.UI.Color.FromArgb(a, r, g, b);
                Background.ToRgba(out r, out g, out b, out a);
                customRenderer.Background = Windows.UI.Color.FromArgb(a, r, g, b);
                var bitmap = customRenderer.Render(bitMatrix, format, code);
                BitmapEncoder encoder = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream()).GetAwaiter().GetResult();
                encoder.SetSoftwareBitmap(bitmap);
                encoder.FlushAsync().GetAwaiter().GetResult();
                stream.Position = 0;
                imageSource = ImageSource.FromStream(()=>stream);
#elif IOS || MACCATALYST
                customRenderer.Foreground = new CoreGraphics.CGColor(Foreground.Red, Foreground.Green, Foreground.Blue, Foreground.Alpha);
                customRenderer.Background = new CoreGraphics.CGColor(Background.Red, Background.Green, Background.Blue, Background.Alpha);
                var bitmap = customRenderer.Render(bitMatrix, format, code);
                bitmap.AsPNG().AsStream().CopyTo(stream);
                stream.Position = 0;
                imageSource = ImageSource.FromStream(() => stream);
#elif ANDROID
                byte a, r, g, b;
                Foreground.ToRgba(out r, out g, out b, out a);
                customRenderer.Foreground = new Android.Graphics.Color(r, g, b, a);
                Background.ToRgba(out r, out g, out b, out a);
                customRenderer.Background = new Android.Graphics.Color(r, g, b, a);
                var bitmap = customRenderer.Render(bitMatrix, format, code);
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                stream.Position = 0;
                imageSource = ImageSource.FromStream(() => stream);
#endif
            }
        }
        catch
        {
        }
        return imageSource;
    }
}
