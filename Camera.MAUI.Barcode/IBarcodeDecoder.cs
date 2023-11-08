#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else

using DecodeDataType = System.Object;

#endif

namespace Camera.MAUI.Barcode
{
    public interface IBarcodeDecoder
    {
        #region Public Delegates

        public delegate void BarcodeResultHandler(object sender, BarcodeEventArgs args);

        #endregion Public Delegates

        #region Public Events

        event BarcodeResultHandler BarcodeDetected;

        #endregion Public Events

        #region Public Methods

        void ClearResults();

        void Decode(DecodeDataType data);

        #endregion Public Methods
    }
}