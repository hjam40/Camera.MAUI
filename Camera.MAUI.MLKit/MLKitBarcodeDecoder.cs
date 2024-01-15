#if IOS
using MLKit.BarcodeScanning;
using MLKit.Core;
using DecodeDataType = UIKit.UIImage;
#elif MACCATALYST
using BarcodeScanner = System.Object;
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;
using static Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode;
using DecodeDataType = Android.Graphics.Bitmap;
using BarcodeScanner = Xamarin.Google.MLKit.Vision.BarCode.IBarcodeScanner;
using Android.Gms.Extensions;
using Android.Runtime;
using Java.Util;
using Xamarin.Google.MLKit.Vision.BarCode;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
using BarcodeScanner = System.Object;
#else
using BarcodeScanner = System.Object;
using DecodeDataType = System.Object;
#endif

namespace Camera.MAUI.MLKit;

public class MLKitBarcodeDecoder : IBarcodeDecoder
{
    private BarcodeScanner barcodeScanner;

    public void SetDecodeOptions(BarcodeDecodeOptions options)
    {
#if ANDROID || IOS
        var platformFormats = options.PossibleFormats?.Count > 0
            ? options.PossibleFormats.Select(x => ToPlatform(x)).Aggregate((r, x) => r |= x)
            : default;
#if ANDROID
                barcodeScanner = BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                    .SetBarcodeFormats(platformFormats == default ? FormatAllFormats : platformFormats)
                    .Build()
                );
#elif IOS
        var scannerOptions = new BarcodeScannerOptions(platformFormats == default ? global::MLKit.BarcodeScanning.BarcodeFormat.Unknown : platformFormats);
        barcodeScanner = BarcodeScanner.BarcodeScannerWithOptions(scannerOptions);
#endif
#endif
    }

    public BarcodeResult[] Decode(DecodeDataType data)
    {
        List<BarcodeResult> returnResults = null;

        if (barcodeScanner == null) SetDecodeOptions(new BarcodeDecodeOptions());
        try
        {
#if ANDROID
            var image = InputImage.FromBitmap(data, 0);
            var result = barcodeScanner.Process(image).GetAwaiter().GetResult();
            if (result != null)
            {
                var javaList = result.JavaCast<ArrayList>();
                if (!javaList.IsEmpty)
                {
                    foreach (var barcode in javaList.ToArray())
                    {
                        var mapped = barcode.JavaCast<Barcode>();
                        var cornerPoints = new List<Point>();

                        foreach (var cornerPoint in mapped.GetCornerPoints())
                            cornerPoints.Add(new Point(cornerPoint.X, cornerPoint.Y));

                        returnResults.Add(new BarcodeResult(mapped.DisplayValue, mapped.GetRawBytes(), cornerPoints.ToArray(), ToNative(mapped.Format)));
                    }
                }
            }
            image.Dispose();
#elif IOS
                var image = new MLImage(data) { Orientation = UIKit.UIImageOrientation.Up };
                barcodeScanner.ProcessImage(image, (barcodes, error) =>
                {
                    foreach (var barcode in barcodes)
                    {
                        var cornerPoints = new List<Point>();

                        foreach (var cornerPoint in barcode.CornerPoints)
                            cornerPoints.Add(new Point(cornerPoint.CGPointValue.X, cornerPoint.CGPointValue.Y));

                        returnResults.Add(new BarcodeResult(barcode.DisplayValue, barcode.RawData.ToArray(), cornerPoints.ToArray(), ToNative(barcode.Format)));
                    }

                    image.Dispose();
                });
#else
                throw new NotImplementedException();
#endif
        }
        catch { }

        return returnResults?.ToArray();
    }
#if IOS
    internal static List<global::MLKit.BarcodeScanning.BarcodeFormat> ToPlatformList(IList<BarcodeFormat> formats)
    {
        List<global::MLKit.BarcodeScanning.BarcodeFormat> result = new();

        foreach (var f in formats) result.Add(ToPlatform(f));

        return result;
    }
    internal static global::MLKit.BarcodeScanning.BarcodeFormat ToPlatform(BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.AZTEC => global::MLKit.BarcodeScanning.BarcodeFormat.Aztec,
            BarcodeFormat.CODABAR => global::MLKit.BarcodeScanning.BarcodeFormat.CodaBar,
            BarcodeFormat.CODE_128 => global::MLKit.BarcodeScanning.BarcodeFormat.Code128,
            BarcodeFormat.CODE_39 => global::MLKit.BarcodeScanning.BarcodeFormat.Code39,
            BarcodeFormat.CODE_93 => global::MLKit.BarcodeScanning.BarcodeFormat.Code93,
            BarcodeFormat.DATA_MATRIX => global::MLKit.BarcodeScanning.BarcodeFormat.DataMatrix,
            BarcodeFormat.EAN_13 => global::MLKit.BarcodeScanning.BarcodeFormat.Ean13,
            BarcodeFormat.EAN_8 => global::MLKit.BarcodeScanning.BarcodeFormat.Ean8,
            BarcodeFormat.ITF => global::MLKit.BarcodeScanning.BarcodeFormat.Itf,
            BarcodeFormat.PDF_417 => global::MLKit.BarcodeScanning.BarcodeFormat.Pdf417,
            BarcodeFormat.QR_CODE => global::MLKit.BarcodeScanning.BarcodeFormat.QrCode,
            BarcodeFormat.UPC_A => global::MLKit.BarcodeScanning.BarcodeFormat.Upca,
            BarcodeFormat.UPC_E => global::MLKit.BarcodeScanning.BarcodeFormat.Upce,
            _ => global::MLKit.BarcodeScanning.BarcodeFormat.Unknown,
        };
    }

    internal static BarcodeFormat ToNative(global::MLKit.BarcodeScanning.BarcodeFormat format)
    {
        return format switch
        {
            global::MLKit.BarcodeScanning.BarcodeFormat.Aztec => BarcodeFormat.AZTEC,
            global::MLKit.BarcodeScanning.BarcodeFormat.CodaBar => BarcodeFormat.CODABAR,
            global::MLKit.BarcodeScanning.BarcodeFormat.Code128 => BarcodeFormat.CODE_128,
            global::MLKit.BarcodeScanning.BarcodeFormat.Code39 => BarcodeFormat.CODE_39,
            global::MLKit.BarcodeScanning.BarcodeFormat.Code93 => BarcodeFormat.CODE_93,
            global::MLKit.BarcodeScanning.BarcodeFormat.DataMatrix => BarcodeFormat.DATA_MATRIX,
            global::MLKit.BarcodeScanning.BarcodeFormat.Ean13 => BarcodeFormat.EAN_13,
            global::MLKit.BarcodeScanning.BarcodeFormat.Ean8 => BarcodeFormat.EAN_8,
            global::MLKit.BarcodeScanning.BarcodeFormat.Itf => BarcodeFormat.ITF,
            global::MLKit.BarcodeScanning.BarcodeFormat.Pdf417 => BarcodeFormat.PDF_417,
            global::MLKit.BarcodeScanning.BarcodeFormat.QrCode => BarcodeFormat.QR_CODE,
            global::MLKit.BarcodeScanning.BarcodeFormat.Upca => BarcodeFormat.UPC_A,
            global::MLKit.BarcodeScanning.BarcodeFormat.Upce => BarcodeFormat.UPC_E,
            _ => throw new NotSupportedException()
        };
    }
#elif ANDROID
    internal static List<int> ToPlatformList(IList<BarcodeFormat> formats)
    {
        List<int> result = new();

        foreach (var f in formats) result.Add(ToPlatform(f));

        return result;
    }
    internal static int ToPlatform(BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.AZTEC => Barcode.FormatAztec,
            BarcodeFormat.CODABAR => Barcode.FormatCodabar,
            BarcodeFormat.CODE_128 => Barcode.FormatCode128,
            BarcodeFormat.CODE_39 => Barcode.FormatCode39,
            BarcodeFormat.CODE_93 => Barcode.FormatCode93,
            BarcodeFormat.DATA_MATRIX => Barcode.FormatDataMatrix,
            BarcodeFormat.EAN_13 => Barcode.FormatEan13,
            BarcodeFormat.EAN_8 => Barcode.FormatEan8,
            BarcodeFormat.ITF => Barcode.FormatItf,
            BarcodeFormat.PDF_417 => Barcode.FormatPdf417,
            BarcodeFormat.QR_CODE => Barcode.FormatQrCode,
            BarcodeFormat.UPC_A => Barcode.FormatUpcA,
            BarcodeFormat.UPC_E => Barcode.FormatUpcE,
            _ => Barcode.FormatUnknown,
        };
    }

    internal static BarcodeFormat ToNative(int format)
    {
        return format switch
        {
            Barcode.FormatAztec => BarcodeFormat.AZTEC,
            Barcode.FormatCodabar => BarcodeFormat.CODABAR,
            Barcode.FormatCode128 => BarcodeFormat.CODE_128,
            Barcode.FormatCode39 => BarcodeFormat.CODE_39,
            Barcode.FormatCode93 => BarcodeFormat.CODE_93,
            Barcode.FormatDataMatrix => BarcodeFormat.DATA_MATRIX,
            Barcode.FormatEan13 => BarcodeFormat.EAN_13,
            Barcode.FormatEan8 => BarcodeFormat.EAN_8,
            Barcode.FormatItf => BarcodeFormat.ITF,
            Barcode.FormatPdf417 => BarcodeFormat.PDF_417,
            Barcode.FormatQrCode => BarcodeFormat.QR_CODE,
            Barcode.FormatUpcA => BarcodeFormat.UPC_A,
            Barcode.FormatUpcE => BarcodeFormat.UPC_E,
            _ => throw new NotSupportedException()
        };
    }
#endif
}
