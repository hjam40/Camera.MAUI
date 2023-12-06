using Android.Runtime;
using Java.Util;
using Xamarin.Google.MLKit.Vision.Barcode.Common;

namespace Camera.MAUI.Plugin.MLKit
{
    internal static class Methods
    {
        internal static int ToPlatform(this BarcodeFormat format)
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

        internal static BarcodeFormat BarcodeFormatToNative(this int format)
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

        internal static List<BarcodeResult> ProcessBarcodeResult(Java.Lang.Object result)
        {
            if (result == null)
                return null;
            var javaList = result.JavaCast<ArrayList>();
            if (javaList.IsEmpty)
                return null;
            var resultList = new List<BarcodeResult>();

            foreach (var barcode in javaList.ToArray())
            {
                var mapped = barcode.JavaCast<Barcode>();
                var cornerPoints = new List<Point>();

                foreach (var cornerPoint in mapped.GetCornerPoints())
                    cornerPoints.Add(new Point(cornerPoint.X, cornerPoint.Y));

                resultList.Add(new BarcodeResult(mapped.DisplayValue, mapped.GetRawBytes(), cornerPoints.ToArray(), mapped.Format.BarcodeFormatToNative()));
            }

            return resultList;
        }

        internal static Task<Java.Lang.Object> ToAwaitableTask(this global::Android.Gms.Tasks.Task task)
        {
            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var taskCompleteListener = new TaskCompleteListener(taskCompletionSource);
            task.AddOnCompleteListener(taskCompleteListener);

            return taskCompletionSource.Task;
        }
    }
}