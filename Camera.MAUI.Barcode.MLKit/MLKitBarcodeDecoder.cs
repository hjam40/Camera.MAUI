using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if IOS || MACCATALYST
using Foundation;

using global::MLKit.BarcodeScanning;
using global::MLKit.Core;
using DecodeDataType = UIKit.UIImage;
#elif ANDROID

using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using DecodeDataType = Android.Graphics.Bitmap;
using static Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode;

#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else

using DecodeDataType = System.Object;

#endif

namespace Camera.MAUI.Barcode.MLKit
{
    public class MLKitBarcodeDecoder : BindableObject, IBarcodeDecoder
    {
#if ANDROID
        private IBarcodeScanner barcodeScanner;
#elif IOS
        private BarcodeScanner barcodeDetector;
#endif

        public event IBarcodeDecoder.BarcodeResultHandler BarcodeDetected;

        public static readonly BindableProperty BarCodeFormatsProperty = BindableProperty.Create(nameof(BarCodeFormats), typeof(IList<BarcodeFormat>), typeof(MLKitBarcodeDecoder), new List<BarcodeFormat> { BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX }, propertyChanged: BarCodeFormatsChanged);

        public IList<BarcodeFormat> BarCodeFormats
        {
            get { return (IList<BarcodeFormat>)GetValue(BarCodeFormatsProperty); }
            set { SetValue(BarCodeFormatsProperty, value); }
        }

        private static void BarCodeFormatsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && oldValue != newValue && bindable is MLKitBarcodeDecoder decoder && newValue is IList<BarcodeFormat> formats)
            {
#if ANDROID
                decoder.barcodeScanner = BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                    .SetBarcodeFormats(formats?.Count > 0 ? formats.Select(x => x.ToPlatform()).Aggregate((r, x) => r |= x) : FormatAllFormats)
                    .Build()
                );
#endif
            }
        }

        public MLKitBarcodeDecoder()
        {
#if ANDROID || IOS
            var platformFormats = BarCodeFormats?.Count > 0
                ? BarCodeFormats.Select(x => x.ToPlatform()).Aggregate((r, x) => r |= x)
                : default;

#if ANDROID
            barcodeScanner = BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(platformFormats == default ? FormatAllFormats : platformFormats)
                .Build()
            );
#elif IOS
            var options = new BarcodeScannerOptions(platformFormats == default ? global::MLKit.BarcodeScanning.BarcodeFormat.Unknown : platformFormats);
            barcodeDetector = BarcodeScanner.BarcodeScannerWithOptions(options);
#endif
#endif
        }

        public void ClearResults()
        {
        }

        public
#if ANDROID
            async
#endif
            void Decode(DecodeDataType data)
        {
#if ANDROID
            var image = InputImage.FromBitmap(data, 0);
            var result = await barcodeScanner.Process(image).ToAwaitableTask();
            var results = Methods.ProcessBarcodeResult(result);
            if (results.Count > 0)
            {
                BarcodeDetected?.Invoke(this, new BarcodeEventArgs { Result = results.ToArray() });
            }
#elif IOS
            var visionImage = new MLImage(data) { Orientation = UIKit.UIImageOrientation.Up };
            barcodeDetector.ProcessImage(visionImage, (barcodes, error) =>
            {
                var results = new List<BarcodeResult>();
                foreach (var barcode in barcodes)
                    results.Add(Methods.ProcessBarcodeResult(barcode));

                if (results.Count > 0)
                {
                    BarcodeDetected?.Invoke(this, new BarcodeEventArgs { Result = results.ToArray() });
                }
            });
#endif
        }
    }
}