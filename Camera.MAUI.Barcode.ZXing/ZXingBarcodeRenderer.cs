using ZXing.Common;
using ZXing;
using Camera.MAUI.Barcode;
using BarcodeFormat = Camera.MAUI.Barcode.BarcodeFormat;
using Camera.MAUI.Barcode.ZXing;

#if WINDOWS
using Windows.Graphics.Imaging;
using CustomRenderer = Camera.MAUI.Barcode.ZXing.Platforms.Windows.SoftwareBitmapRenderer;
#elif IOS || MACCATALYST
using CustomRenderer = Camera.MAUI.Barcode.ZXing.Platforms.MaciOS.BitmapRenderer;
#elif ANDROID
using CustomRenderer = Camera.MAUI.Barcode.ZXing.Platforms.Android.BitmapRenderer;
#else

using CustomRenderer = System.Object;

#endif

namespace Camera.MAUI.Barcode.ZXing;

public class ZXingBarcodeRenderer : IBarcodeRenderer
{
    #region Private Fields

    private CustomRenderer customRenderer = new();
    private BarcodeWriterPixelData writer = new();

    #endregion Private Fields

    #region Public Properties

    public Color Background { get; set; } = Colors.White;
    public Color Foreground { get; set; } = Colors.Black;

    #endregion Public Properties

    #region Public Methods

    public ImageSource EncodeBarcode(string code, BarcodeFormat format = BarcodeFormat.QR_CODE, int width = 400, int height = 400, int margin = 5)
    {
        ImageSource imageSource = null;
        writer.Options = new EncodingOptions { Width = width, Height = height, Margin = margin };
        writer.Format = format.ToPlatform();
        try
        {
            var bitMatrix = writer.Encode(code);
            if (bitMatrix != null)
            {
                var stream = new MemoryStream();
#if WINDOWS
                byte a, r, g, b;
                Foreground.ToRgba(out r, out g, out b, out a);
                customRenderer.Foreground = Windows.UI.Color.FromArgb(a, r, g, b);
                Background.ToRgba(out r, out g, out b, out a);
                customRenderer.Background = Windows.UI.Color.FromArgb(a, r, g, b);
                var bitmap = customRenderer.Render(bitMatrix, format.ToPlatform(), code);
                BitmapEncoder encoder = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream()).GetAwaiter().GetResult();
                encoder.SetSoftwareBitmap(bitmap);
                encoder.FlushAsync().GetAwaiter().GetResult();
                stream.Position = 0;
                imageSource = ImageSource.FromStream(()=>stream);
#elif IOS || MACCATALYST
                customRenderer.Foreground = new CoreGraphics.CGColor(Foreground.Red, Foreground.Green, Foreground.Blue, Foreground.Alpha);
                customRenderer.Background = new CoreGraphics.CGColor(Background.Red, Background.Green, Background.Blue, Background.Alpha);
                var bitmap = customRenderer.Render(bitMatrix, format.ToPlatform(), code);
                bitmap.AsPNG().AsStream().CopyTo(stream);
                stream.Position = 0;
                imageSource = ImageSource.FromStream(() => stream);
#elif ANDROID
                byte a, r, g, b;
                Foreground.ToRgba(out r, out g, out b, out a);
                customRenderer.Foreground = new Android.Graphics.Color(r, g, b, a);
                Background.ToRgba(out r, out g, out b, out a);
                customRenderer.Background = new Android.Graphics.Color(r, g, b, a);
                var bitmap = customRenderer.Render(bitMatrix, format.ToPlatform(), code);
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

    #endregion Public Methods
}