#if IOS || MACCATALYST
using Foundation;
using global::MLKit.BarcodeScanning;
using global::MLKit.Core;
using DecodeDataType = UIKit.UIImage;
#elif ANDROID

using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using static Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode;
using BarcodeScanner = Xamarin.Google.MLKit.Vision.BarCode.IBarcodeScanner;
using DecodeDataType = Android.Graphics.Bitmap;

#elif WINDOWS
using BarcodeScanner = System.Object;
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#endif

namespace Camera.MAUI.Plugin.MLKit
{
    public class MLKitBarcodeDecoder : PluginDecoder<MLKitBarcodeDecoderOptions, BarcodeResult>
    {
        #region Private Fields

        private BarcodeScanner barcodeScanner;

        #endregion Private Fields

        #region Public Methods

        public override void ClearResults()
        {
        }

        public
#if ANDROID
            async
#endif
            override void Decode(DecodeDataType data)
        {
            try
            {
#if ANDROID
                var image = InputImage.FromBitmap(data, 0);
                var result = await barcodeScanner.Process(image).ToAwaitableTask();
                var results = Methods.ProcessBarcodeResult(result);
                if (results?.Count > 0)
                {
                    OnDecoded(new PluginDecodedEventArgs { Results = results.ToArray() });
                }
                image.Dispose();
#elif IOS
                var image = new MLImage(data) { Orientation = UIKit.UIImageOrientation.Up };
                barcodeScanner.ProcessImage(image, (barcodes, error) =>
                {
                    var results = new List<BarcodeResult>();
                    foreach (var barcode in barcodes)
                        results.Add(Methods.ProcessBarcodeResult(barcode));

                    if (results.Count > 0)
                    {
                        OnDecoded(new PluginDecodedEventArgs { Results = results.ToArray() });
                    }

                    image.Dispose();
                });
#else
                throw new NotImplementedException();
#endif
            }
            catch { }
            finally
            {
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void OnOptionsChanged(object oldValue, object newValue)
        {
            if (newValue is BarcodeDecoderOptions barcodeOptions)
            {
#if ANDROID || IOS
                var platformFormats = barcodeOptions.PossibleFormats?.Count > 0
                    ? barcodeOptions.PossibleFormats.Select(x => x.ToPlatform()).Aggregate((r, x) => r |= x)
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
            if (newValue is MLKitBarcodeDecoderOptions mlkitOptions)
            {
            }
        }

        #endregion Protected Methods
    }
}