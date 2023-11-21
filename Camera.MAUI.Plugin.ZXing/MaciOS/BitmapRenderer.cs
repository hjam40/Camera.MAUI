#if IOS || MACCATALYST
using CoreGraphics;
using UIKit;
using ZXing.Common;
using ZXing.Rendering;

namespace Camera.MAUI.Plugin.ZXing.Platforms.MaciOS;

public class BitmapRenderer : IBarcodeRenderer<UIImage>
{
    private CGColor foreground;
    private CGColor background;
    private UIColor foregroundUIColor;
    private UIColor backgroundUIColor;

    public CGColor Foreground
    {
        get => foreground;
        set { foreground = value; foregroundUIColor = new UIColor(value); }
    }

    public CGColor Background
    {
        get => background;
        set { background = value; backgroundUIColor = new UIColor(value); }
    }

    public BitmapRenderer()
    {
        Foreground = new CGColor(0f, 0f, 0f);
        Background = new CGColor(1.0f, 1.0f, 1.0f);
    }
    public UIImage Render(BitMatrix matrix, global::ZXing.BarcodeFormat format, string content)
    {
        return Render(matrix, format, content, new EncodingOptions());
    }

    public UIImage Render(BitMatrix matrix, global::ZXing.BarcodeFormat format, string content, EncodingOptions options)
    {
#if (IOS17_0_OR_GREATER || MACCATALYST17_0_OR_GREATER)
        var renderer = new UIGraphicsImageRenderer(new CGSize(matrix.Width, matrix.Height));
        var img = renderer.CreateImage((UIGraphicsImageRendererContext context) =>
        {
            for (int x = 0; x < matrix.Width; x++)
            {
                for (int y = 0; y < matrix.Height; y++)
                {
                    if (matrix[x, y])
                        foregroundUIColor.SetFill();
                    else
                        backgroundUIColor.SetFill();

                    context.FillRect(new CGRect(x, y, 1, 1));
                }
            }
        });
        return img;
#else
        UIGraphics.BeginImageContext(new CGSize(matrix.Width, matrix.Height));
        var context = UIGraphics.GetCurrentContext();

        for (int x = 0; x < matrix.Width; x++)
        {
            for (int y = 0; y < matrix.Height; y++)
            {
                context.SetFillColor(matrix[x, y] ? Foreground : Background);
                context.FillRect(new CGRect(x, y, 1, 1));
            }
        }

        var img = UIGraphics.GetImageFromCurrentImageContext();

        UIGraphics.EndImageContext();

        return img;
#endif
    }
}
#endif