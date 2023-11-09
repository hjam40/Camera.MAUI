namespace Camera.MAUI.Plugin.ZXing
{
    public class ZXingEncoderOptions : BarcodeEncoderOptions
    {
        public ZXingEncoderOptions(BarcodeFormat format = BarcodeFormat.QR_CODE, int width = 400, int height = 400, int margin = 5)
            : base(format, width, height, margin)
        { }
    }
}