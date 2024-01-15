#if IOS || MACCATALYST
using CoreGraphics;
using System.Runtime.InteropServices;
using UIKit;
using ZXing;

namespace Camera.MAUI.ZXing;

internal class RGBLuminanceSource : BaseLuminanceSource
{
    public enum BitmapFormat
    {
        Unknown,
        Gray8,
        Gray16,
        RGB24,
        RGB32,
        ARGB32,
        BGR24,
        BGR32,
        BGRA32,
        RGB565,
        RGBA32,
        UYVY,
        YUYV
    }

    protected RGBLuminanceSource(int width, int height)
       : base(width, height)
    {
    }
    public RGBLuminanceSource(UIImage d)
    : base((int)d.CGImage.Width, (int)d.CGImage.Height)
    {
        CalculateLuminance(d);
    }

    protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
    {
        return new RGBLuminanceSource(width, height) { luminances = newLuminances };
    }
    private static BitmapFormat DetermineBitmapFormat(byte[] rgbRawBytes, int width, int height)
    {
        var square = width * height;
        var byteperpixel = rgbRawBytes.Length / square;

        return byteperpixel switch
        {
            1 => BitmapFormat.Gray8,
            2 => BitmapFormat.RGB565,
            3 => BitmapFormat.RGB24,
            4 => BitmapFormat.RGB32,
            _ => throw new ArgumentException("The bitmap format could not be determined. Please specify the correct value."),
        };
    }
    protected void CalculateLuminance(byte[] rgbRawBytes, BitmapFormat bitmapFormat)
    {
        if (bitmapFormat == BitmapFormat.Unknown)
        {
            bitmapFormat = DetermineBitmapFormat(rgbRawBytes, Width, Height);
        }
        switch (bitmapFormat)
        {
            case BitmapFormat.Gray8:
                Buffer.BlockCopy(rgbRawBytes, 0, luminances, 0, rgbRawBytes.Length < luminances.Length ? rgbRawBytes.Length : luminances.Length);
                break;
            case BitmapFormat.Gray16:
                CalculateLuminanceGray16(rgbRawBytes);
                break;
            case BitmapFormat.RGB24:
                CalculateLuminanceRGB24(rgbRawBytes);
                break;
            case BitmapFormat.BGR24:
                CalculateLuminanceBGR24(rgbRawBytes);
                break;
            case BitmapFormat.RGB32:
                CalculateLuminanceRGB32(rgbRawBytes);
                break;
            case BitmapFormat.BGR32:
                CalculateLuminanceBGR32(rgbRawBytes);
                break;
            case BitmapFormat.RGBA32:
                CalculateLuminanceRGBA32(rgbRawBytes);
                break;
            case BitmapFormat.ARGB32:
                CalculateLuminanceARGB32(rgbRawBytes);
                break;
            case BitmapFormat.BGRA32:
                CalculateLuminanceBGRA32(rgbRawBytes);
                break;
            case BitmapFormat.RGB565:
                CalculateLuminanceRGB565(rgbRawBytes);
                break;
            case BitmapFormat.UYVY:
                CalculateLuminanceUYVY(rgbRawBytes);
                break;
            case BitmapFormat.YUYV:
                CalculateLuminanceYUYV(rgbRawBytes);
                break;
            default:
                throw new ArgumentException("The bitmap format isn't supported.", bitmapFormat.ToString());
        }
    }

    private void CalculateLuminanceRGB565(byte[] rgb565RawData)
    {
        var luminanceIndex = 0;
        for (var index = 0; index < rgb565RawData.Length && luminanceIndex < luminances.Length; index += 2, luminanceIndex++)
        {
            var byte1 = rgb565RawData[index];
            var byte2 = rgb565RawData[index + 1];

            var b5 = byte1 & 0x1F;
            var g5 = (((byte1 & 0xE0) >> 5) | ((byte2 & 0x03) << 3)) & 0x1F;
            var r5 = (byte2 >> 2) & 0x1F;
            var r8 = (r5 * 527 + 23) >> 6;
            var g8 = (g5 * 527 + 23) >> 6;
            var b8 = (b5 * 527 + 23) >> 6;

            luminances[luminanceIndex] = (byte)((RChannelWeight * r8 + GChannelWeight * g8 + BChannelWeight * b8) >> ChannelWeight);
        }
    }

    private void CalculateLuminanceRGB24(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            int r = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int b = rgbRawBytes[rgbIndex++];
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
        }
    }

    private void CalculateLuminanceBGR24(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            int b = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int r = rgbRawBytes[rgbIndex++];
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
        }
    }

    private void CalculateLuminanceRGB32(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            int r = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int b = rgbRawBytes[rgbIndex++];
            rgbIndex++;
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
        }
    }

    private void CalculateLuminanceBGR32(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            int b = rgbRawBytes[rgbIndex++];
            int g = rgbRawBytes[rgbIndex++];
            int r = rgbRawBytes[rgbIndex++];
            rgbIndex++;
            luminances[luminanceIndex] = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
        }
    }

    private void CalculateLuminanceBGRA32(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            var b = rgbRawBytes[rgbIndex++];
            var g = rgbRawBytes[rgbIndex++];
            var r = rgbRawBytes[rgbIndex++];
            var alpha = rgbRawBytes[rgbIndex++];
            var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
            luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
        }
    }

    private void CalculateLuminanceRGBA32(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            var r = rgbRawBytes[rgbIndex++];
            var g = rgbRawBytes[rgbIndex++];
            var b = rgbRawBytes[rgbIndex++];
            var alpha = rgbRawBytes[rgbIndex++];
            var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
            luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
        }
    }

    private void CalculateLuminanceARGB32(byte[] rgbRawBytes)
    {
        for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < rgbRawBytes.Length && luminanceIndex < luminances.Length; luminanceIndex++)
        {
            // Calculate luminance cheaply, favoring green.
            var alpha = rgbRawBytes[rgbIndex++];
            var r = rgbRawBytes[rgbIndex++];
            var g = rgbRawBytes[rgbIndex++];
            var b = rgbRawBytes[rgbIndex++];
            var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
            luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
        }
    }

    private void CalculateLuminanceUYVY(byte[] uyvyRawBytes)
    {
        // start by 1, jump over first U byte
        for (int uyvyIndex = 1, luminanceIndex = 0; uyvyIndex < uyvyRawBytes.Length - 3 && luminanceIndex < luminances.Length;)
        {
            byte y1 = uyvyRawBytes[uyvyIndex];
            uyvyIndex += 2; // jump from 1 to 3 (from Y1 over to Y2)
            byte y2 = uyvyRawBytes[uyvyIndex];
            uyvyIndex += 2; // jump from 3 to 5

            luminances[luminanceIndex++] = y1;
            luminances[luminanceIndex++] = y2;
        }
    }

    private void CalculateLuminanceYUYV(byte[] yuyvRawBytes)
    {
        // start by 0 not by 1 like UYUV
        for (int yuyvIndex = 0, luminanceIndex = 0; yuyvIndex < yuyvRawBytes.Length - 3 && luminanceIndex < luminances.Length;)
        {
            byte y1 = yuyvRawBytes[yuyvIndex];
            yuyvIndex += 2; // jump from 0 to 2 (from Y1 over over to Y2)
            byte y2 = yuyvRawBytes[yuyvIndex];
            yuyvIndex += 2; // jump from 2 to 4

            luminances[luminanceIndex++] = y1;
            luminances[luminanceIndex++] = y2;
        }
    }

    private void CalculateLuminanceGray16(byte[] gray16RawBytes)
    {
        for (int grayIndex = 0, luminanceIndex = 0; grayIndex < gray16RawBytes.Length && luminanceIndex < luminances.Length; grayIndex += 2, luminanceIndex++)
        {
            byte gray8 = gray16RawBytes[grayIndex];

            luminances[luminanceIndex] = gray8;
        }
    }

    private void CalculateLuminance(UIImage d)
    {
        var imageRef = d.CGImage;
        var width = (int)imageRef.Width;
        var height = (int)imageRef.Height;
        var colorSpace = CGColorSpace.CreateDeviceRGB();

        var rawData = Marshal.AllocHGlobal(height * width * 4);

        try
        {
            var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
            var context = new CGBitmapContext(rawData, width, height, 8, 4 * width,
            colorSpace, (CGImageAlphaInfo)flags);

            context.DrawImage(new CGRect(0.0f, 0.0f, (float)width, (float)height), imageRef);
            var pixelData = new byte[height * width * 4];
            Marshal.Copy(rawData, pixelData, 0, pixelData.Length);

            CalculateLuminance(pixelData, BitmapFormat.BGRA32);
        }
        finally
        {
            Marshal.FreeHGlobal(rawData);
        }
    }
}
#endif