using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using Java.Nio;
using Java.Util;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using static Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode;

namespace Camera.MAUI.Barcode.MLKit
{
    internal static class Methods
    {
        internal static int ToPlatform(this BarcodeFormat format)
        {
            return format switch
            {
                BarcodeFormat.AZTEC => FormatAztec,
                BarcodeFormat.CODABAR => FormatCodabar,
                BarcodeFormat.CODE_128 => FormatCode128,
                BarcodeFormat.CODE_39 => FormatCode39,
                BarcodeFormat.CODE_93 => FormatCode93,
                BarcodeFormat.DATA_MATRIX => FormatDataMatrix,
                BarcodeFormat.EAN_13 => FormatEan13,
                BarcodeFormat.EAN_8 => FormatEan8,
                BarcodeFormat.ITF => FormatItf,
                BarcodeFormat.PDF_417 => FormatPdf417,
                BarcodeFormat.QR_CODE => FormatQrCode,
                BarcodeFormat.UPC_A => FormatUpcA,
                BarcodeFormat.UPC_E => FormatUpcE,
                _ => FormatUnknown,
            };
        }

        internal static BarcodeFormat BarcodeFormatToNative(this int format)
        {
            return format switch
            {
                FormatAztec => BarcodeFormat.AZTEC,
                FormatCodabar => BarcodeFormat.CODABAR,
                FormatCode128 => BarcodeFormat.CODE_128,
                FormatCode39 => BarcodeFormat.CODE_39,
                FormatCode93 => BarcodeFormat.CODE_93,
                FormatDataMatrix => BarcodeFormat.DATA_MATRIX,
                FormatEan13 => BarcodeFormat.EAN_13,
                FormatEan8 => BarcodeFormat.EAN_8,
                FormatItf => BarcodeFormat.ITF,
                FormatPdf417 => BarcodeFormat.PDF_417,
                FormatQrCode => BarcodeFormat.QR_CODE,
                FormatUpcA => BarcodeFormat.UPC_A,
                FormatUpcE => BarcodeFormat.UPC_E,
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
                var mapped = barcode.JavaCast<Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode>();
                var cornerPoints = new List<Microsoft.Maui.Graphics.Point>();

                foreach (var cornerPoint in mapped.GetCornerPoints())
                    cornerPoints.Add(new Microsoft.Maui.Graphics.Point(cornerPoint.X, cornerPoint.Y));

                resultList.Add(new BarcodeResult(mapped.DisplayValue, mapped.GetRawBytes(), cornerPoints.ToArray(), mapped.Format.BarcodeFormatToNative(), null, 0, 0));
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