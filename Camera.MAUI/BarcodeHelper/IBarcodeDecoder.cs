#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else
using DecodeDataType = System.Object;
#endif

namespace Camera.MAUI;

public interface IBarcodeDecoder
{
    void SetDecodeOptions(BarcodeDecodeOptions options);
    BarcodeResult[] Decode(DecodeDataType data);
}
