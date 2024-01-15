#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else
using DecodeDataType = System.Object;
#endif
using ZXing;

namespace Camera.MAUI.ZXing;

// All the code in this file is included in all platforms.
public class ZXingBarcodeDecoder : IBarcodeDecoder
{
    private readonly BarcodeReaderGeneric BarcodeReader = new();
    private bool ReadMultipleCodes = false;

    public void SetDecodeOptions(BarcodeDecodeOptions options)
    {
        BarcodeReader.AutoRotate = options.AutoRotate;
        if (options.CharacterSet != string.Empty) BarcodeReader.Options.CharacterSet = options.CharacterSet;
        BarcodeReader.Options.PossibleFormats = ToPlatformList(options.PossibleFormats);
        BarcodeReader.Options.TryHarder = options.TryHarder;
        BarcodeReader.Options.TryInverted = options.TryInverted;
        BarcodeReader.Options.PureBarcode = options.PureBarcode;
        ReadMultipleCodes = options.ReadMultipleCodes;
    }

    public BarcodeResult[] Decode(DecodeDataType data)
    {
        LuminanceSource lumSource = default;
#if ANDROID
        lumSource = new BitmapLuminanceSource(data);
#elif IOS || MACCATALYST
        lumSource = new RGBLuminanceSource(data);
#elif WINDOWS
        lumSource = new SoftwareBitmapLuminanceSource(data);
#endif
        List<BarcodeResult> returnResults = null;
        try
        {
            Result[] results = null;
            if (ReadMultipleCodes)
                results = BarcodeReader.DecodeMultiple(lumSource);
            else
            {
                var result = BarcodeReader.Decode(lumSource);
                if (result != null) results = new Result[] { result };
            }
            if (results != null)
            {
                returnResults = new();
                foreach (var r in results)
                {
                    returnResults.Add(new BarcodeResult(r.Text, r.RawBytes, r.ResultPoints.Select(x => new Point(x.X, x.Y)).ToArray(), ToNative(r.BarcodeFormat)));
                }
            }
        }
        catch { }

        return returnResults?.ToArray();
    }

    internal static List<global::ZXing.BarcodeFormat> ToPlatformList(IList<BarcodeFormat> formats)
    {
        List<global::ZXing.BarcodeFormat> result = new();

        foreach (var f in formats) result.Add(ToPlatform(f));

        return result;
    }
    internal static global::ZXing.BarcodeFormat ToPlatform(BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.AZTEC => global::ZXing.BarcodeFormat.AZTEC,
            BarcodeFormat.CODABAR => global::ZXing.BarcodeFormat.CODABAR,
            BarcodeFormat.CODE_39 => global::ZXing.BarcodeFormat.CODE_39,
            BarcodeFormat.CODE_93 => global::ZXing.BarcodeFormat.CODE_93,
            BarcodeFormat.CODE_128 => global::ZXing.BarcodeFormat.CODE_128,
            BarcodeFormat.DATA_MATRIX => global::ZXing.BarcodeFormat.DATA_MATRIX,
            BarcodeFormat.EAN_8 => global::ZXing.BarcodeFormat.EAN_8,
            BarcodeFormat.EAN_13 => global::ZXing.BarcodeFormat.EAN_13,
            BarcodeFormat.ITF => global::ZXing.BarcodeFormat.ITF,
            BarcodeFormat.MAXICODE => global::ZXing.BarcodeFormat.MAXICODE,
            BarcodeFormat.PDF_417 => global::ZXing.BarcodeFormat.PDF_417,
            BarcodeFormat.QR_CODE => global::ZXing.BarcodeFormat.QR_CODE,
            BarcodeFormat.RSS_14 => global::ZXing.BarcodeFormat.RSS_14,
            BarcodeFormat.RSS_EXPANDED => global::ZXing.BarcodeFormat.RSS_EXPANDED,
            BarcodeFormat.UPC_A => global::ZXing.BarcodeFormat.UPC_A,
            BarcodeFormat.UPC_E => global::ZXing.BarcodeFormat.UPC_E,
            BarcodeFormat.UPC_EAN_EXTENSION => global::ZXing.BarcodeFormat.UPC_EAN_EXTENSION,
            BarcodeFormat.MSI => global::ZXing.BarcodeFormat.MSI,
            BarcodeFormat.PLESSEY => global::ZXing.BarcodeFormat.PLESSEY,
            BarcodeFormat.IMB => global::ZXing.BarcodeFormat.IMB,
            BarcodeFormat.PHARMA_CODE => global::ZXing.BarcodeFormat.PHARMA_CODE,
            BarcodeFormat.All_1D => global::ZXing.BarcodeFormat.All_1D,
            _ => throw new NotSupportedException(),
        };
    }

    internal static BarcodeFormat ToNative(global::ZXing.BarcodeFormat format)
    {
        return format switch
        {
            global::ZXing.BarcodeFormat.AZTEC => BarcodeFormat.AZTEC,
            global::ZXing.BarcodeFormat.CODABAR => BarcodeFormat.CODABAR,
            global::ZXing.BarcodeFormat.CODE_39 => BarcodeFormat.CODE_39,
            global::ZXing.BarcodeFormat.CODE_93 => BarcodeFormat.CODE_93,
            global::ZXing.BarcodeFormat.CODE_128 => BarcodeFormat.CODE_128,
            global::ZXing.BarcodeFormat.DATA_MATRIX => BarcodeFormat.DATA_MATRIX,
            global::ZXing.BarcodeFormat.EAN_8 => BarcodeFormat.EAN_8,
            global::ZXing.BarcodeFormat.EAN_13 => BarcodeFormat.EAN_13,
            global::ZXing.BarcodeFormat.ITF => BarcodeFormat.ITF,
            global::ZXing.BarcodeFormat.MAXICODE => BarcodeFormat.MAXICODE,
            global::ZXing.BarcodeFormat.PDF_417 => BarcodeFormat.PDF_417,
            global::ZXing.BarcodeFormat.QR_CODE => BarcodeFormat.QR_CODE,
            global::ZXing.BarcodeFormat.RSS_14 => BarcodeFormat.RSS_14,
            global::ZXing.BarcodeFormat.RSS_EXPANDED => BarcodeFormat.RSS_EXPANDED,
            global::ZXing.BarcodeFormat.UPC_A => BarcodeFormat.UPC_A,
            global::ZXing.BarcodeFormat.UPC_E => BarcodeFormat.UPC_E,
            global::ZXing.BarcodeFormat.UPC_EAN_EXTENSION => BarcodeFormat.UPC_EAN_EXTENSION,
            global::ZXing.BarcodeFormat.MSI => BarcodeFormat.MSI,
            global::ZXing.BarcodeFormat.PLESSEY => BarcodeFormat.PLESSEY,
            global::ZXing.BarcodeFormat.IMB => BarcodeFormat.IMB,
            global::ZXing.BarcodeFormat.PHARMA_CODE => BarcodeFormat.PHARMA_CODE,
            global::ZXing.BarcodeFormat.All_1D => BarcodeFormat.All_1D,
            _ => throw new NotSupportedException(),
        };
    }
}
