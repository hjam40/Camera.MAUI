using ZXing.Common;
using ZXing;

#if WINDOWS
using Windows.Graphics.Imaging;
using CustomRenderer = Camera.MAUI.Plugin.ZXing.Platforms.Windows.SoftwareBitmapRenderer;
#elif IOS || MACCATALYST
using CustomRenderer = Camera.MAUI.Plugin.ZXing.Platforms.MaciOS.BitmapRenderer;
#elif ANDROID
using CustomRenderer = Camera.MAUI.Plugin.ZXing.Platforms.Android.BitmapRenderer;
#else

using CustomRenderer = System.Object;

#endif

namespace Camera.MAUI.Plugin.ZXing
{
    public class ZXingRenderer : IPluginRenderer
    {
        #region Private Fields

        private readonly CustomRenderer customRenderer = new();
        private readonly BarcodeWriterPixelData writer = new();

        #endregion Private Fields

        #region Public Properties

        public Color Background { get; set; } = Colors.White;
        public Color Foreground { get; set; } = Colors.Black;

        #endregion Public Properties

        #region Public Methods

        public ImageSource EncodeBarcode(string code, IPluginEncoderOptions options)
        {
            ImageSource imageSource = null;
            if (options is BarcodeEncoderOptions noptions)
            {
                writer.Options = new EncodingOptions { Width = noptions.Width, Height = noptions.Height, Margin = noptions.Margin };
                writer.Format = noptions.Format.ToPlatform();
            }
            else
            {
                writer.Options = new EncodingOptions { Width = 400, Height = 400, Margin = 5 };
                writer.Format = global::ZXing.BarcodeFormat.QR_CODE;
            }

            try
            {
                var bitMatrix = writer.Encode(code);
                if (bitMatrix != null)
                {
                    var stream = new MemoryStream();
#if WINDOWS
                    Foreground.ToRgba(out byte r, out byte g, out byte b, out byte a);
                    customRenderer.Foreground = Windows.UI.Color.FromArgb(a, r, g, b);
                    Background.ToRgba(out r, out g, out b, out a);
                    customRenderer.Background = Windows.UI.Color.FromArgb(a, r, g, b);
                    var bitmap = customRenderer.Render(bitMatrix, writer.Format, code);
                    BitmapEncoder encoder = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream()).GetAwaiter().GetResult();
                    encoder.SetSoftwareBitmap(bitmap);
                    encoder.FlushAsync().GetAwaiter().GetResult();
                    stream.Position = 0;
                    imageSource = ImageSource.FromStream(() => stream);
#elif IOS || MACCATALYST
                    customRenderer.Foreground = new CoreGraphics.CGColor(Foreground.Red, Foreground.Green, Foreground.Blue, Foreground.Alpha);
                    customRenderer.Background = new CoreGraphics.CGColor(Background.Red, Background.Green, Background.Blue, Background.Alpha);
                    var bitmap = customRenderer.Render(bitMatrix, writer.Format, code);
                    bitmap.AsPNG().AsStream().CopyTo(stream);
                    stream.Position = 0;
                    imageSource = ImageSource.FromStream(() => stream);
#elif ANDROID
                    Foreground.ToRgba(out byte r, out byte g, out byte b, out byte a);
                    customRenderer.Foreground = new Android.Graphics.Color(r, g, b, a);
                    Background.ToRgba(out r, out g, out b, out a);
                    customRenderer.Background = new Android.Graphics.Color(r, g, b, a);
                    var bitmap = customRenderer.Render(bitMatrix, writer.Format, code);
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
}