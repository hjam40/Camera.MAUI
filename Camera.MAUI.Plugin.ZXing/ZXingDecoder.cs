using ZXing;

#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else

using DecodeDataType = System.Object;

#endif

namespace Camera.MAUI.Plugin.ZXing
{
    public class ZXingDecoder : BindableObject, IPluginDecoder
    {
        #region Public Fields

        public static readonly BindableProperty OptionsProperty = BindableProperty.Create(nameof(Options), typeof(ZXingDecoderOptions), typeof(ZXingDecoder), new ZXingDecoderOptions(), propertyChanged: OptionsChanged);
        public static readonly BindableProperty ResultsProperty = BindableProperty.Create(nameof(Results), typeof(ZXingResult[]), typeof(ZXingDecoder), null, BindingMode.OneWayToSource);

        #endregion Public Fields

        #region Private Fields

        private readonly BarcodeReaderGeneric BarcodeReader;

        #endregion Private Fields

        #region Public Constructors

        public ZXingDecoder()
        {
            BarcodeReader = new BarcodeReaderGeneric();
            /*Options = new ZXingDecoderOptions()
            {
                PossibleFormats = { BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX },
                AutoRotate = true,
            };*/
        }

        #endregion Public Constructors

        #region Public Events

        /// <summary>
        /// Event launched every time a code is detected in the image if "BarCodeDetectionEnabled" is set to true.
        /// </summary>
        public event IPluginDecoder.DecoderResultHandler Decoded;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// If true BarcodeDetected event will invoke only if a Results is diferent from preview Results
        /// </summary>
        public bool ControlBarcodeResultDuplicate { get; set; } = false;

        /// <summary>
        /// Options for the barcode detection. This is a bindable property.
        /// </summary>
        public IPluginDecoderOptions Options
        {
            get { return (IPluginDecoderOptions)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        /// <summary>
        /// It refresh each time a barcode is detected if BarCodeDetectionEnabled property is true
        /// </summary>
        public ZXingResult[] Results
        {
            get { return (ZXingResult[])GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void ClearResults()
        {
            Results = null;
        }

        public void Decode(DecodeDataType data)
        {
            System.Diagnostics.Debug.WriteLine("Calculate Luminance " + DateTime.Now.ToString("mm:ss:fff"));

            LuminanceSource lumSource = default;
#if ANDROID
            lumSource = new Platforms.Android.BitmapLuminanceSource(data);
#elif IOS || MACCATALYST
            lumSource = new Platforms.MaciOS.RGBLuminanceSource(data);
#elif WINDOWS
            lumSource = new Platforms.Windows.SoftwareBitmapLuminanceSource(data);
#endif
            System.Diagnostics.Debug.WriteLine("End Calculate Luminance " + DateTime.Now.ToString("mm:ss:fff"));

            try
            {
                Result[] results = null;
                if (Options is ZXingDecoderOptions zxingOptions && zxingOptions.ReadMultipleCodes)
                {
                    results = BarcodeReader.DecodeMultiple(lumSource);
                }
                else
                {
                    var result = BarcodeReader.Decode(lumSource);
                    if (result != null) results = new Result[] { result };
                }
                if (results?.Length > 0)
                {
                    var nativeResults = results.Select(x => x.ToNative()).ToArray();
                    bool refresh = true;
                    if (ControlBarcodeResultDuplicate)
                    {
                        if (Results != null)
                        {
                            foreach (var result in nativeResults)
                            {
                                refresh = Results.FirstOrDefault(b => b.Text == result.Text && b.BarcodeFormat == result.BarcodeFormat) == null;
                                if (refresh) break;
                            }
                        }
                    }
                    if (refresh)
                    {
                        Results = nativeResults;
                        Decoded?.Invoke(this, new PluginDecodedEventArgs { Results = Results });
                    }
                }
            }
            catch { }
        }

        #endregion Public Methods

        #region Private Methods

        private static void OptionsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && oldValue != newValue && bindable is ZXingDecoder xingDecoder)
            {
                if (newValue is BarcodeDecoderOptions barcodeOptions)
                {
                    xingDecoder.BarcodeReader.Options.PossibleFormats = barcodeOptions.PossibleFormats.Select(x => x.ToPlatform()).ToList();
                }
                if (newValue is ZXingDecoderOptions zxingOptions)
                {
                    xingDecoder.BarcodeReader.AutoRotate = zxingOptions.AutoRotate;
                    if (zxingOptions.CharacterSet != string.Empty)
                        xingDecoder.BarcodeReader.Options.CharacterSet = zxingOptions.CharacterSet;

                    xingDecoder.BarcodeReader.Options.TryHarder = zxingOptions.TryHarder;
                    xingDecoder.BarcodeReader.Options.TryInverted = zxingOptions.TryInverted;
                    xingDecoder.BarcodeReader.Options.PureBarcode = zxingOptions.PureBarcode;
                }
            }
        }

        #endregion Private Methods
    }
}