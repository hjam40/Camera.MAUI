using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using ZXing.Common;
using ZXing.Rendering;
using Color = Windows.UI.Color;

namespace Camera.MAUI.Plugin.ZXing.Platforms.Windows
{
    public class SoftwareBitmapRenderer : IBarcodeRenderer<SoftwareBitmap>
    {
        public Color Foreground { get; set; }
        public Color Background { get; set; }

        public SoftwareBitmapRenderer()
        {
            Foreground = Color.FromArgb(1, 0, 0, 0);
            Background = Color.FromArgb(1, 255, 255, 255);
        }

        public SoftwareBitmap Render(BitMatrix matrix, global::ZXing.BarcodeFormat format, string content)
        {
            return Render(matrix, format, content, null);
        }

        public virtual SoftwareBitmap Render(BitMatrix matrix, global::ZXing.BarcodeFormat format, string content, EncodingOptions options)
        {
            int width = matrix.Width;
            int height = matrix.Height;
            bool outputContent = (options == null || !options.PureBarcode) &&
                                 !string.IsNullOrEmpty(content) && (format == global::ZXing.BarcodeFormat.CODE_39 ||
                                                                    format == global::ZXing.BarcodeFormat.CODE_128 ||
                                                                    format == global::ZXing.BarcodeFormat.EAN_13 ||
                                                                    format == global::ZXing.BarcodeFormat.EAN_8 ||
                                                                    format == global::ZXing.BarcodeFormat.CODABAR ||
                                                                    format == global::ZXing.BarcodeFormat.ITF ||
                                                                    format == global::ZXing.BarcodeFormat.UPC_A ||
                                                                    format == global::ZXing.BarcodeFormat.MSI ||
                                                                    format == global::ZXing.BarcodeFormat.PLESSEY);
            int emptyArea = outputContent ? 16 : 0;
            int pixelsize = 1;

            if (options != null && !options.NoPadding)
            {
                if (options.Width > width)
                {
                    width = options.Width;
                }
                if (options.Height > height)
                {
                    height = options.Height;
                }
                // calculating the scaling factor
                pixelsize = width / matrix.Width;
                if (pixelsize > height / matrix.Height)
                {
                    pixelsize = height / matrix.Height;
                }
            }

            var foreground = new[] { Foreground.B, Foreground.G, Foreground.R, Foreground.A };
            var background = new[] { Background.B, Background.G, Background.R, Background.A };

            var writableBitmap = new WriteableBitmap(width, height);

            using (var stream = writableBitmap.PixelBuffer.AsStream())
            {
                for (int y = 0; y < matrix.Height - emptyArea; y++)
                {
                    for (var pixelsizeHeight = 0; pixelsizeHeight < pixelsize; pixelsizeHeight++)
                    {
                        for (var x = 0; x < matrix.Width; x++)
                        {
                            var color = matrix[x, y] ? foreground : background;
                            for (var pixelsizeWidth = 0; pixelsizeWidth < pixelsize; pixelsizeWidth++)
                            {
                                stream.Write(color, 0, 4);
                            }
                        }
                        for (var x = pixelsize * matrix.Width; x < width; x++)
                        {
                            stream.Write(background, 0, 4);
                        }
                    }
                }
                for (int y = matrix.Height * pixelsize - emptyArea; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        stream.Write(background, 0, 4);
                    }
                }
            }
            writableBitmap.Invalidate();
            return SoftwareBitmap.CreateCopyFromBuffer(writableBitmap.PixelBuffer, BitmapPixelFormat.Bgra8, width, height);
        }
    }
}