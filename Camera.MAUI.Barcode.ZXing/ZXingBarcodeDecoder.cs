using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Camera.MAUI.Barcode;
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

namespace Camera.MAUI.Barcode.ZXing
{
    public class ZXingBarcodeDecoder : BindableObject, IBarcodeDecoder
    {
        #region Public Fields

        public static readonly BindableProperty BarCodeOptionsProperty = BindableProperty.Create(nameof(BarCodeOptions), typeof(BarcodeDecodeOptions), typeof(ZXingBarcodeDecoder), new BarcodeDecodeOptions(), propertyChanged: BarCodeOptionsChanged);
        public static readonly BindableProperty BarCodeResultsProperty = BindableProperty.Create(nameof(BarCodeResults), typeof(BarcodeResult[]), typeof(ZXingBarcodeDecoder), null, BindingMode.OneWayToSource);

        #endregion Public Fields

        #region Private Fields

        private readonly BarcodeReaderGeneric BarcodeReader;

        #endregion Private Fields

        #region Public Constructors

        public ZXingBarcodeDecoder()
        {
            BarcodeReader = new BarcodeReaderGeneric();
            BarCodeOptions = new()
            {
                PossibleFormats = { BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX },
                AutoRotate = true,
            };
        }

        #endregion Public Constructors

        #region Public Events

        /// <summary>
        /// Event launched every time a code is detected in the image if "BarCodeDetectionEnabled" is set to true.
        /// </summary>
        public event IBarcodeDecoder.BarcodeResultHandler BarcodeDetected;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Options for the barcode detection. This is a bindable property.
        /// </summary>
        public BarcodeDecodeOptions BarCodeOptions
        {
            get { return (BarcodeDecodeOptions)GetValue(BarCodeOptionsProperty); }
            set { SetValue(BarCodeOptionsProperty, value); }
        }

        /// <summary>
        /// It refresh each time a barcode is detected if BarCodeDetectionEnabled porperty is true
        /// </summary>
        public BarcodeResult[] BarCodeResults
        {
            get { return (BarcodeResult[])GetValue(BarCodeResultsProperty); }
            set { SetValue(BarCodeResultsProperty, value); }
        }

        /// <summary>
        /// If true BarcodeDetected event will invoke only if a Results is diferent from preview Results
        /// </summary>
        public bool ControlBarcodeResultDuplicate { get; set; } = false;

        #endregion Public Properties

        #region Public Methods

        public void ClearResults()
        {
            BarCodeResults = null;
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
                if (BarCodeOptions.ReadMultipleCodes)
                    results = BarcodeReader.DecodeMultiple(lumSource);
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
                        if (BarCodeResults != null)
                        {
                            foreach (var result in nativeResults)
                            {
                                refresh = BarCodeResults.FirstOrDefault(b => b.Text == result.Text && b.BarcodeFormat == result.BarcodeFormat) == null;
                                if (refresh) break;
                            }
                        }
                    }
                    if (refresh)
                    {
                        BarCodeResults = nativeResults;
                        BarcodeDetected?.Invoke(this, new BarcodeEventArgs { Result = results.Select(x => x.ToNative()).ToArray() });
                    }
                }
            }
            catch { }
        }

        #endregion Public Methods

        #region Private Methods

        private static void BarCodeOptionsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && oldValue != newValue && bindable is ZXingBarcodeDecoder xingDecoder && newValue is BarcodeDecodeOptions options)
            {
                xingDecoder.BarcodeReader.AutoRotate = options.AutoRotate;
                if (options.CharacterSet != string.Empty)
                    xingDecoder.BarcodeReader.Options.CharacterSet = options.CharacterSet;
                xingDecoder.BarcodeReader.Options.PossibleFormats = options.PossibleFormats.Select(x => x.ToPlatform()).ToList();
                xingDecoder.BarcodeReader.Options.TryHarder = options.TryHarder;
                xingDecoder.BarcodeReader.Options.TryInverted = options.TryInverted;
                xingDecoder.BarcodeReader.Options.PureBarcode = options.PureBarcode;
            }
        }

        #endregion Private Methods
    }
}