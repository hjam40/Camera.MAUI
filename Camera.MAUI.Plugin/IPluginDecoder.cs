using System.Windows.Input;

#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else

using DecodeDataType = System.Object;

#endif

namespace Camera.MAUI.Plugin
{
    public interface IPluginDecoder
    {
        #region Public Events

        /// <summary>
        /// Event launched every time a successful decode occurs in the image if "Camera.MAUI.CameraView.BarCodeDetectionEnabled" is set to true.
        /// </summary>
        event PluginDecoderResultHandler Decoded;

        #endregion Public Events

        #region Public Properties

        ICommand OnDecodedCommand { get; set; }

        bool VibrateOnDetected { get; set; }

        #endregion Public Properties

        #region Public Methods

        void ClearResults();

        void Decode(DecodeDataType data);

        #endregion Public Methods
    }
}