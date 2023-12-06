using ZXing;

namespace Camera.MAUI.Plugin.ZXing
{
    internal static class Extensions
    {
        internal static global::ZXing.BarcodeFormat ToPlatform(this BarcodeFormat format)
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

        internal static BarcodeFormat ToNative(this global::ZXing.BarcodeFormat format)
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

        internal static ZXingResult ToNative(this Result result)
        {
            return new ZXingResult(
                result.Text,
                result.RawBytes,
                result.ResultPoints.Select(x => new Point(x.X, x.Y)).ToArray(),
                result.BarcodeFormat.ToNative(),
                result.ResultMetadata.ToDictionary(k => k.Key.ToString(), v => v.Value),
                result.NumBits,
                result.Timestamp);
        }
    }
}