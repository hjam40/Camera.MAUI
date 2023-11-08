using CoreGraphics;
using Foundation;
using global::MLKit.BarcodeScanning;
using global::MLKit.Core;
using UIKit;

namespace Camera.MAUI.Barcode.MLKit
{
    internal static class Methods
    {
        internal static global::MLKit.BarcodeScanning.BarcodeFormat ToPlatform(this BarcodeFormat format)
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

        internal static BarcodeFormat BarcodeFormatToNative(this global::MLKit.BarcodeScanning.BarcodeFormat format)
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

        internal static BarcodeResult ProcessBarcodeResult(global::MLKit.BarcodeScanning.Barcode barcode)
        {
            var cornerPoints = new List<Point>();

            foreach (var cornerPoint in barcode.CornerPoints)
                cornerPoints.Add(new Point(cornerPoint.CGPointValue.X, cornerPoint.CGPointValue.Y));

            return new BarcodeResult(barcode.DisplayValue, barcode.RawData.ToArray(), cornerPoints.ToArray(), barcode.Format.BarcodeFormatToNative(), null, 0, 0);
        }
    }
}