#if IOS || MACCATALYST
using Foundation;
using global::MLKit.BarcodeScanning;
using global::MLKit.Core;
using DecodeDataType = UIKit.UIImage;
#elif ANDROID

using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using DecodeDataType = Android.Graphics.Bitmap;
using BarcodeScanner = Xamarin.Google.MLKit.Vision.BarCode.IBarcodeScanner;
using static Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode;

#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
using BarcodeScanner = System.Object;
#endif

namespace Camera.MAUI.Plugin.MLKit
{
    public class MLKitBarcodeDecoder : BindableObject, IPluginDecoder
    {
        #region Public Fields

        public static readonly BindableProperty OptionsProperty = BindableProperty.Create(nameof(Options), typeof(MLKitBarcodeDecoderOptions), typeof(MLKitBarcodeDecoder), new MLKitBarcodeDecoderOptions(), propertyChanged: OptionsChanged);

        #endregion Public Fields

        #region Private Fields

        private BarcodeScanner barcodeScanner;

        #endregion Private Fields

        #region Public Constructors

        public MLKitBarcodeDecoder()
        {
            Init(this, Options);
        }

        #endregion Public Constructors

        #region Public Events

        public event IPluginDecoder.DecoderResultHandler Decoded;

        #endregion Public Events

        #region Public Properties

        public IPluginDecoderOptions Options
        {
            get { return (IPluginDecoderOptions)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        #endregion Public Properties

        #region Public Methods

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
            if (results?.Count > 0)
            {
                Decoded?.Invoke(this, new PluginDecodedEventArgs { Results = results.ToArray() });
            }
#elif IOS
            var visionImage = new MLImage(data) { Orientation = UIKit.UIImageOrientation.Up };
            barcodeScanner.ProcessImage(visionImage, (barcodes, error) =>
            {
                var results = new List<BarcodeResult>();
                foreach (var barcode in barcodes)
                    results.Add(Methods.ProcessBarcodeResult(barcode));

                if (results.Count > 0)
                {
                    Decoded?.Invoke(this, new PluginDecodedEventArgs { Results = results.ToArray() });
                }
            });
#endif
        }

        #endregion Public Methods

        #region Private Methods

        private static void Init(MLKitBarcodeDecoder decoder, IPluginDecoderOptions options)
        {
            if (options is BarcodeDecoderOptions barcodeOptions)
            {
#if ANDROID || IOS
                var platformFormats = barcodeOptions.PossibleFormats?.Count > 0
                    ? barcodeOptions.PossibleFormats.Select(x => x.ToPlatform()).Aggregate((r, x) => r |= x)
                    : default;
#if ANDROID
                decoder.barcodeScanner = BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                    .SetBarcodeFormats(platformFormats == default ? FormatAllFormats : platformFormats)
                    .Build()
                );
#elif IOS
                var scannerOptions = new BarcodeScannerOptions(platformFormats == default ? global::MLKit.BarcodeScanning.BarcodeFormat.Unknown : platformFormats);
                decoder.barcodeScanner = BarcodeScanner.BarcodeScannerWithOptions(scannerOptions);
#endif
#endif
            }
            if (options is MLKitBarcodeDecoderOptions mlkitOptions)
            {
            }
        }

        private static void OptionsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && oldValue != newValue && bindable is MLKitBarcodeDecoder decoder)
            {
                Init(decoder, (IPluginDecoderOptions)newValue);
            }
        }

        #endregion Private Methods
    }
}