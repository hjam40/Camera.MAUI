namespace Camera.MAUI.Plugin
{
    public class BarcodeEncoderOptions : IPluginEncoderOptions
    {
        public BarcodeFormat Format { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Margin { get; set; }

        public BarcodeEncoderOptions(BarcodeFormat format = BarcodeFormat.QR_CODE, int width = 400, int height = 400, int margin = 5)
        {
            Format = format;
            Width = width;
            Height = height;
            Margin = margin;
        }
    }
}