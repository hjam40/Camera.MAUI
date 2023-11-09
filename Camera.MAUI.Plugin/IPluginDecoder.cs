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
        #region Public Delegates

        public delegate void DecoderResultHandler(object sender, PluginDecodedEventArgs args);

        #endregion Public Delegates

        #region Public Events

        event DecoderResultHandler Decoded;

        #endregion Public Events

        #region Public Properties

        IPluginDecoderOptions Options { get; set; }

        #endregion Public Properties

        #region Public Methods

        void ClearResults();

        void Decode(DecodeDataType data);

        #endregion Public Methods
    }
}