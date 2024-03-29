﻿#if IOS || MACCATALYST
using CoreGraphics;
using UIKit;
using ZXing.Common;
using ZXing.Rendering;

namespace Camera.MAUI.ZXing.Platforms.Apple;

public class BitmapRenderer : IBarcodeRenderer<UIImage>
{
    public CGColor Foreground { get; set; }
    public CGColor Background { get; set; }

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
    }
}
#endif
